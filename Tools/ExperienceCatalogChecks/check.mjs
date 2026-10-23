import fs from 'node:fs/promises';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
const require = createRequire(pathToFileURL(`${process.argv[2]}/package.json`));
const { Workbook } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')));

const csv = await fs.readFile('분석/Saver_1.2/catalogs/experience_progression.csv', 'utf8');
const workbook = await Workbook.fromCSV(csv.replace(/^\uFEFF/, ''), { sheetName: 'Experience' });
const rows = workbook.worksheets.getItemAt(0).getUsedRange().values;
const header = rows.shift();
const expected = ['jobId', 'jobName', 'level', 'requiredXp', 'nextJobId', 'rewardGold', 'rewardXp', 'learnedMagic', 'usableSkills'];
if (JSON.stringify(header) !== JSON.stringify(expected)) throw new Error('Unexpected header: ' + JSON.stringify(header));
const keys = new Map(), jobs = new Set(), previous = new Map();
let maxXp = 0, maxRecord, duplicateKeys = 0;
for (const row of rows) {
  if (row.length !== 9) throw new Error('Invalid column count');
  for (const i of [0, 2, 3, 4, 5, 6]) if (!/^\d+$/.test(String(row[i]))) throw new Error('Invalid number');
  const [job, , level, xp] = [Number(row[0]), row[1], Number(row[2]), Number(row[3])];
  const key = `${job}:${level}`;
  if (level < 1 || level > 100) throw new Error('Invalid level');
  if (keys.has(key)) {
    if (keys.get(key) !== JSON.stringify(row)) throw new Error('Conflicting duplicate: ' + key);
    duplicateKeys++;
  }
  keys.set(key, JSON.stringify(row)); jobs.add(job);
  if (xp > maxXp) { maxXp = xp; maxRecord = { job, level, xp }; }
}
const ordered = [...rows].sort((a,b) => Number(a[0])-Number(b[0]) || Number(a[2])-Number(b[2]));
for (const row of ordered) {
  const job = Number(row[0]), xp = Number(row[3]);
  if (previous.has(job) && xp < previous.get(job)) throw new Error('XP decreases');
  previous.set(job, xp);
}
console.log(JSON.stringify({ rows: rows.length, uniqueKeys: keys.size, jobs: jobs.size, maxRecord, identicalDuplicateRows: duplicateKeys, decreasingXp: 0 }));
