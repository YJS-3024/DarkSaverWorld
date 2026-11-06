using System;
using System.Collections.Generic;
using System.IO;

namespace DarkSaver.Original.Maps
{
    public sealed class NativeMapStamp
    { public int x, y, width, height; public byte[] rgba; }
    public sealed class NativeMapPixels
    { public int width, height, decorations; public byte[] rgba,renderAttributes; public NativeMapStamp[] foreground; public NativeMapTileAnimation[] animations; public byte animationPhase; }

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
        private sealed class Group { public int width,height; public byte[] palette; public byte[][] tiles; public Frame[] frames; }
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
                    int width=r.Int(),height=r.Int();var g = new Group { width=width,height=height,palette = r.Bytes(1024) }; r.Byte(); r.Byte();
                    g.frames = new Frame[r.Byte()]; for (int f = 0; f < g.frames.Length; f++) g.frames[f] = Bitmap(r);
                    sprites[i] = g;
                }
                r.End(); return new Graphics { tiles = groups, sprites = sprites };
            }
        }
        public sealed class MapChange
        {public string name;public int[] args;}
        public sealed class ChangedMap
        {public byte[] bytes;public int[] attributes,renderAttributes;}
        // Patch a private MRC copy. SpriteChange replaces the first matching origin only;
        // its collision footprint comes from the GRS group declaration, not frame dimensions.
        public static ChangedMap ApplyChanges(byte[] source,Func<int,byte[]> resource,MapChange[] changes,int[] originalAttributes)
        {
            if(source==null)throw new InvalidDataException("Missing native map.");
            var dimensions=Dimensions(source);int w=dimensions.width,h=dimensions.height,cells=checked(w*h);
            int detail=checked(16+BitConverter.ToInt32(source,0)*4+cells*2),sprite=checked(detail+cells*5);
            int count=BitConverter.ToInt32(source,detail-cells*2-4);
            if(count<0||sprite+(long)count*16!=source.Length||originalAttributes==null||originalAttributes.Length!=cells)throw new InvalidDataException("Invalid mutable MRC.");
            var bytes=(byte[])source.Clone();var attrs=(int[])originalAttributes.Clone();var graphics=new Dictionary<int,Graphics>();
            foreach(var change in changes??Array.Empty<MapChange>())
            {
                var a=change.args;if(a==null)throw new InvalidDataException("Missing map change arguments.");
                if(change.name=="CHANGETILE2"&&a.Length==7)
                {
                    if(a[0]<0||a[1]<0||a[0]>=w||a[1]>=h)continue;
                    int tile=a[1]*w+a[0],at=detail+tile*5;
                    bytes[at]=(byte)a[2];bytes[at+1]=(byte)a[3];bytes[at+2]=(byte)a[4];
                    bytes[at+3]=(byte)a[6];bytes[at+4]=(byte)a[5];attrs[tile]=(byte)a[6];
                }
                else if(change.name=="CHANGESPRITE"&&a.Length==6)
                {
                    int at=-1;for(int i=0;i<count;i++){int start=sprite+i*16;if(BitConverter.ToInt32(bytes,start+4)==a[0]&&BitConverter.ToInt32(bytes,start+8)==a[1]){at=start;break;}}
                    if(at<0)continue;
                    int id=(byte)a[2],index=(byte)a[3],frame=(byte)a[4];
                    if(!graphics.TryGetValue(id,out var bank)){bank=ReadGraphics(resource(id));graphics.Add(id,bank);}
                    if(index>=bank.sprites.Length||frame>=bank.sprites[index].frames.Length)throw new InvalidDataException("Missing replacement native sprite.");
                    var group=bank.sprites[index];if(group.width<0||group.height<0||group.width>4096||group.height>4096)throw new InvalidDataException("Invalid sprite footprint.");
                    bytes[at]=(byte)id;bytes[at+12]=(byte)index;bytes[at+13]=(byte)frame;
                    for(int y=a[1];y<a[1]+group.height/32;y++)for(int x=a[0];x<a[0]+group.width/32;x++)
                        if(x>=0&&y>=0&&x<w&&y<h){int tile=y*w+x;attrs[tile]=(byte)a[5];bytes[detail+tile*5+3]=(byte)a[5];}
                }
                else throw new InvalidDataException("Unsupported map mutation.");
            }
            var render=new int[cells];for(int i=0;i<cells;i++)render[i]=bytes[detail+i*5+3];
            return new ChangedMap{bytes=bytes,attributes=attrs,renderAttributes=render};
        }
        public static NativeMapPixels Render(byte[] map, Func<int, byte[]> resource) => Render(map, resource, false);
        public static NativeMapPixels RenderLayers(byte[] map, Func<int, byte[]> resource) => Render(map, resource, true);
        public static NativeMapPixels RenderAnimatedLayers(byte[] map,Func<int,byte[]> resource,uint seed)
            =>Render(map,resource,true,new NativeMapRandom(seed));
        private static NativeMapPixels Render(byte[] map, Func<int, byte[]> resource, bool layers,NativeMapRandom random=null)
        {
            using (var r = new Reader(map))
            {
                int n = r.Count(256); var resources = new Dictionary<int, Graphics>();
                for (int i = 0; i < n; i++) { int id = r.Int(); if(!resources.ContainsKey(id))resources.Add(id, ReadGraphics(resource(id))); }
                int w = r.Count(512), h = r.Count(512), count = r.Count(1048576);
                if (w == 0 || h == 0 || (long)w * h > 65536) throw new InvalidDataException("Native map dimensions out of range.");
                int cells = checked(w * h); var a = r.Bytes(cells); var b = r.Bytes(cells); var details = r.Bytes(cells * 5);
                var pixels = new NativeMapPixels { width = w * 32, height = h * 32, decorations = count, rgba = new byte[checked(w * h * 32 * 32 * 4)],renderAttributes=new byte[cells] };
                for(int i=0;i<cells;i++)pixels.renderAttributes[i]=details[i*5+3];
                for (int i = 3; i < pixels.rgba.Length; i += 4) pixels.rgba[i] = 255;
                var animations=new List<NativeMapTileAnimation>();
                var animationCache=new Dictionary<(byte,byte,byte,byte,byte),NativeMapStamp[]>();
                for (int i = 0; i < cells; i++)
                {
                    byte randomCell=random==null?(byte)0:random.Next(); // Native loader generates one byte for every cell.
                    var ground = resources[a[i]].tiles[b[i]]; Draw(pixels, ground, 0, i % w * 32, i / w * 32, false);
                    int at = i * 5;
                    if (details[at + 1] != 255)
                    {
                        var g = resources[details[at]].tiles[details[at + 1]]; var frames = g.tiles[details[at + 2]];
                        if (frames.Length == 0) throw new InvalidDataException("Empty native detail animation.");
                        int initial=random==null?0:NativeMapAnimation.Index(details[at+4],randomCell,0,frames.Length);
                        Draw(pixels, g, frames[initial], i % w * 32, i / w * 32, true);
                        if(random!=null&&details[at+4]>=2&&Array.Exists(frames,f=>f!=frames[0]))
                        {
                            var key=(a[i],b[i],details[at],details[at+1],details[at+2]);
                            if(!animationCache.TryGetValue(key,out var bank))
                            {
                                bank=new NativeMapStamp[frames.Length];var cache=new Dictionary<byte,NativeMapStamp>();
                                for(int f=0;f<frames.Length;f++)
                                {
                                    if(!cache.TryGetValue(frames[f],out var stamp))
                                    {
                                        var tile=new NativeMapPixels{width=32,height=32,rgba=new byte[32*32*4]};
                                        Draw(tile,ground,0,0,0,false);Draw(tile,g,frames[f],0,0,true);
                                        stamp=new NativeMapStamp{width=32,height=32,rgba=tile.rgba};cache.Add(frames[f],stamp);
                                    }
                                    bank[f]=stamp;
                                }
                                animationCache.Add(key,bank);
                            }
                            animations.Add(new NativeMapTileAnimation{x=i%w,y=i/w,frames=bank});
                        }
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
                pixels.foreground = foreground.ToArray();pixels.animations=animations.ToArray(); r.End(); return pixels;
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
