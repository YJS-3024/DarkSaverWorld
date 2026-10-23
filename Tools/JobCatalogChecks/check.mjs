import fs from 'node:fs/promises';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
const require = createRequire(pathToFileURL(`${process.argv[2]}/package.json`));
const { Workbook } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')));
const sources = process.argv.slice(3);
const seen = new Map();
for (const file of sources.length ? sources : ['jobs.csv']) {
const source = `분석/Saver_1.2/catalogs/${file}`;
const csv = await fs.readFile(source, 'utf8');
const workbook = await Workbook.fromCSV(csv.replace(/^\uFEFF/, ''), { sheetName: 'Jobs' });
const data = workbook.worksheets.getItemAt(0).getUsedRange().values;
const [header, ...rows] = data;
const numericCount = header.indexOf('수평전환리스트');
const ids = new Set(), categories = {}, nonInteger = {}, transitions = [];
const min = Array(numericCount).fill(Infinity), max = Array(numericCount).fill(-Infinity);
let blankNames = 0, spriteIdDifferences = 0;
for (const row of rows) {
  if (row.length !== header.length) throw new Error('Column count differs.');
  if (!/^\d+$/.test(String(row[0])) || ids.has(row[0])) throw new Error('Invalid/duplicate job ID.');
  ids.add(row[0]);
  if (seen.has(row[0])) throw new Error(`Cross-catalog duplicate ID ${row[0]} in ${file} and ${seen.get(row[0])}`);
  seen.set(row[0], file);
  if (String(row[0]) !== String(row[1])) spriteIdDifferences++;
  for (let i = 0; i < numericCount; i++) {
    const text = String(row[i]);
    if (!/^-?\d+$/.test(text)) {
      const issue = nonInteger[header[i]] ??= { count: 0, examples: [] };
      issue.count++; if (issue.examples.length < 3) issue.examples.push({ id: row[0], raw: text });
      continue;
    }
    const value = Number(text);
    if (!Number.isSafeInteger(value)) throw new Error('Unsafe integer.');
    min[i] = Math.min(min[i], value); max[i] = Math.max(max[i], value);
  }
  const category = String(row.at(-1)); categories[category] = (categories[category] ?? 0) + 1;
  if (!String(row[header.indexOf('이름')] ?? '').trim()) blankNames++;
  if (String(row[numericCount]) !== '0' && transitions.length < 8) transitions.push({ id: row[0], raw: row[numericCount] });
}
console.log(JSON.stringify({ source: file, rows: rows.length, columns: header.length, uniqueIds: ids.size,
  idRange: [min[0], max[0]], blankNames, categories, numericCount, maxNumber: Math.max(...max), nonInteger,
  negativeColumns: header.slice(0, numericCount).filter((_, i) => min[i] < 0).map(name => ({name,min:min[header.indexOf(name)]})),
  transitions, spriteIdDifferences, samples: rows.filter(row => ['100','701','800'].includes(String(row[0]))), first: rows[0], last: rows.at(-1) }));
}
