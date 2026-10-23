import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
// This workspace volume cannot create junctions; resolve the bundled public entry.
const require = createRequire(pathToFileURL(`${process.argv[2]}/package.json`));
const { Workbook } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')));

// Read-only source audit: no workbook export or source writes.
for (const source of (process.argv[3] ? [process.argv[3]] : ['catalogs/jobs.csv', 'converted/Magictbl.csv', 'converted/magicatr.csv'])) {
  const file = `분석/Saver_1.2/${source}`;
  const before = await fs.readFile(file);
  const workbook = await Workbook.fromCSV(before.toString('utf8').replace(/^\uFEFF/, ''), { sheetName: 'Source' });
  const matrix = workbook.worksheets.getItemAt(0).getUsedRange().values;
  const [header, ...rows] = matrix;
  const columns = header.map((label, index) => ({label, index}));
  const tick = columns.filter(c => /틱|AP|tick|비용|소모/i.test(String(c.label)));
  const named = columns.filter(c => /직업|마법|이름|번호|ID/i.test(String(c.label)));
  const relevant = [...new Map([...named, ...tick].map(c => [c.index, c])).values()];
  if (source === 'converted/Magictbl.csv') {
    const numericColumns = columns.filter(c => c.index !== 1);
    for (const row of rows) for (const column of numericColumns)
      if (!/^-?\d+$/.test(String(row[column.index])) || !Number.isSafeInteger(Number(row[column.index])))
        throw new Error(`Invalid numeric field ${column.index}`);
    const ids = rows.map(row => Number(row[0]));
    if (new Set(ids).size !== ids.length) throw new Error('Duplicate magic ID');
    console.log(JSON.stringify({magicAudit: true, count: rows.length, idMin: Math.min(...ids), idMax: Math.max(...ids),
      numericColumns: numericColumns.length, emptyNames: rows.filter(row => !row[1]).length,
      negativeColumns: numericColumns.filter(c => rows.some(row => Number(row[c.index]) < 0)).map(c => c.label)}));
  }
  console.log(JSON.stringify({ source, rows: rows.length, header,
    tick: tick.map(c => ({...c, unique: [...new Set(rows.map(r => r[c.index]))]})),
    samples: rows.slice(0, 8).map((r, index) => ({row: index + 2, fields: relevant.map(c => [c.label, r[c.index]])})) }));
  const after = await fs.readFile(file);
  if (crypto.createHash('sha256').update(before).digest('hex') !== crypto.createHash('sha256').update(after).digest('hex'))
    throw new Error(`Source changed: ${source}`);
}
