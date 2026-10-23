import fs from 'node:fs/promises';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
const require = createRequire(pathToFileURL(`${process.argv[2]}/package.json`));
const { Workbook } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')));
const csv = await fs.readFile('분석/Saver_1.2/catalogs/items.csv', 'utf8');
const workbook = await Workbook.fromCSV(csv.replace(/^\uFEFF/, ''), { sheetName: 'Items' });
const rows = workbook.worksheets.getItemAt(0).getUsedRange().values;
const header = rows.shift();
const textStart = header.indexOf('데미지병종(n명)');
const ids = new Set(), categories = {}, minimums = Array(textStart).fill(Infinity), maximums = Array(textStart).fill(-Infinity);
let blankNames = 0;
const nonInteger = {};
for (const row of rows) {
  if (row.length !== header.length) throw new Error('Column count differs.');
  for (let i = 0; i < textStart; i++) {
    if (!/^-?\d+$/.test(String(row[i]))) {
      const issue = nonInteger[header[i]] ??= { count: 0, examples: [] };
      issue.count++;
      if (issue.examples.length < 2) issue.examples.push({ id: row[0], raw: row[i] });
      if (i === header.indexOf('가격') && /^-?\d+[Ee]\+?\d+$/.test(String(row[i])) && Number.isSafeInteger(Number(row[i]))) {
        minimums[i] = Math.min(minimums[i], Number(row[i])); maximums[i] = Math.max(maximums[i], Number(row[i]));
      }
      continue;
    }
    const value = Number(row[i]);
    if (!Number.isSafeInteger(value)) throw new Error('Unsafe integer.');
    minimums[i] = Math.min(minimums[i], value); maximums[i] = Math.max(maximums[i], value);
  }
  if (!/^\d+$/.test(String(row[0]))) throw new Error('Invalid item ID.');
  if (ids.has(row[0])) throw new Error(`Duplicate ID ${row[0]}`);
  ids.add(row[0]);
  if (!String(row[header.indexOf('이름')] ?? '').trim()) blankNames++;
  const category = String(row.at(-1)); categories[category] = (categories[category] ?? 0) + 1;
}
console.log(JSON.stringify({ rows: rows.length, header, numericColumns: textStart, uniqueIds: ids.size,
  idRange: [minimums[0], maximums[0]], blankNames, categories,
  negativeColumns: header.slice(0, textStart).filter((_, i) => minimums[i] < 0).map(name => ({ name, min: minimums[header.indexOf(name)] })),
  maxNumber: Math.max(...maximums), nonInteger, first: rows[0], last: rows.at(-1) }));
