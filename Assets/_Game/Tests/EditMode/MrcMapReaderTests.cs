using System.IO;
using DarkSaver.Maps;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class MrcMapReaderTests
    {
        [Test]
        public void Read_ParsesMinimumMapAndResolvesSentinelAttribute()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(0); writer.Write(1); writer.Write(1); writer.Write(0);
                writer.Write((byte)0); writer.Write((byte)0);
                writer.Write((byte)1); writer.Write((byte)2); writer.Write((byte)3);
                writer.Write((byte)0xFF); writer.Write((byte)0);
            }

            var map = MrcMapReader.Read(stream.ToArray());
            Assert.That(map.Width, Is.EqualTo(1));
            Assert.That(map.TileAt(0, 0).TerrainAttribute, Is.Zero);
            Assert.That(map.TileAt(0, 0).SpriteOccluded, Is.True);
        }
    }
}
