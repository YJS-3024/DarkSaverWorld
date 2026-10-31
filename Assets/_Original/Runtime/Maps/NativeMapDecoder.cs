using System;
using System.Collections.Generic;
using System.IO;

namespace DarkSaver.Original.Maps
{
    public sealed class NativeMapStamp
    { public int x, y, width, height; public byte[] rgba; }
    public sealed class NativeMapPixels
    { public int width, height, decorations; public byte[] rgba; public NativeMapStamp[] foreground; }

    // Literal MRC/GRS first-frame reconstruction. No generated map JSON or old runtime.
    public static class NativeMapDecoder
    {
        private sealed class Reader : IDisposable
        {
            private readonly BinaryReader reader;
            public Reader(byte[] bytes) { reader = new BinaryReader(new MemoryStream(bytes)); }
            public int Int() => reader.ReadInt32();
            public int Count(int max) { int n = Int(); if (n < 0 || n > max) throw new InvalidDataException("Native count out of range."); return n; }
            public byte Byte() => reader.ReadByte();
            public byte[] Bytes(int n) { var bytes = reader.ReadBytes(n); if (bytes.Length != n) throw new EndOfStreamException(); return bytes; }
            public void End() { if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Native trailing bytes."); }
            public void Dispose() => reader.Dispose();
        }
        private sealed class Frame { public int width, height; public byte[] data; }
        private sealed class Group { public byte[] palette; public byte[][] tiles; public Frame[] frames; }
        private sealed class Graphics { public Group[] tiles, sprites; }
        public static (int width, int height) Dimensions(byte[] map)
        {
            using (var r = new Reader(map))
            { int n = r.Count(256); r.Bytes(n * 4); int w = r.Count(512), h = r.Count(512); if (w == 0 || h == 0) throw new InvalidDataException("Empty map."); return (w, h); }
        }
        private static Frame Bitmap(Reader r)
        {
            int w = r.Count(4096), h = r.Count(4096), size = r.Count(16777218); r.Bytes(2);
            if (w == 0 || h == 0 || (size != w * h && size != w * h + 2)) throw new InvalidDataException("Invalid native bitmap size.");
            return new Frame { width = w, height = h, data = r.Bytes(size) };
        }
        private static Graphics ReadGraphics(byte[] bytes)
        {
            using (var r = new Reader(bytes))
            {
                r.Int(); r.Int(); var groups = new Group[r.Count(4096)];
                for (int i = 0; i < groups.Length; i++)
                {
                    r.Int(); int cols = r.Count(4096), rows = r.Count(4096);
                    if ((long)cols * rows > 1048576) throw new InvalidDataException("Too many native tile cells.");
                    var g = new Group { palette = r.Bytes(1024), tiles = new byte[cols * rows][] };
                    for (int t = 0; t < g.tiles.Length; t++) { r.Byte(); r.Byte(); g.tiles[t] = r.Bytes(r.Byte()); }
                    g.frames = new Frame[r.Byte()]; for (int f = 0; f < g.frames.Length; f++) g.frames[f] = Bitmap(r);
                    groups[i] = g;
                }
                var sprites = new Group[r.Count(65536)];
                for (int i = 0; i < sprites.Length; i++)
                {
                    r.Int(); r.Int(); var g = new Group { palette = r.Bytes(1024) }; r.Byte(); r.Byte();
                    g.frames = new Frame[r.Byte()]; for (int f = 0; f < g.frames.Length; f++) g.frames[f] = Bitmap(r);
                    sprites[i] = g;
                }
                r.End(); return new Graphics { tiles = groups, sprites = sprites };
            }
        }
        public static NativeMapPixels Render(byte[] map, Func<int, byte[]> resource) => Render(map, resource, false);
        public static NativeMapPixels RenderLayers(byte[] map, Func<int, byte[]> resource) => Render(map, resource, true);
        private static NativeMapPixels Render(byte[] map, Func<int, byte[]> resource, bool layers)
        {
            using (var r = new Reader(map))
            {
                int n = r.Count(256); var resources = new Dictionary<int, Graphics>();
                for (int i = 0; i < n; i++) { int id = r.Int(); if(!resources.ContainsKey(id))resources.Add(id, ReadGraphics(resource(id))); }
                int w = r.Count(512), h = r.Count(512), count = r.Count(1048576);
                if (w == 0 || h == 0 || (long)w * h > 65536) throw new InvalidDataException("Native map dimensions out of range.");
                int cells = checked(w * h); var a = r.Bytes(cells); var b = r.Bytes(cells); var details = r.Bytes(cells * 5);
                var pixels = new NativeMapPixels { width = w * 32, height = h * 32, decorations = count, rgba = new byte[checked(w * h * 32 * 32 * 4)] };
                for (int i = 3; i < pixels.rgba.Length; i += 4) pixels.rgba[i] = 255;
                for (int i = 0; i < cells; i++)
                {
                    var ground = resources[a[i]].tiles[b[i]]; Draw(pixels, ground, 0, i % w * 32, i / w * 32, false);
                    int at = i * 5;
                    if (details[at + 1] != 255)
                    {
                        var g = resources[details[at]].tiles[details[at + 1]]; var frames = g.tiles[details[at + 2]];
                        if (frames.Length == 0) throw new InvalidDataException("Empty native detail animation.");
                        Draw(pixels, g, frames[0], i % w * 32, i / w * 32, true);
                    }
                }
                var foreground = new List<NativeMapStamp>();
                for (int i = 0; i < count; i++)
                {
                    int id = r.Byte(); r.Bytes(3); int x = r.Int(), y = r.Int(), index = r.Byte(), frame = r.Byte(); r.Bytes(2);
                    var group = resources[id].sprites[index];
                    if (!layers) Draw(pixels, group, frame, x * 32, y * 32, true);
                    else
                    {
                        var image = group.frames[frame]; var target = new NativeMapPixels { width = image.width, height = image.height, rgba = new byte[image.width * image.height * 4] };
                        Draw(target, group, frame, 0, 0, true);
                        foreground.Add(new NativeMapStamp { x = x * 32, y = y * 32, width = image.width, height = image.height, rgba = target.rgba });
                    }
                }
                pixels.foreground = foreground.ToArray(); r.End(); return pixels;
            }
        }
        private static void Draw(NativeMapPixels target, Group group, int frame, int x, int y, bool transparent)
        {
            var image = group.frames[frame];
            for (int sy = 0; sy < image.height; sy++)
            {
                int dy = y + sy; if (dy < 0 || dy >= target.height) continue;
                for (int sx = 0; sx < image.width; sx++)
                {
                    int dx = x + sx; if (dx < 0 || dx >= target.width) continue;
                    int p = image.data[(image.height - 1 - sy) * image.width + sx] * 4;
                    byte red = group.palette[p], green = group.palette[p + 1], blue = group.palette[p + 2];
                    if (transparent && red == 0 && green == 0 && blue == 0) continue;
                    int at = (dy * target.width + dx) * 4;
                    target.rgba[at] = red; target.rgba[at + 1] = green; target.rgba[at + 2] = blue; target.rgba[at + 3] = 255;
                }
            }
        }
        // Kept pure so viewport/zoom input can be checked without a Unity window.
        public static double Zoom(double current, double wheel) => Math.Max(.125, Math.Min(4, current * Math.Pow(1.12, -wheel)));
        public static int Grid(double canvas, double pan, double zoom, int limit) => Math.Max(0, Math.Min(limit - 1, (int)Math.Floor((canvas - pan) / (32 * zoom))));
    }
}
