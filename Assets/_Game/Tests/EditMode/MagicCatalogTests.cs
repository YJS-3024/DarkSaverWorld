using System;
using System.IO;
using System.Linq;
using DarkSaver.Editor;
using DarkSaver.Magic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class MagicCatalogTests
    {
        private static string Row(string id = "101", string name = "마법_이름", string tick = "25") =>
            id + "," + name + ",1,-1,3,900,7,8,9,-2,11,12," + tick + ",14,15,16,17";

        [Test]
        public void ParserSeparatesTickTimeMpAndPreservesSignedValues()
        {
            var row = MagicCatalogConverter.Parse("\uFEFF" + MagicCatalogConverter.Header + "\n" + Row())[0];
            Assert.That(row.Id, Is.EqualTo(101));
            Assert.That(row.Name, Is.EqualTo("마법_이름"));
            Assert.That(row.RawTick, Is.EqualTo(25));
            Assert.That(row.RawTime, Is.EqualTo(7));
            Assert.That(row.Mp, Is.EqualTo(900));
            Assert.That(row.Range, Is.EqualTo(-1));
            Assert.That(row.GetNumericValue(9), Is.EqualTo(-2));
            Assert.That(row.IconY, Is.EqualTo(17));
            Assert.Throws<ArgumentOutOfRangeException>(() => row.GetNumericValue(1));
        }

        [Test]
        public void ParserPreservesQuotesEmptyNamesZeroAndLargeValues()
        {
            var rows = MagicCatalogConverter.Parse(MagicCatalogConverter.Header + "\n" +
                Row("101", "\"마법,\"\"인용\"\"\"", long.MaxValue.ToString()) + "\n" + Row("102", "", "0"));
            Assert.That(rows[0].Name, Is.EqualTo("마법,\"인용\""));
            Assert.That(rows[0].RawTick, Is.EqualTo(long.MaxValue));
            Assert.That(rows[1].Name, Is.Empty);
            Assert.That(rows[1].RawTick, Is.Zero);
        }

        [Test]
        public void ParserRejectsInvalidIntegersAndIds()
        {
            foreach (var value in new[] { "1.5", "1E+2", "", "9223372036854775808" })
                Assert.Throws<InvalidDataException>(() => MagicCatalogConverter.Parse(MagicCatalogConverter.Header + "\n" + Row(tick: value)));
            foreach (var value in new[] { "-1", "2147483648" })
                Assert.Throws<InvalidDataException>(() => MagicCatalogConverter.Parse(MagicCatalogConverter.Header + "\n" + Row(id: value)));
        }

        [Test]
        public void ParserRejectsHeaderColumnsQuotesEmptyAndDuplicates()
        {
            foreach (var csv in new[] { "", MagicCatalogConverter.Header,
                "wrong\n" + Row(), MagicCatalogConverter.Header + "\n101,name",
                MagicCatalogConverter.Header + "\n" + Row(name: "\"unterminated"),
                MagicCatalogConverter.Header + "\n" + Row() + "\n" + Row() })
                Assert.Throws<InvalidDataException>(() => MagicCatalogConverter.Parse(csv));
        }

        [Test]
        public void CatalogCopiesInputsSortsAndReportsMissing()
        {
            var numbers = new long[16]; numbers[0] = 102; numbers[11] = 25;
            var entry = new MagicCatalogEntry(numbers, "copied");
            numbers[0] = 999; numbers[11] = 0;
            var source = new[] { entry, MagicCatalogConverter.Parse(MagicCatalogConverter.Header + "\n" + Row())[0] };
            var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
            try
            {
                catalog.Initialize(source); source[0] = null;
                Assert.That(catalog.GetAt(0).Id, Is.EqualTo(101));
                Assert.That(catalog.TryGet(102, out var found), Is.True);
                Assert.That(found.RawTick, Is.EqualTo(25));
                Assert.That(catalog.TryGet(999, out var missing), Is.False);
                Assert.That(missing, Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void InvalidInitializationAndVerificationPreserveExistingData()
        {
            var row = MagicCatalogConverter.Parse(MagicCatalogConverter.Header + "\n" + Row())[0];
            var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
            try
            {
                catalog.Initialize(new[] { row });
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new[] { row, row }));
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new MagicCatalogEntry[] { null }));
                Assert.Throws<ArgumentException>(() => catalog.Initialize(Array.Empty<MagicCatalogEntry>()));
                Assert.Throws<InvalidDataException>(() => MagicCatalogConverter.Verify(catalog,
                    MagicCatalogConverter.Parse(MagicCatalogConverter.Header + "\n" + Row(tick: "0"))));
                Assert.That(catalog.Count, Is.EqualTo(1));
                Assert.That(catalog.GetAt(0).RawTick, Is.EqualTo(25));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void NativeAssetMatchesAllSourceFieldsWithoutRawAssetDependencies()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_1.2/converted/Magictbl.csv");
            var rows = MagicCatalogConverter.Parse(File.ReadAllText(path));
            var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>(MagicCatalogConverter.Output);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(439));
            MagicCatalogConverter.Verify(catalog, rows);
            Assert.That(catalog.TryGet(101, out var fireball), Is.True);
            Assert.That(fireball.RawTick, Is.EqualTo(10));
            Assert.That(catalog.TryGet(201, out var fire), Is.True);
            Assert.That(fire.RawTick, Is.EqualTo(15));
            Assert.That(AssetDatabase.GetDependencies(MagicCatalogConverter.Output, true)
                .Any(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".atr", StringComparison.OrdinalIgnoreCase)), Is.False);
        }
    }
}
