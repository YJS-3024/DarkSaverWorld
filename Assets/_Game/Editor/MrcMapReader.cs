using System;
using System.Collections.Generic;
using System.IO;

namespace DarkSaver.Maps
{
    public readonly struct MrcTile
    {
        public MrcTile(byte resource, byte group, byte frame, byte rawAttribute, byte animation)
        {
            Resource = resource;
            Group = group;
            Frame = frame;
            RawAttribute = rawAttribute;
            Animation = animation;
        }

        public byte Resource { get; }
        public byte Group { get; }
        public byte Frame { get; }
        public byte RawAttribute { get; }
        public byte Animation { get; }
        public bool SpriteOccluded => (RawAttribute & 0x80) != 0;
        public byte TerrainAttribute
        {
            get
            {
                var value = (byte)(RawAttribute & 0x7F);
                return value > 0x1B ? (byte)0 : value;
            }
        }
        public bool UsesWaterOverlay => TerrainAttribute == 0x06 || TerrainAttribute == 0x14;
    }

    public readonly struct MrcSprite
    {
        public MrcSprite(int sourceIndex, int x, int y, byte[] raw)
        {
            SourceIndex = sourceIndex;
            X = x;
            Y = y;
            Raw = raw;
        }
        public int SourceIndex { get; }
        public int X { get; }
        public int Y { get; }
        public byte[] Raw { get; }
    }

    public sealed class MrcMap
    {
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int[] ResourceIds { get; internal set; }
        public byte[] BaseResourceLayer { get; internal set; }
        public byte[] BaseTileLayer { get; internal set; }
        public MrcTile[] Tiles { get; internal set; }
        public IReadOnlyList<MrcSprite> Sprites { get; internal set; }
        public MrcTile TileAt(int x, int y) => Tiles[y * Width + x];
    }

    public static class MrcMapReader
    {
        public static MrcMap Read(byte[] data)
        {
            if (data == null || data.Length < 16) throw new InvalidDataException("MRC header is missing.");
            using var stream = new MemoryStream(data, false);
            using var reader = new BinaryReader(stream);
            var resourceCount = reader.ReadInt32();
            if (resourceCount < 0) throw new InvalidDataException("MRC resource count is invalid.");
            var resources = new int[resourceCount];
            for (var i = 0; i < resources.Length; i++) resources[i] = reader.ReadInt32();
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var spriteCount = reader.ReadInt32();
            if (width <= 0 || height <= 0 || spriteCount < 0)
                throw new InvalidDataException("MRC header contains invalid counts.");

            var expected = 16L + 4L * resourceCount + 7L * width * height + 16L * spriteCount;
            if (expected != data.Length)
                throw new InvalidDataException($"MRC size mismatch: expected {expected}, got {data.Length}.");
            var cellCount = checked(width * height);
            var baseResources = reader.ReadBytes(cellCount);
            var baseTiles = reader.ReadBytes(cellCount);
            var tiles = new MrcTile[cellCount];
            for (var i = 0; i < tiles.Length; i++)
                tiles[i] = new MrcTile(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(),
                    reader.ReadByte(), reader.ReadByte());

            var sprites = new List<MrcSprite>(spriteCount);
            for (var i = 0; i < spriteCount; i++)
            {
                var raw = reader.ReadBytes(16);
                sprites.Add(new MrcSprite(i, BitConverter.ToInt32(raw, 4), BitConverter.ToInt32(raw, 8), raw));
            }

            return new MrcMap
            {
                Width = width,
                Height = height,
                ResourceIds = resources,
                BaseResourceLayer = baseResources,
                BaseTileLayer = baseTiles,
                Tiles = tiles,
                Sprites = sprites
            };
        }
    }
}
