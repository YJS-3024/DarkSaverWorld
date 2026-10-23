# Experience catalog checks

Read-only CSV audit using the bundled Node.js and `@oai/artifact-tool` runtime.
Run from the project root, passing the bundled Node dependency directory:

`node Tools/ExperienceCatalogChecks/check.mjs <bundled-node-directory>`

Checks headers, integer values, level bounds, conflicting duplicate keys and
non-decreasing required XP. A leading UTF-8 BOM is removed in memory only.
The identical source duplicate at job 163 / level 35 is retained and reported.
No CSV, workbook or game asset is written by this check.

Native conversion is a separate Unity menu command:
`DarkSaver/Convert/Experience Progression`.
It creates `ExperienceProgression.asset` once and refuses to overwrite changed data.
The table is data/lookup only, not an automatic leveling or XP distribution system.
