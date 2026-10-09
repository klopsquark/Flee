# Change rationale

Every change this fork makes to code, build or tests, relative to upstream `mparlak/Flee` at
`f3b4fe2` (tag `upstream-baseline`), with the reason for it and how it was verified.

This file is for maintainers and reviewers. The user-facing changelog and the migration notes
come later (plan, Phase 6) and are written from the entries here.

## How to add an entry

- One entry per change. A change is one commit, or a small series of commits of the same kind.
- Number entries consecutively (`R-001`, `R-002`, ...) and never renumber. Newest at the bottom.
- Add the entry in the same pull request as the change, and add a row to the index.
- **Kind** is one of: `build`, `test`, `cleanup` (no behaviour change), `fix` (behaviour change
  that corrects a bug), `api` (public surface changes), `perf`, `docs`.
- **Behaviour** says whether anything a library user can observe changes: `none`, or what changes.
- **Discussed** records the maintainer's decision when there was a real choice, or `not needed`.
- Documentation-only changes (README, plan, this file) do not need an entry.

Entry template:

```
### R-000: Short title

- **Kind / phase:** build / Phase 1
- **Commits:** abc1234
- **What:** What changed, in a few sentences.
- **Why:** The reason, and the alternatives that were rejected.
- **Behaviour:** none
- **Verified:** How: build, test run (counts), benchmark comparison, manual check.
- **Discussed:** Decision and date, or "not needed".
```

## Index

| Id | Title | Kind | Phase |
| --- | --- | --- | --- |
| R-001 | Shared build layout, packaging and versioning | build | before Phase 0 |
| R-002 | Licence file | build | Phase 0 |

## Entries

### R-001: Shared build layout, packaging and versioning

Recorded after the fact: these commits landed on `develop` before this file existed.

- **Kind / phase:** build / before Phase 0
- **Commits:** b12dc4c, 5b1b14f, e9d250b, f04c2a5, 40d041c, 3108048, 2b58e2f (merged in PR #1)
- **What:**
  - `Directory.Build.props` sets `LangVersion latest` for every project, defaults
    `Configuration` to Debug, and sends all output to `build/<project>/<configuration>/`
    (Flee keeps one subfolder per target framework).
  - NuGet packaging is opt-in per project; only `Flee` packs. Packages go to `C:\dev\nuget\`
    when it exists, else `build/nuget/`.
  - Shared package metadata (authors, company, copyright, repository URL) lives in
    `Directory.Build.props`; the project and repository URLs point at the fork.
  - Every build copies the Flee assemblies to `build/runtime/Flee/<framework>/` and writes a
    CycloneDX SBOM there (tool pinned in `.config/dotnet-tools.json`).
  - Versions come from GitVersion (`GitVersion.MsBuild` 6.4.0, `next-version: 2.6.0`) instead of
    the fixed 2.0.0.
- **Why:** Same layout as the maintainer's other repositories; SBOM for Cyber Resilience Act
  readiness; 2.6.0 keeps the fork's packages apart from Flee 2.0.0 on nuget.org and from a 2.5.1
  already in the local feed.
- **Behaviour:** none in the library. Assembly and package versions change from 2.0.0 to
  GitVersion's 2.6.0 series.
- **Verified:** Solution builds (0 errors); the test run is unchanged at 40 of 48 passing.
- **Discussed:** Requested by the maintainer (props file), October 2026.

### R-002: Licence file

- **Kind / phase:** build / Phase 0
- **Commits:** in the Phase 0 pull request
- **What:** Adds `LICENSE` with the unmodified text of the GNU LGPL version 2.1, as published at
  gnu.org.
- **Why:** Upstream states "LGPL 2.1 or (at your option) any later version" in the grammar
  header and the README, but ships no licence file. The licence itself does not change.
- **Behaviour:** none.
- **Verified:** Text compared with gnu.org's `lgpl-2.1.txt` (downloaded copy, 501 lines).
- **Discussed:** Licence kept as LGPL 2.1 or later, decided by the maintainer.
