---
title: "RFC emlang-0004: Lint rules, a non-normative appendix (draft)"
status: draft
created: 2026-09-09
targets: emlang spec 1.0.0, new appendix "Lint rules (non-normative)"; no schema change
depends: "RFC emlang-0002 for `em-phase-transition-uncovered` (phase per decider) and for the dialect severity of `em-actor-identity`; RFC emlang-0003 for the trigger rule it indexes; not independently acceptable (redteam.md R22)"
consumed by: xmlang RFC 0001, debts 3, 6, 7 and 8
---
# RFC emlang-0004: Lint rules (non-normative appendix)

**Status: draft.** Nothing here is applied to the upstream spec, to `src/`, or to the fixtures. This RFC is the artifact the maintainer decides on. It proposes one appendix to the emlang spec and is the single list of every lint the emlang RFC set introduces, with the count each rule fires today on the four evidence models. The appendix is non-normative: no rule in it makes a document non-conforming, every rule is a recommendation, and it assigns no `error` severity of its own (`redteam.md` R21); where the dialect adopts a rule as a base rule, RFC emlang-0002 assigns its severity and this index copies it.

## Summary

1. A new spec appendix, "Lint rules (non-normative)", with one row per rule: id, severity in upstream-shaped documents and in the dialect, the check in one sentence, and what a consumer does with it.
2. Four new rules: `em-phase-transition-uncovered` (scenario coverage), `em-actor-identity` (a prop-naming convention so events carry who acted), `em-view-prop-untraced` (a view prop with no demonstrated event origin; the origin half of Event Modeling's completeness rule, not the whole), and `em-param-note-malformed` with the `(@param)` props note for projection arguments.
3. An index of the rules introduced by RFC emlang-0002 and emlang-0003 and of the three reference rules, so the appendix is the one place a tool author reads.
4. No construct. Every rule reads existing free text: element names, `props` values, and tests.

## Motivation

The reference linter has three rules, all warnings, hardcoded at one severity (`Linter.cs:14-18`, `:92`; Go `linter.go`), and the upstream README documents ten rules of which the code implements three (`compat.md` §4). The rule list is therefore already non-normative and already ahead of the code; an appendix that names rules beyond the reference implementation has precedent in the reference repository itself.

xmlang's derived interaction stratum (RFC 0001 §4) rests on four facts emlang does not state: which command succeeds in which phase (RFC 0001 L184 keeps `xm-command-phase-mismatch` at info "until emlang has a scenario-coverage lint"), who acted (the selection default L135 and `self:`, XSPEC L145-151, must resolve against one prop), whether a view carries the datum a heading shows (RFC 0001 L121), and which view props are inputs rather than data (`SYNTHESIS.md:68`). These are debts 3, 6, 7 and 8 (`xmlang-0001-interaction-model.md:238, 241-243`); the design pass ruled all four lints or props conventions, not constructs (`cut-lines.md` §A E4, E6, §D).

## Proposed appendix text

### Conventions

- A rule id is a lowercase hyphenated token; rules introduced by this appendix carry the prefix `em-` to keep them apart from xmlang's `xm-` rules; the three reference rules keep their unprefixed ids.
- Each rule has a recommended default severity and a severity in the dialect (RFC emlang-0002 makes the decider rules the dialect default, not a switchable profile; the default column is what a rule costs a document written in the upstream shape); this appendix is non-normative, so tools SHOULD report at the listed severity, MAY let a document suppress a rule by id (the local `ignoreRules`, `Linter.cs:22`), and no rule here makes a document non-conforming.
- Severities are `off`, `info`, `warning`, `error` (`off` means the rule is not evaluated in that column); `error` appears only in rows copied from a normative RFC, never assigned here; a rule listed at `warning` in both columns fires on every known model today and is not to be promoted until a model exists with a clean baseline.
- A rule that spans slices reads the whole document; "document" means one YAML document, not the file.

### `em-phase-transition-uncovered`

- Severity: warning; in the dialect warning.
- Check: for each decider (an `s:` state element with a `phase` enum note, RFC emlang-0002; the fixtures still write `v: State / X`), for each command that has at least one scenario giving that decider's state, and for each declared phase value, the document has a test with that command in `when` and that state in `given` with `phase:` equal to the value; the rule fires per (phase, command) pair with no such test, success or rejection.
- The empty stream (`given: []`) is not a phase and is not counted; its shape is RFC emlang-0002's.
- Consumer: xmlang promotes `xm-command-phase-mismatch` from info to warning and derives enablement per phase from success scenarios only (RFC 0001 L136, L184); a modeller reads the matrix as the decider's specification and adds the rejection scenarios that say what a command does in a phase where it must not succeed.

Coverage census, mechanical (PyYAML script in the session scratchpad; `S` = a success scenario exists, `R` = only rejection scenarios exist, `-` = no scenario). The `(empty)` column is shown and not counted.

`rfcs/0001-evidence/lob-ap.em.yaml`, decider `State / Invoice` (`:1283`, enum `:1286`):

| command | (empty) | draft | submitted | onHold | approved | rejected | scheduled | paid | voided | discarded |
|---|---|---|---|---|---|---|---|---|---|---|
| EditDraft | - | S | R | - | - | - | - | - | - | - |
| DiscardDraft | - | S | R | - | - | - | - | - | - | - |
| SubmitInvoice | - | S | - | - | - | - | - | - | - | - |
| WithdrawSubmission | - | - | S | - | R | - | - | - | - | - |
| ReopenInvoice | - | - | - | - | - | S | - | - | - | - |
| ApproveInvoice | - | R | S | - | - | - | - | - | - | - |
| RejectInvoice | - | - | S | - | - | - | - | - | - | - |
| ApproveInvoices | R | - | S | - | - | - | - | - | - | - |
| HoldInvoice | - | - | - | - | S | - | - | R | - | - |
| ReleaseHold | - | - | - | S | R | - | - | - | - | - |
| SchedulePayment | - | - | R | - | S | - | - | - | - | - |
| ReversePayment | - | - | - | - | - | - | R | S | - | - |
| VoidInvoice | - | R | - | - | S | - | - | R | - | - |

13 commands, 9 phases, 117 pairs: 13 with a success scenario, 10 with rejection only, 94 with none. The matrix exposes two things: `DraftInvoice` (`:294`) is absent because both its scenarios give `State / Supplier` (`:324`, `:334`), a cross-decider given for RFC emlang-0002 to rule on; and `scheduled` shows no success for any command because its exit, `ExecutePaymentRun`, gives `Todo / Due payments` (`:933`), RFC emlang-0002's Todo-given smell, so a non-terminal phase reads as terminal.

`lob-ap.em.yaml`, decider `State / Supplier` (`:1263`): its lifecycle prop is `status`, not `phase` (`:1267`), so the rule does not apply today. Once RFC emlang-0002 lets it be named `phase`: 5 commands (including `DraftInvoice`), 2 phases, 10 pairs: 4 success, 3 rejection only, 3 none.

`tests/Emlang.Tests/fixtures/blindbudet.em.yaml`, decider `State / Game` (`:922`, enum `:926`):

| command | (empty) | lobby | started | ended |
|---|---|---|---|---|
| OpenAuction | S | - | - | - |
| JoinAuction | R | S | R | - |
| StartAuction | R | S | - | - |
| PlaceBid | R | - | S | - |
| RevealLot | - | - | S | - |
| AskNextLot | - | - | S | - |
| EndAuction | - | - | S | - |

7 commands, 3 phases, 21 pairs: 6 success, 1 rejection only, 14 none, of which 7 are the `ended` column.

`tests/Emlang.Tests/fixtures/mer-eller-mindre.em.yaml`, decider `State / Game` (`:1293`):

| command | (empty) | lobby | started | ended |
|---|---|---|---|---|
| OpenLobby | S | - | - | - |
| JoinGame | R | S | R | - |
| StartGame | R | S | - | - |
| SubmitDirection | R | R | S | - |
| SubmitDifference | - | - | S | - |
| RevealDirection | - | - | S | - |
| ScoreDifference | - | - | S | - |
| AskNextQuestion | - | - | S | - |
| EndGame | - | - | S | - |

9 commands, 27 pairs: 8 success, 2 rejection only, 17 none, of which 9 are `ended`.

`tests/Emlang.Tests/fixtures/tank-till-tusen.em.yaml`, decider `State / Game` (`:1011`, enum `:1015`):

| command | (empty) | lobby | started | ended |
|---|---|---|---|---|
| OpenLobby | S | - | - | - |
| JoinGame | R | S | R | - |
| StartGame | R | S | - | - |
| SubmitSolution | R | - | S | - |
| ScoreRound | - | - | S | - |
| AskNextPuzzle | - | - | S | - |
| EndGame | - | - | S | - |

7 commands, 21 pairs: 6 success, 1 rejection only, 14 none, of which 7 are `ended`.

Fires today: lob-ap 94 (97 once Supplier is a decider), blindbudet 14, mer-eller-mindre 17, tank-till-tusen 14. No model has a phase in which every command is specified; the terminal phase is unspecified for every command in all three games.

### `em-actor-identity`

- Severity: warning; in the dialect as RFC emlang-0002 assigns (the dialect severity is defined there, not here).
- Convention: an event produced by a slice whose trigger set (RFC emlang-0003 Section A) contains a human role carries exactly one actor prop, named either `<verb>By`, where `<verb>` is the participle in the event's own name (`InvoiceApproved` carries `approvedBy`), or `<role>Id`, where `<role>` is a normalized trigger role of the slice (`BidPlaced` carries `playerId`, `AuctionOpened` carries `hostPlayerId`).
- Role normalization: take the trigger's swimlane text, remove every leading character that is not a letter or digit (emoji, variation selectors, zero-width joiners, whitespace), trim, and lowercase what remains; `🧑‍🎓 Player` normalizes to `player`, `🧑‍🏫 host` to `host`, `🧾 Clerk` to `clerk`, `⚙️ System` to `system`.
- Prop matching: a prop is a `<role>Id` prop when its name, lowercased, starts with a normalized role of the slice's trigger set and ends with `id`; `playerId` matches `player`, `hostPlayerId` matches `host`, `invoiceId` matches no role.
- Resolution order for consumers: the unique prop ending in `By`; otherwise the unique `<role>Id` prop.
- Check: fires on an event in a human-triggered slice with no prop matching either form, and on one with two.
- An event in a `System`-only slice is exempt; a `<role>Id` prop there names a subject, not an actor (`RoundScored.playerId`, `blindbudet.em.yaml:540`), and consumers MUST NOT read it as one.
- Consumer: xmlang's selection default (RFC 0001 L135, "the viewer's own command's event") compares the event's actor prop with the identity that `self:` names on the composed view (XSPEC L147-149); both resolve against one identity value, which is debt 7. `em-actor-identity` is what makes "fired for her" a reference.

Census, mechanical over the props-richest occurrence of each event in steps. lob-ap: 19 events in human-triggered slices, 18 carry `<verb>By` (`approvedBy` `:577`, `heldBy` `:756`, `reversedBy` `:989`); the exception is `Ledger / LiabilityReversed` (`:1034`), a ledger posting in the Approver-triggered `✍️ Void Invoice` slice. The games use `<role>Id`: blindbudet 3 of 4 (`AuctionStarted` `:265` has none), mer-eller-mindre 4 of 5 (`GameStarted` `:339`), tank-till-tusen 3 of 4 (`GameStarted` `:270`). No model uses both forms. Fires today: 1, 1, 1, 1.

### `em-view-prop-untraced`

- Severity: info; in the dialect warning.
- Check: every prop declared on a view element in `steps`, other than a `(@param)` prop, is asserted in the `then` of at least one test of the document whose `given` is events and whose `then` is that view; the rule fires per prop never so asserted.
- Scope: Event Modeling's completeness rule is two-sided, "All information has to have an origin and a destination" (eventmodeling.org, quoted in `redteam.md`); this rule checks the origin half only, taking the projection test as the document's demonstration that a prop has an event origin. The destination half, whether a consumer receives every datum it shows, is not checkable from the emlang document and stays manual (below). The rule is not Event Modeling's completeness check and does not claim to be (`redteam.md` R19).
- Consumer: xmlang's label arguments (`xm-label-arg-missing`, RFC 0001 L102) and links default (L139) resolve against view props; an unasserted prop is one the transformer cannot rely on being populated.

Census, mechanical, never-asserted props excluding the projection-argument `(@param)` candidates below: lob-ap 48 across 11 of 13 views, of which 8 are the UI filter props the `(@param)` section removes from the model (40 after that edit; `Invoice details` `:1150` alone has 14, among them `supplierName`, `createdBy`, `bankReference`), blindbudet 11, mer-eller-mindre 13, tank-till-tusen 10. In the games 8 of each count is `gameId`, a routing id every fixture test omits; the lint makes that habit visible. A name-based proxy (view prop name appears on some event) fires 54, 14, 16, 17 and is dominated by derived aggregates (`hasDue`, `totalLots`, `pendingPlayerIds`), so the assertion form is the proposal.

The reverse direction, a datum a consumer needs that the view lacks, is not mechanical: the need lives in the xmlang label, so the count is manual. RFC 0001's instances: `Round scores` (`blindbudet.em.yaml:690-696`) lacks `description`, which `Lot card` carries (`:305-310`); `Direction reveal` (`mer-eller-mindre.em.yaml:989-995`) and `Round scores` (`:1027-1034`) lack `questionText`, which `Question card` carries (`:382-390`); `Bid progress` (`blindbudet.em.yaml:429`) carries id lists but not the counts its heading shows (RFC 0001 L121). Four views, all games, none on lob-ap.

### `(@param)` props note and `em-param-note-malformed`

Two kinds of non-projected view prop exist in lob-ap and only one belongs in emlang (`redteam.md` R20).

- Convention: a view prop whose value ends with the parenthesized note `(@param)` is a projection argument: an input the projection takes to compute its rows, such as the clock it reads (`asOf`) or the identity it is projected for (`viewerId`, `approverId`); it is a domain fact because the same events project differently under a different argument.
- UI filters are not projection arguments: a prop that records what the user asked to see (`phaseFilter`, `supplierFilter`, `dueBefore`, `query`, `statusFilter`, a date range) is screen state, belongs to the surface, and SHOULD NOT be declared on an emlang view; this RFC proposes no emlang construct for it and records that it is xmlang's or the transformer's.
- The note MUST be the last parenthesized note of the value, so an enum note may precede it: `mode: RunMode (dry|live) (@param)`.
- The note MUST appear only on view elements in `steps`; test props are fixture values and carry no notes (`EmParser.cs:46-48`).
- `@param` MUST NOT appear in a props value outside a parenthesized note.
- Check (`em-param-note-malformed`, warning in both columns): fires on `@param` outside a note, on a `(@param)` note that is not last, and on a `(@param)` note on a non-view element or inside a test.
- Consumer: xmlang never tiers a projection argument (a tier is a salience judgment on projected data), MAY label it, MAY name it in `self:` when it is the viewer's identity, and `em-view-prop-untraced` excludes it.

```yaml
# proposed
  📋 Due Payments:
    steps:
      - v: Todo / Due payments
        props:
          asOf: DateOnly (@param)
          due: DuePayment[]
          hasDue: bool
  👀 Outstanding Invoices:
    steps:
      - v: Outstanding invoices
        props:
          viewerId: Guid (@param)     # projected for this viewer; also what xmlang self: names
          # projected rows follow, unchanged
```

Rejected: the bare suffix `asOf: DateTimeOffset @param`. It round-trips through both formatters (`compat.md` §1) but `SpecModel.MapType` strips only a parenthesized note (`SpecModel.cs:115-128`), so the generated record gets the C# type `DateTimeOffset @param` and does not compile. The parenthesized form is stripped by `StripParenthesizedNote` (`:124-128`) and is not an enum because `IsEnumNote` requires a `|` (`:101-105`); `EmParser.EnumValues` reads only the first note (`EmParser.cs:153-159`), so an enum note before `(@param)` still resolves.

Projection arguments today, manual, all on lob-ap: `approverId` `:713`, `asOf` `:887`, `asOf` `:1079`, `viewerId` `:1190`, `asOf` `:1217`; five props on five views (`asOf` at `:1100` is a command prop, out of scope). UI filters present today, to leave the model under this convention: `query` `:64`, `statusFilter` `:65`, `fromDate` `:958`, `toDate` `:959`, `phaseFilter` `:1126`, `supplierFilter` `:1127`, `dueBefore` `:1128`, `query` `:1129`; eight props on three views. Games: none of either kind. `em-param-note-malformed` fires 0 today.

### Reference rules and the decision-model slice

`slice-missing-event` fires 13 / 9 / 11 / 9 today, once on every `👀` and `📋` slice, the Decision Model slices included (`em lint` 0.3.0). Upstream's README also lists `test-missing-command` at error, "Test without command (when)", which `linter.go` does not implement (`compat.md` §4) and which would condemn every fold and projection test. RFC emlang-0002 makes the Decision Model slice and its fold tests mandatory, so the profile would require an artefact upstream's documented lints condemn (`redteam.md` R12). Recommendation, coordinated with RFC emlang-0002's subsection on upstream's `test-missing-command`: exempt from `slice-missing-event` any slice whose steps contain no command element, since a view slice has no event by construction; and retract `test-missing-command`, or exempt tests without `when` whose `then` is a view (fold and projection tests). Baseline after both: 0 / 0 / 0 / 0.

### Index of all rules

Counts are fires today per model in the order lob-ap / blindbudet / mer-eller-mindre / tank-till-tusen. Reference counts are from `em lint` 0.3.0 on 2026-09-09; RFC emlang-0002 rows copy its own table (`emlang-0002-decider-profile.md:352-359`), whose columns are dialect first, default second; its `em-given-todo` fires are cited here at the element lines `:933, :945, :1110` (its §4), not the test-name lines its table uses (`redteam.md` R23).

| rule | source | default | dialect | fires today | consumer |
|---|---|---|---|---|---|
| `command-without-event` | reference (`Linter.cs:53-56`) | warning | warning | 0 / 0 / 0 / 0 | shape of a slice |
| `orphan-exception` | reference (`:59-62`) | warning | warning | 0 / 0 / 0 / 0 | shape of a slice |
| `slice-missing-event` | reference (`:65-67`) | warning | warning | 13 / 9 / 11 / 9, one per `👀` or `📋` slice | exempt command-less slices (recommendation above, with RFC 0002) |
| `em-given-not-one-state` | RFC 0002 | off | error | 8 (`lob-ap:685` three states; `:931`, `:943`, `:1108` Todo only; 4 empty givens) / 5 / 7 / 6 (the 22 empty givens) | generator emits the given state, never a fold; replaces `em-given-events-in-decision`, `em-given-mixed`, `em-given-multi-state` |
| `em-fold-shape` | RFC 0002 | off | error | 0 / 0 / 0 / 0 | a fold has events and no `when` |
| `em-then-outside-query` | RFC 0002 | off | error | 10 / 3 / 3 / 3 | the append condition covers every emitted type; replaces the aggregate rule `em-given-decider-mismatch` |
| `em-fold-untagged-event` | RFC 0002 | off | error | 0 / 0 / 0 / 0 | query tags are the state's identity props |
| `em-state-without-fold` | RFC 0002 | off | error | 0 / 0 / 0 / 0 | xmlang `during` trusts the fold |
| `em-state-phase-without-fold` | RFC 0002 | off | error | 17 / 4 / 5 / 4 | every pinned phase value is produced by a fold |
| `em-given-todo` | RFC 0002 | off | warning | 3 (`lob-ap:933, 945, 1110`) / 0 / 0 / 0 | coverage matrix above reads the transition |
| `em-trigger-after-command` | RFC 0003 | warning | warning | 0 / 0 / 0 / 0 | trigger set attaches to the right command |
| `em-phase-transition-uncovered` | this RFC | warning | warning | 94 / 14 / 17 / 14 | `xm-command-phase-mismatch` to warning |
| `em-actor-identity` | this RFC | warning | per RFC 0002 | 1 / 1 / 1 / 1 | selection default and `self:` share one prop |
| `em-view-prop-untraced` | this RFC | info | warning | 48 / 11 / 13 / 10 (40 / 11 / 13 / 10 once the UI filters leave lob-ap) | label arguments and links resolve against traced props |
| `em-param-note-malformed` | this RFC | warning | warning | 0 / 0 / 0 / 0 | xmlang never tiers a projection argument |

## Changelog entry (draft, for the emlang spec)

### Appendix: Lint rules (non-normative)

- **Added** the appendix and its conventions (ids, two severity columns, suppression by id).
- **Added** `em-phase-transition-uncovered`, `em-actor-identity` (with role normalization), `em-view-prop-untraced`, `em-param-note-malformed`, and the `(@param)` props note for projection arguments; UI filters are recorded as surface state outside emlang.
- **Indexed** the rules of RFC emlang-0002 and emlang-0003 and the three reference rules with their present severities; recommended exempting command-less slices from `slice-missing-event` and retracting upstream's `test-missing-command`, with RFC emlang-0002.

## Migration

None required; every rule reads documents that are valid today and no document becomes invalid. Baselines on the four models are in the index. A team adopting the appendix on an existing model suppresses `em-phase-transition-uncovered` by id until the matrix is filled, or accepts the count. On lob-ap, marking the five projection arguments `(@param)` is five one-token edits, and moving the eight UI filter props out of the views is eight deletions that take `em-view-prop-untraced` from 48 to 40.

## Implementation notes (reference implementation)

- `LintSeverity` has `Warning` and `Error` (`Linter.cs:3`); `Info` is needed. Severity is fixed at `Add` (`:87-93`); the dialect column needs a severity table keyed by rule id.
- `Linter.Lint` is a per-slice pass (`:27-29`); the three document-spanning rules need a pass over `EmDocument` before the slice loop.
- `em-phase-transition-uncovered` needs enum values per decider; `EmParser.PhaseValues` (`EmParser.cs:145-151`) computes one union today, which RFC emlang-0002 changes.
- `EmField` (`EmParser.cs:8`) gains an `IsParam` flag; `SpecModel.MapType` needs no change. Adding fields to `EmSpec` or `EmField` re-approves `ApprovalTests.EmSpecShape.verified.txt`.
- Role normalization is shared with xmlang: the red team reports that `XmLinter.cs:38` compares persona `role` to lane text verbatim (`redteam.md` R18, not re-verified here), so the same strip-and-lowercase rule must apply on both sides or the trigger-set comparison fails on every emoji-prefixed role.
- `Linter.cs:14-18` declares the set a faithful port of the Go reference; each rule here is a divergence to negotiate, with the upstream README's ten listed rules against three implemented as precedent that the list may lead the code.

## Non-goals

- `params:` as a schema key beside `props`. It is breaking for every v1.0.0 validator (`compat.md` §1, §2.3) and has one consumer. Re-open condition: a second consumer beyond xmlang needs to distinguish inputs from data without reading props notes.
- Any rule that requires a new element kind, including a state element or a processor element.
- `compensates:` or `reverses:` (deferred by PLAN.md; xmlang derives nothing from it).
- A rule that reads an xmlang document; the reverse-direction completeness check stays manual for that reason.
- A severity policy beyond the two columns; per-team severity overrides are tool configuration.
- Assigning `error` to any rule; that is a normative RFC's job (RFC emlang-0002 for the dialect).
- An emlang construct for UI filters; they are surface state.

## Open objections (recorded, not resolved)

1. Every count rests on four models by one team, and both actor conventions were written by that team, so "restates practice" is evidence of one practice.
2. `em-phase-transition-uncovered` fires 94 times on the LOB model and 14 to 17 on each game today; a warning that fires on every existing model may train users to ignore it, and the appendix has no promotion path for it.
3. The coverage rule counts terminal phases (7 to 9 per game are `ended`) and cross-decider givens (`DraftInvoice`) in ways a modeller may find pedantic; the matrix is honest, the rule's boundary is a judgment.
4. `em-view-prop-untraced` fires mostly on `gameId` and on `Invoice details`; both mechanical proxies are noisy, the proposed form depends on modellers asserting every prop in projection tests, and it covers the origin half of a two-sided rule.
5. The `(@param)` note is invisible to the schema and silently ignored by tools that do not know it (`cut-lines.md` §E); the re-open condition for a key is stated, not met; and the argument-versus-filter line (`fromDate`/`toDate` fell on the filter side here) is a judgment per prop.
6. Origin resolution left emlang on 2026-09-11 (RFC emlang-0003 Rejected alternatives); it is `xm-origin-mismatch` in xmlang RFC 0001 and no longer indexed here.
7. The `em-` prefix diverges from the unprefixed reference ids.
8. Because the appendix is non-normative, nothing in it is enforceable; a team that ignores every rule is conforming, so the baselines above measure only what a team chooses to look at.

## Evidence

`rfcs/emlang-evidence/{PLAN,cut-lines,census-gwt-state,compat,redteam}.md` (redteam R12, R18-R23); `rfcs/emlang-0002-decider-profile.md` L255-261; `rfcs/xmlang-0001-interaction-model.md` L102, L121, L132-139, L172-185, L232-243; `rfcs/0001-evidence/p1-derivability.md` §2 rows 4, 6, 7, §3, §4a; `rfcs/0001-evidence/SYNTHESIS.md` L35, L43, L66-73; `xmlang-spec.md` L111-119, L145-151; upstream `SPEC.md` L81-86, `schema.json` L222-225; the four models at the lines cited; `src/emlang/Emlang/EmParser.cs:8, 46-48, 145-159`, `SpecModel.cs:101-105, 115-128`, `Linting/Linter.cs:3-8, 14-18, 22, 27-29, 53-67, 87-93`; `em lint` 0.3.0 output on the four models, 2026-09-09; the census scripts `census.py` and `census2.py` in the session scratchpad.
