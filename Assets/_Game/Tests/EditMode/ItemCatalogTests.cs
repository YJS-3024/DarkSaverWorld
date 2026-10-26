using System;
using System.IO;
using DarkSaver.Editor;
using DarkSaver.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ItemCatalogTests
    {
        [Test]
        public void ParserPreservesExponentNegativeValuesAndText()
        {
            var fields = Fields("1E+11");
            fields[4] = "-5"; fields[21] = "-50"; fields[41] = "1,4"; fields[42] = "이름_\"보존\"";
            var row = ItemCatalogConverter.Parse("\uFEFF" + ItemCatalogConverter.Header + "\n" + CsvRow(fields))[0];
            Assert.That(row.GetNumericValue(ItemCatalogEntry.PriceColumn), Is.EqualTo(100000000000L));
            Assert.That(row.PriceNotation, Is.EqualTo("1E+11"));
            Assert.That(row.GetNumericValue(4), Is.EqualTo(-5));
            Assert.That(row.GetNumericValue(21), Is.EqualTo(-50));
            Assert.That(row.DamageTargets, Is.EqualTo("1,4"));
            Assert.That(row.Name, Is.EqualTo("이름_\"보존\""));
            Assert.That(row.Description, Is.EqualTo("설명_보존"));
            fields[42] = "";
            Assert.That(ItemCatalogConverter.Parse(ItemCatalogConverter.Header + "\n" + CsvRow(fields))[0].Name, Is.Empty);
        }

        [TestCase("1.00E+11",100000000000L)]
        [TestCase("1.23E+2",123L)]
        [TestCase("9.223372036854775807E+18",long.MaxValue)]
        [TestCase("-9.223372036854775808E+18",long.MinValue)]
        public void ScientificPricesPreserveExactIntegers(string price,long expected)
        {
            var row=ItemCatalogConverter.Parse(ItemCatalogConverter.Header+"\n"+CsvRow(Fields(price)))[0];
            Assert.That(row.GetNumericValue(ItemCatalogEntry.PriceColumn),Is.EqualTo(expected));
            Assert.That(row.PriceNotation,Is.EqualTo(price));
        }

        [TestCase("1.5E+0")]
        [TestCase("0.999999999999999999999999999999999999E+18")]
        [TestCase("9.223372036854775808E+18")]
        [TestCase("0.5")]
        [TestCase("1E-50")]
        [TestCase("1E+30")]
        [TestCase("9223372036854775808")]
        [TestCase("NaN")]
        [TestCase("")]
        public void ParserRejectsUnsupportedOrOverflowPrice(string price)
        {
            Assert.Throws<InvalidDataException>(() => ItemCatalogConverter.Parse(ItemCatalogConverter.Header + "\n" + CsvRow(Fields(price))));
        }

        [Test]
        public void LookupCopiesInputSortsAndRejectsDuplicateWithoutReplacingData()
        {
            var values = new long[ItemCatalogEntry.NumericColumnCount]; values[0] = 2;
            var item = new ItemCatalogEntry(values, "0", "0", "", "", "unknown");
            values[0] = 99;
            Assert.That(item.Id, Is.EqualTo(2));
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            try
            {
                var zero = ItemCatalogConverter.Parse(ItemCatalogConverter.Header + "\n" + CsvRow(Fields("-1")))[0];
                var input = new[] { item, zero };
                catalog.Initialize(input);
                input[0] = zero;
                Assert.That(catalog.GetAt(0).Id, Is.Zero);
                Assert.That(catalog.TryGet(2, out var actual), Is.True);
                Assert.That(actual.Name, Is.Empty);
                Assert.That(catalog.TryGet(1, out var missing), Is.False);
                Assert.That(missing, Is.Null);
                Assert.That(catalog.TryGet(-1, out _), Is.False);
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new[] { item, item }));
                Assert.That(catalog.Count, Is.EqualTo(2));
                Assert.That(catalog.TryGet(2, out _), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void NativeAssetPreservesAllItemsAndHasNoCsvDependency()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(1212));
            Assert.That(catalog.TryGet(0, out var first), Is.True);
            Assert.That(first.Name, Is.EqualTo("바늘"));
            Assert.That(catalog.TryGet(1245, out var expensive), Is.True);
            Assert.That(expensive.PriceNotation, Is.EqualTo("80000000000"));
            Assert.That(expensive.GetNumericValue(ItemCatalogEntry.PriceColumn), Is.EqualTo(80000000000L));
            Assert.That(catalog.TryGet(1538, out var last), Is.True);
            Assert.That(last.Name, Is.EqualTo("닥세_사이다세트"));
            Assert.That(catalog.TryGet(1572, out _), Is.False);
            var blankNames = 0;
            for (var i = 0; i < catalog.Count; i++) if (string.IsNullOrWhiteSpace(catalog.GetAt(i).Name)) blankNames++;
            Assert.That(blankNames, Is.Zero);
            foreach (var path in AssetDatabase.GetDependencies(ItemCatalogConverter.Output, true))
                Assert.That(path.EndsWith(".cs", StringComparison.Ordinal) || path == ItemCatalogConverter.Output, Is.True, path);
        }

        private static string[] Fields(string price)
        {
            var fields = new string[45]; Array.Fill(fields, "0");
            fields[16] = price; fields[42] = "이름_보존"; fields[43] = "설명_보존"; fields[44] = "weapon";
            return fields;
        }

        private static string CsvRow(string[] fields) => "\"" + string.Join("\",\"", Array.ConvertAll(fields, value => value.Replace("\"", "\"\""))) + "\"";
    }
}
