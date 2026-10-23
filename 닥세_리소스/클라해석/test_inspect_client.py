"""Synthetic fixtures only: no client files are required."""
import contextlib
import io
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch
import inspect_client as kit
import build_release
import zipfile
import hashlib


class InspectionTests(unittest.TestCase):
    def test_probe(self):
        self.assertEqual(kit.entropy(b''), 0)
        self.assertEqual(kit.entropy(bytes(range(256))), 8)
        self.assertEqual(kit.probe_bytes(bytes(32))['size'], 32)

    def test_records(self):
        data = struct.pack('<4H', 3, 5, 7, 9)
        self.assertEqual(kit.numeric_records(data, 0, 2, '<2H')['rows'], [[3, 5], [7, 9]])
        self.assertEqual(kit.numeric_records(data, 8, 0, '<H')['rows'], [])

    def test_bad_records(self):
        for offset, count, fmt, stride in [(-1, 1, '<H', None), (0, 65, '<H', None),
                (0, 1, 'H', None), (0, 1, '<4s', None), (0, 1, '<H', 1),
                (8, 1, '<H', None), (0, 1, '<999Q', None)]:
            with self.subTest(fmt=fmt, offset=offset, count=count):
                with self.assertRaises(ValueError):
                    kit.numeric_records(bytes(8), offset, count, fmt, stride)

    def test_table_privacy(self):
        result = kit.table_shape('가상표본 3\n검사문구 9'.encode('cp949'), 'cp949')
        self.assertEqual(result['rows'], 2)
        self.assertEqual(result['columns'][1]['maximum'], 9)
        self.assertNotIn('가상표본', json.dumps(result, ensure_ascii=False))
        with self.assertRaises(ValueError):
            kit.table_shape(b'\0')

    def test_grid(self):
        result = kit.grid_summary(bytes([0, 1, 0, 0, 1, 0]), 3, 2, 0, 1, 0, {1})
        self.assertEqual(result['largest_components'], [2, 2])
        self.assertEqual(result['passable_cells'], 4)
        with self.assertRaises(ValueError):
            kit.grid_summary(bytes(2), 3, 2, 0, 1, 0, {1})

    def test_pe(self):
        for bits in (32, 64):
            # Construct headers from the public PE specification, not a binary fixture.
            data = bytearray(512)
            data[:2] = b'MZ'
            struct.pack_into('<I', data, 60, 64)
            data[64:68] = b'PE\0\0'
            size = 96 if bits == 32 else 112
            struct.pack_into('<HHIIIHH', data, 68, 0x14c if bits == 32 else 0x8664, 1, 0, 0, 0, size, 0)
            struct.pack_into('<H', data, 88, 0x10b if bits == 32 else 0x20b)
            result = kit.pe_metadata(data)
            self.assertEqual(result['bits'], bits)
            self.assertEqual(len(result['sections']), 1)
            with self.assertRaises(ValueError):
                kit.pe_metadata(data[:100])
        with self.assertRaises(ValueError):
            kit.pe_metadata(bytes(512))

    def test_inventory_and_output(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder) / 'input'
            root.mkdir()
            (root / 'a.dat').write_bytes(bytes(range(32)))
            (root / 'b.dat').write_bytes(bytes(range(32)))
            (root / 'ignored.sav').write_bytes(b'')
            before = (root / 'a.dat').read_bytes()
            result = kit.inventory(root)
            self.assertEqual(len(result['files']), 2)
            self.assertIn('Duplicate content groups: 1', kit.brief(result))
            self.assertLess(len(kit.brief(result)), 2000)
            self.assertEqual(kit.compare(result, result), dict(added=[], removed=[], changed=[]))
            with self.assertRaises(ValueError):
                kit.inventory(root, max_files=1)
            with self.assertRaises(ValueError):
                kit.bounded_read(root / 'a.dat', 8)
            with self.assertRaises(ValueError):
                kit.write_inventory(root, root / 'report')
            output = Path(folder) / 'reports'
            kit.write_inventory(root, output)
            self.assertTrue((output / 'BRIEF.md').is_file())
            with self.assertRaises(ValueError):
                kit.write_inventory(root, output)
            self.assertEqual((root / 'a.dat').read_bytes(), before)

    def test_reparse_refusal(self):
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(kit, 'linked', return_value=True):
                with self.assertRaises(ValueError):
                    kit.inventory(folder)
                with self.assertRaises(ValueError):
                    kit.write_inventory(folder, str(Path(folder).parent / 'unused-report'))

    def test_cli(self):
        with tempfile.TemporaryDirectory() as folder:
            sample = Path(folder) / 'sample.dat'
            sample.write_bytes(bytes(8))
            output = io.StringIO()
            with contextlib.redirect_stdout(output):
                kit.main(['probe', str(sample)])
            self.assertEqual(json.loads(output.getvalue())['result']['size'], 8)
            with contextlib.redirect_stderr(io.StringIO()):
                with self.assertRaises(SystemExit) as error:
                    kit.main(['records', str(sample), '--format', '<H', '--count', '65'])
            self.assertEqual(error.exception.code, 2)

    def test_release_allowlist_and_integrity(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder) / 'kit'
            root.mkdir()
            original = Path(__file__).resolve().parent
            for name in build_release.FILES:
                (root / name).write_bytes((original / name).read_bytes())
            target = Path(folder) / 'release.zip'
            with patch.object(build_release, '__file__', str(root / 'build_release.py')):
                with contextlib.redirect_stdout(io.StringIO()):
                    build_release.build(target)
                with self.assertRaises(ValueError):
                    build_release.build(target)
                (root / 'unexpected.dat').write_bytes(bytes(8))
                with self.assertRaises(ValueError):
                    build_release.build(Path(folder) / 'rejected.zip')
            with zipfile.ZipFile(target) as archive:
                self.assertIsNone(archive.testzip())
                self.assertEqual(set(archive.namelist()), set(build_release.FILES) | {'release_manifest.json'})
                manifest = json.loads(archive.read('release_manifest.json'))
                for row in manifest['files']:
                    self.assertEqual(hashlib.sha256(archive.read(row['path'])).hexdigest(), row['sha256'])


if __name__ == '__main__':
    unittest.main()
