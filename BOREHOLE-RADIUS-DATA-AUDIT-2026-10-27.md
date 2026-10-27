# Borehole-radius audit — 27 October 2026

The read-only, hash-verified Ullandhaug snapshot captured 26 October contains 29
calculated-station chunks with 1,618 station rows: 1,033 zero radii, 585 positive
radii, and no null radii. This is a sequential capture for one field, not an audit
of every deployed database or a transactional backup. Snapshot and hashes are in
`C:/OSDC/DrillWeaver/TestSamples/Snapshots/ullrigg-2026-10-26-octree-replay`.

The shared surveying `SurveyStation.BoreholeRadius` was nullable but defaulted to
zero in Surveying 1.3.6; version 1.3.7 defaults to null. The snapshot alone cannot
establish the provenance of each stored zero.
The user confirmed zero means missing for the U3 and U4 data. The U3 architecture
provides 23 radii in the captured station range; U4's station at 917.08 m has no
covering interval in its captured architecture. Three of the eight comparison
wellbores (U6A, U8, U5X) had no registered architecture in the 27 October
discovery capture. See the preparation snapshot and Stage 6 review in DrillWeaver.

Target landing now requires a positive source endpoint radius and carries it into
the projected lead and landing ellipse; null or legacy zero stops calculation.

No live database was inspected or changed by this audit. Do not migrate based on
these counts: first perform a scoped read-only audit of actual deployed trajectory,
survey-run, tie-in and calculated-station records, review zero semantics and
ownership, and verify independent application-level and database backups. If a
migration is later approved, test an idempotent transactional dry run and restore;
preserve provenance and distinguish records with missing architecture. Legacy
zero handling remains necessary after any migration because old clients and imports
can still supply zero.
