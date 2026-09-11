---
title: "emlang RFC 0001: Profiles and the emlang document header (withdrawn)"
status: withdrawn
created: 2026-09-09
withdrawn: 2026-09-11
targets: "none (was emlang spec 1.1.0 upstream; local Emlang 0.4.0)"
---
# emlang RFC 0001: Profiles and the `emlang:` document header

**Status: withdrawn (2026-09-11).** Nothing here is applied anywhere. The draft proposed one optional root key, `emlang: { version, profile }`, so that a document could declare the spec version it was written against and opt into a stricter rule set (`profile: decider`) while remaining a valid upstream v1.0.0 document once the header was stripped. The full draft is in git history (`git show 03de7f5:rfcs/emlang-0001-profiles.md`).

## Why withdrawn

1. **Red team R1** (`rfcs/emlang-evidence/redteam.md`): emlang already ships a marked per-project configuration, `.emlang.yaml` with `lint.ignore` and `fmt.keys` (Go `internal/config/config.go`, ported at `src/emlang/Emlang.Cli/Program.cs:63-71`). A `lint.profile` key there delivers every lint in RFCs 0002 to 0004 with no schema change and no document failing any tool. The root key only bought document portability, and the draft never weighed the config file against it. The maintainer accepted this on 2026-09-11.
2. **The decision that made the question moot.** On the same day the maintainer made `s:` the canonical state element (RFC 0002, decision 2 in `rfcs/emlang-evidence/PLAN.md`). A sixth element kind is a grammar change; no v1.0.0 tool reads a document that carries it, and no header or config key can make it one. There is no v1.0.0 document left to mark with a profile, so the profile mechanism has nothing to do.

## What replaces it

- The decider rules (RFC 0002 sections 1 to 7) are the dialect's base rules, not a switchable profile. Every document the dialect's tools read is subject to them.
- Upstream v1.0.0 compatibility is no longer a goal of the dialect (RFC 0002, Non-goals). The dialect is a fork of the upstream grammar, hosted in `src/emlang`.
- No `.emlang.yaml` `lint.profile` key is added either: with the rules as defaults there is nothing to switch. Severity overrides remain what `lint.ignore` already does.

## Pointers

- RFC 0002, `rfcs/emlang-0002-decider-profile.md`: the state element, the decider rules, the lints, and the rejected form A.
- `rfcs/emlang-evidence/PLAN.md`: decisions of 2026-09-09, 2026-09-10 and 2026-09-11.
- xmlang RFC 0001, `rfcs/xmlang-0001-interaction-model.md`, open objection 3, which this RFC was drafted to answer; RFC 0002 answers it instead.

## Evidence

- Upstream text quoted above: `SPEC.md` sections "Overview", "Document Structure", "Comments"; `schema.json` root (lines 6-13); Go `internal/config/config.go:10-25` and `README.md` "Configuration" (fetched 2026-09-09 from `emlang-project/emlang` main).
- Compatibility runs against schema, Go `emlang` 1.0.0, `em` 0.2.0/0.3.0 and the codegen path: `rfcs/emlang-evidence/compat.md` (sections 1, 2.1, 3, 5; probe files `p1-rootkey`, `p2-comment`).
- Red team: `rfcs/emlang-evidence/redteam.md` R1, R2, R3, R7, R22 (applied), and "The single strongest objection" (recorded above).
- Local parser, formatter and CLI: `src/emlang/Emlang/Linting/EmAst.cs`, `src/emlang/Emlang/Linting/EmFormatter.cs`, `src/emlang/Emlang/EmParser.cs`, `src/emlang/Emlang/Linting/Linter.cs`, `src/emlang/Emlang.Cli/Program.cs`.
- Design pass and plan: `rfcs/emlang-evidence/cut-lines.md` (sections A, C, D), `rfcs/emlang-evidence/PLAN.md` (Decisions, Final cut).
- The debt this pays: xmlang `rfcs/xmlang-0001-interaction-model.md`, "Open objections" item 3.
