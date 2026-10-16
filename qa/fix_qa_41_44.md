# Fix QA #41–#44 (from LIVE @ 25ab5cf)

## Tests
- EditMode: 35/35 Passed
- PlayMode: 5/5 Passed (incl. HireNonno_StartsWorkingWithin10Seconds)

## Changes
- #41 Ada ≤36dp one line ≤60 chars; PadTag/floating chips off; no pad/cassa cards
- #42 NONNO Transform-only (no NavMeshAgent); hire 10 LN; tutorial Nonno before Zaino; unlock 0; Hire test fails on NavMesh/Idle
- #43 thicker pad rims, fill α≈0.40, higher saturation
- #44 objective HUD top only; edge arrow OK; mid-screen marker/waypoints off

## Build
- `/workspace/um-recovery/build/WebGL/` (uncompressed)
- **Do NOT publish / do NOT run Build & Release until QA LIVE re-pass**

## Shots
- before: `qa/shots_before/before_1080x2400.png`
- after: `qa/shots_after/after_1080x2400_start.png`, `after_1080x2400_walk.png`
