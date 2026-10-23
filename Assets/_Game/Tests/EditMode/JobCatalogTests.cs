using System;
using System.IO;
using System.Linq;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class JobCatalogTests
    {
        [Test]
        public void ParserPreservesListsSentinelsQuotesAndSignedValues()
        {
            var fields = Fields();
            fields[2] = long.MaxValue.ToString(); fields[3] = "-5";
            fields[34] = "134,137"; fields[35] = "이름_\"보존\"";
            var row = JobCatalogConverter.Parse("\uFEFF" + JobCatalogConverter.Header + "\n" + CsvRow(fields))[0];
            Assert.That(row.GetNumericValue(2), Is.EqualTo(long.MaxValue));
            Assert.That(row.GetNumericValue(3), Is.EqualTo(-5));
            Assert.That(row.HorizontalTransitionText, Is.EqualTo("134,137"));
            Assert.That(row.Name, Is.EqualTo("이름_\"보존\""));
            Assert.That(row.Description, Is.EqualTo("설명_원문"));
            foreach (var sentinel in new[] { "0", "-1" })
            {
                fields[34] = sentinel; fields[35] = string.Empty;
                row = JobCatalogConverter.Parse(JobCatalogConverter.Header + "\n" + CsvRow(fields))[0];
                Assert.That(row.HorizontalTransitionText, Is.EqualTo(sentinel));
                Assert.That(row.Name, Is.Empty);
            }
        }

        [TestCase("1.5")]
        [TestCase("1E+2")]
        [TestCase("")]
        [TestCase("9223372036854775808")]
        public void ParserRejectsInvalidInteger(string value)
        {
            var fields = Fields(); fields[2] = value;
            Assert.Throws<InvalidDataException>(() => JobCatalogConverter.Parse(JobCatalogConverter.Header + "\n" + CsvRow(fields)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ParserRejectsMalformedCsv(int kind)
        {
            var header = JobCatalogConverter.Header;
            var row = CsvRow(Fields());
            if (kind == 0) header = header.Replace("직그림", "잘못된헤더");
            if (kind == 1) row = CsvRow(Fields().Take(37).ToArray());
            if (kind == 2) row = "\"100,100";
            Assert.Throws<InvalidDataException>(() => JobCatalogConverter.Parse(header + "\n" + row));
            Assert.Throws<InvalidDataException>(() => JobCatalogConverter.Parse(JobCatalogConverter.Header));
        }

        [TestCase(-1L)]
        [TestCase(2147483648L)]
        public void RecordRejectsInvalidId(long id)
        {
            var numbers = new long[JobCatalogEntry.NumericColumnCount]; numbers[0] = id;
            Assert.Throws<ArgumentException>(() => new JobCatalogEntry(numbers, "0", "직업", "설명", "player_job"));
        }

        [Test]
        public void InitializationCopiesSortsAndRejectsDuplicatesWithoutReplacing()
        {
            var catalog = ScriptableObject.CreateInstance<JobCatalog>();
            try
            {
                var numbers = new long[JobCatalogEntry.NumericColumnCount]; numbers[0] = 2;
                var two = new JobCatalogEntry(numbers, "0", "둘", "설명", "player_job");
                numbers[0] = 0;
                var zero = new JobCatalogEntry(numbers, "-1", "영", "설명", "player_job");
                numbers[0] = 99;
                var input = new[] { two, zero };
                catalog.Initialize(input); input[0] = zero;
                Assert.That(catalog.GetAt(0).Id, Is.Zero);
                Assert.That(catalog.TryGet(2, out var result), Is.True);
                Assert.That(result, Is.SameAs(two));
                Assert.That(catalog.TryGet(1, out result), Is.False);
                Assert.That(result, Is.Null);
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new[] { two, two }));
                Assert.Throws<ArgumentException>(() => catalog.Initialize(Array.Empty<JobCatalogEntry>()));
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new JobCatalogEntry[] { null }));
                Assert.That(catalog.Count, Is.EqualTo(2));
                Assert.That(catalog.GetAt(0), Is.SameAs(zero));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void SavedNativePlayerJobsPreserveKnownRowsAndHaveNoCsvDependency()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.Output);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(70));
            for (var id = 100; id <= 169; id++)
            {
                Assert.That(catalog.TryGet(id, out var row), Is.True);
                Assert.That(row.CategoryLabel, Is.EqualTo("player_job"));
            }
            Assert.That(catalog.TryGet(100, out var first), Is.True);
            Assert.That(first.Name, Is.EqualTo("파이터"));
            Assert.That(first.SpriteId, Is.EqualTo(100));
            Assert.That(first.GetNumericValue(2), Is.EqualTo(200));
            Assert.That(first.Description, Is.EqualTo("보병타입의_제1단계"));
            Assert.That(catalog.TryGet(135, out var list), Is.True);
            Assert.That(list.HorizontalTransitionText, Is.EqualTo("134,137"));
            Assert.That(catalog.TryGet(169, out var last), Is.True);
            Assert.That(last.Name, Is.EqualTo("그랑메이지"));
            Assert.That(last.HorizontalTransitionText, Is.EqualTo("-1"));
            Assert.That(catalog.TryGet(99, out _), Is.False);
            Assert.That(catalog.TryGet(170, out _), Is.False);
            Assert.That(catalog.TryGet(701, out _), Is.False);
            foreach (var dependency in AssetDatabase.GetDependencies(JobCatalogConverter.Output))
                Assert.That(dependency.EndsWith(".csv", StringComparison.OrdinalIgnoreCase), Is.False);
        }

        [Test]
        public void VerifyRejectsChangedRowsAndDuplicateSourceWithoutModifyingAsset()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.Output);
            var source = Enumerable.Range(0, catalog.Count).Select(catalog.GetAt).ToArray();
            JobCatalogConverter.Verify(catalog, source);
            var original = source[0];
            var values = Enumerable.Range(0, JobCatalogEntry.NumericColumnCount).Select(original.GetNumericValue).ToArray();
            values[2]++;
            source[0] = new JobCatalogEntry(values, original.HorizontalTransitionText, original.Name, original.Description, original.CategoryLabel);
            Assert.Throws<InvalidDataException>(() => JobCatalogConverter.Verify(catalog, source));
            source[0] = original; source[1] = original;
            Assert.Throws<InvalidDataException>(() => JobCatalogConverter.Verify(catalog, source));
            Assert.That(catalog.GetAt(0).GetNumericValue(2), Is.EqualTo(200));
            Assert.That(catalog.Count, Is.EqualTo(70));
        }

        private static string[] Fields()
        {
            var fields = Enumerable.Repeat("0", 38).ToArray();
            fields[0] = "135"; fields[1] = "135"; fields[35] = "직업"; fields[36] = "설명_원문"; fields[37] = "player_job";
            return fields;
        }

        private static string CsvRow(string[] fields) => string.Join(",", fields.Select(field => "\"" + field.Replace("\"", "\"\"") + "\""));
    }
}
