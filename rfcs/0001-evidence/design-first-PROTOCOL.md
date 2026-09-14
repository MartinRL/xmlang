# Design-first run: protocol (pre-registered)

**Status: draft, unrun.** Every rule below is fixed before any design is opened. Changing a rule after the design is opened voids the run. Depends on `rfcs/xmlang-0002-frontend-architecture.md` (accepted 2026-09-14) for the extraction map, `rfcs/xmlang-0001-interaction-model.md` for the `confirm`/`then` candidates, and `rfcs/emlang-0002-decider-profile.md` for `s:` and the dialect lints. Nothing here changes `xmlang-spec.md`, `src/`, or the fixtures.

## What is being tested

Two claims, one design, one run.

| Claim | Question | Instrument |
|---|---|---|
| **Sufficiency** | Can xmlang v0.5 hold the experience judgments a design made *without* xmlang carries, minus declared non-goals? | Four-bin census (§6) |
| **Recovery** | How much of a dialect-valid Event Model (commands, views, events, states, folds, decision tests) can agent inference plus human confirmation recover from the design? | Provenance tags and recovery rate (§5) |

Neither is the five admission tests of `BRIEF.md` §"The five admission tests". Those test whether a key is *necessary*; this run tests whether the vocabulary is *sufficient* for an input the vocabulary's author did not produce. Every prior xmlang testbed was authored inside the vocabulary (`lob-ap.xm.yaml:1-5`: "using ONLY the current vocabulary"); that method finds only the gaps its author thought to want. This run answers `SYNTHESIS.md` §"Open objections", objection 2 ("one constructed testbed, never rendered") and fills the second-testbed slot in `rfcs/emlang-evidence/PLAN.md` §"Decisions (2026-09-10)".

## Two entry points, one protocol

em-first (an Event Model exists, the Experience Model is written over it, as `lob-ap` was) and design-first (a design exists, both models are read out of it) are two first-class entry points to the same pair of specs. Design-first is also an **authoring path**: stakeholders who struggle with events on a timeline can produce a design. The census in §6 and the provenance in §5 are defined so the same flow can be run either way and compared (§11).

## Input and roles

- **Design.** One Claude Design artifact built from the ChronosHub design system (link pending, RFC xmlang-0002 decision 4). One flow, B2B SaaS, scholarly publishing. Anonymise or report counts only per `PLAN.md` ("Second testbed").
- **Reader, not parser.** Extraction is one agent reading the artifact and one human deciding. No importer, no `xm import`.
- **Agent proposes, human decides.** Every element enters the model only on a human's yes. The human is ideally the designer or a subject-matter expert, with the maintainer as reviewer; if the maintainer is the human, the report says so as an inference limit (the maintainer knows how to bend the vocabulary).

## §4 Extraction rules

### 4a. Experience side: the RFC xmlang-0002 map, read backward

The substrate is closed (every component in the artifact is a design-system component), so extraction is a lookup. Component names are **extraction input only**: they are the reader's copy of RFC xmlang-0002's non-normative binding column (the reader is a transformer, and each transformer owns its own copy), and no component or framework name is ever written into the xm file. Families are generic until the inventory is linked; the run fills the first column from the real inventory and lists any family with no row as **map debt** (§7).

| Component in the artifact | xm construct | Note |
|---|---|---|
| Page / Modal / Drawer shell | `surfaces.<Name>`; heading text → `labels.<locale>.<Name>` | one surface per shell instance |
| visible order of Views then Tasks | `compose` order | importance, never position |
| Button `primary` / `secondary`; overflow-menu item | `- c: X` with `prominence: primary` / `secondary` / `overflow` | one Task = one command |
| DataTable, DescriptionList, card, tile | `- v: X` | one Query + View = one view |
| DataTable columns shown / muted text / behind Disclosure or drawer | `fields.primary` / `secondary` / `on-demand` | tiers, not positions |
| row highlight, "you" chip | `self:` | |
| StatusBadge | a `phase` value; if the surface exists only for some values → `during:` | also feeds 4b |
| EmptyState text | `labels.<locale>.<View>.$empty` | |
| ConfirmDialog on a Task | logged as `confirm: true` **candidate** | RFC xmlang-0001 §1, provisional; not written into the model |
| prototype link between shells | `journeys.<Name>.slices` step | |
| role-gated variant of a shell | `personas.<Role>` with `role:` = the em swimlane; `for:` on the surface | |
| any component with no row above | **map debt** entry (§7) | not an xm key |

### 4b. Domain side: inference rules for the em interior

The map says which components are commands and which are views. It says nothing about events, states, folds, or invariants. Those are read by inference, each proposal carrying a provenance tag (§5).

| Signal in the artifact | em candidate | Default provenance |
|---|---|---|
| Task (Button) | `c:` command, one slice `✍️ <Verb noun>` | observed |
| form fields on a Task | command `props` (name and inferred type) | observed |
| DataTable / DescriptionList columns | view `props` | observed |
| StatusBadge value set on one entity | `s:` state with `phase` enum listing exactly those values | inferred |
| a Task whose completion changes a StatusBadge | `e:` event, default name = past tense of the command; human renames | inferred |
| activity feed, history panel, notification, toast | `e:` event candidates (an activity feed is events on a timeline, rendered) | inferred |
| filters, tabs, search on a list | view `props` marked as parameters in a comment (`W-20`, an em debt) or state tags | inferred |
| error banner, inline validation text | `x:` exception on the Task's command | observed (text) / inferred (which command) |
| Task disabled or absent while StatusBadge = S | no success case for that command in phase S; if an explanation is shown, an `x:` | inferred |
| undo affordance | compensating command candidate; logged, not modelled (derivability row 9 is NO) | inferred |
| anything the human adds with no signal (server-side invariant, event prop not shown, fold query detail) | element as written | supplied |

**GWT skeleton rule.** For every command and every `phase` value of the state it acts on: Task enabled in that phase → one decision-test skeleton (`given: - s: <State> {phase: P}`, `when: - c: <Command>`, `then: - e: <Event>`); Task disabled or an error shown → one exception skeleton (`then: - x: <Exception>`). The human fills props and names. Each skeleton is tagged inferred. Folds (`given: [events]` → `then: - s:`) are written by the human for every state the skeletons name; each is tagged supplied unless an activity feed showed the exact event sequence.

## §5 Provenance and recovery rate

Every element and every test in the final em file carries one trailing comment:

```yaml
- c: ApproveInvoice        # prov: observed
- e: InvoiceApproved       # prov: inferred-confirmed
- s: Invoice               # prov: inferred-confirmed
  props: { phase: Phase (draft|submitted|approved|paid) }
```

| Tag | Meaning |
|---|---|
| `observed` | read directly off the artifact (button label, column name, badge value) |
| `inferred-confirmed` | proposed by a 4b rule, accepted by the human |
| `inferred-rejected` | proposed by a 4b rule, rejected by the human; listed in the census, absent from the model |
| `supplied` | added by the human with no signal in the artifact |

Recovery rate = (observed + inferred-confirmed) / (observed + inferred-confirmed + supplied), counted per element kind (`c:`, `v:`, `e:`, `s:`, `x:`, tests) and in total. Counting is one grep per tag. The **supplied** list, grouped by kind, is the measured answer to "what a design cannot tell you".

## §6 Sufficiency census and the pre-registered prediction

Every designer judgment found in the artifact (a component choice, an ordering, a tier, a gating, a piece of copy, a confirmation) gets one row and one bin:

| Bin | Meaning |
|---|---|
| (a) expressed | written into the xm file by a 4a rule |
| (b) default | coincides with the Event-Model-derived default (`xmlang-spec.md` §Defaults); xmlang is silent and right |
| (c) non-goal | geometry, copy beyond labels, design-system-wide behaviour (`xmlang-spec.md` §The geometry prohibition, §Non-goals) |
| (d) gap | a judgment xmlang cannot hold and that is not (b) or (c); logged as `W-n` (§7) |

Verdict is the table plus the threshold on (d) fixed in §12.

**Prediction, fixed now.** Bin (d) is a subset of the rows marked NO in `p1-derivability.md` §2: per-viewer enablement (row 5), destructive or irreversible (8), compensating command (9), grouping within a command (13), who *sees* a view (18), optimistic vs confirmed feedback (22), inline vs modal (23). If (d) contains only those, the constructed `lob-ap` testbed is confirmed by an artefact its author did not make. If (d) contains anything else, the constructed testbed missed a class of judgment; that class is the result and goes to the five admission tests.

## §7 Frozen vocabulary, W-n log, map debt

- **No xm key is added, renamed, or widened during the run.** No change to `XmModel`, `XmParser`, `XmLinter`, or the spec. This is the guard against the slope the repo's own review names (`p1-mbui.md` §"What fell off the cliff": "the frames are pixels"; Figma's conditionals "are the ones on the slope").
- Every bin (d) judgment is a `W-n` row in the census file, same five columns as `p1-lob.md` §2 (id · the judgment in the designer's words · why the vocabulary cannot hold it · derivable? · sense A/B/C), and a `# W-n` comment at the nearest place in the xm file.
- Every component family in the artifact with no row in §4a is a **map debt** row (family · what it seemed to mean · which model construct, if any, it should bind to). Map debt is RFC xmlang-0002's problem, not an xm key (its §"ChronosHub binding": "Anything missing is a design-system debt the run will surface, not an xm key").
- `confirm:` and `then:` candidates are counted, not written; they are evidence for RFC xmlang-0001's provisional keys and its pre-registered `then:` withdrawal rule.

## §8 One fact in two homes: precedence

A prototype link, an Event Model slice's terminal view, and RFC xmlang-0001's `then:` can name the same navigation fact and disagree (MBUI failure F2, `SYNTHESIS.md` §"Open objections").

- **em-first run:** the Event Model wins; the disagreement is a `then:` datum.
- **design-first run:** the Event Model was inferred, so a disagreement means either the inference was wrong (fix the em, tag the fix `supplied`) or the design holds a navigation judgment the default does not derive (bin (d) or a `then:` candidate). The human rules which; both outcomes are logged.

## §9 The TDD ladder, stated honestly

| Rung | Gate | Status today |
|---|---|---|
| Red 1 | `xm lint` reports `xm-dangling-ref` for every `c:`/`v:`/slice/role/phase the xm names | shipped |
| Green 1 | an em file with names and props resolves every reference | shipped |
| Red 2 | dialect lints (`em-given-not-one-state`, `em-state-without-fold`, `em-fold-shape`, …) fail on states without folds | **not implemented**: `s:` is a hard parse error (`rfcs/emlang-evidence/census-gwt-state.md` §"Parser") |
| Green 2 | every `s:` has a fold; every decision test gives exactly one state | as above |
| Unforced | one decision test per command, one projection test per view | the §4b GWT skeleton rule supplies these from the enablement matrix; a coverage lint (already an emlang debt) is written only if this run shows the rule leaves holes |

Bridge for Red 1 / Green 1 while `s:` does not parse: the em file is written in dialect form (`s:` is canonical since 2026-09-11) and `xm lint` is run against a mechanical shadow copy in which each `- s: X` becomes `- v: State / X`. One `sed` line, kept in the census file, deleted the day `EmParser` reads `s:`. The shadow copy is never edited by hand.

## §10 Read-back

When the models are complete, the designer or subject-matter expert is shown the xm file and the em file (or a plain rendering of them) and asked one question: "Is this your design?" Their corrections are logged as a fifth provenance event (`readback-changed`) and counted. This is the author-bias control and the first human-subject comprehension datum; the experiments program's stage 3 is agents-only, so the report states the inference limit (one human, not a sample).

## §11 Both ways on the same flow (optional)

If budget allows, the same flow is also authored em-first by someone who has not seen the design-first files, then both pairs are diffed. The diff is direct evidence on whether the entry point changes the model. Not required for the run to count.

## §12 Building the application (separate stage, gated)

Not part of this run. Domain half: `Emlang.Generators` emits records, deciders, and xUnit tests from the decision tests once `EmParser` reads `s:`, or from the shadow copy in v1.0.0 form. UI half: RFC xmlang-0002's conformance check and skeleton emitter do not exist; until they do, an agent building the UI tests the agent as much as the spec, so it runs under the experiments program's clean-room N-run method, with the design artifact as the known-answer key `lob-ap` never had.

## §13 Kill sentences

Fill the three numbers before the design is opened; they are the maintainer's.

- **Sufficiency dead weight.** If bin (d) is empty *and* bin (a) holds fewer than ⟨a/b-min⟩ of the rows in (a)+(b), xmlang said nothing the default did not already say for this design; a second design-first flow is not worth running for sufficiency.
- **Sufficiency gap.** If bin (d) holds more than ⟨d-max⟩ rows outside the §6 prediction, the constructed testbed missed at least one class; the run stops and the class goes to the admission tests before any second flow.
- **Recovery.** If the total recovery rate is under ⟨recovery-min⟩, the design-first authoring path does not stand on its own and needs an em-first pass anyway; report that as the finding.

## Outputs

All in `rfcs/0001-evidence/design-first/`:

| File | Content |
|---|---|
| `<flow>.xm.yaml` | the Experience Model, `W-n` comments in place |
| `<flow>.em.yaml` | the Event Model in dialect form, `# prov:` on every element and test |
| `<flow>-census.md` | §6 table with every row sourced to a frame or component; §7 `W-n` table; map debt table; `inferred-rejected` list; `confirm:`/`then:` counts; the shadow-copy `sed` line; the three §13 numbers and the verdict |
| `<flow>-readback.md` | §10 question, answers, and the `readback-changed` list |

Verification: `dotnet run --project src/xmlang/Xmlang.Cli -- lint rfcs/0001-evidence/design-first/<flow>.xm.yaml` against the shadow copy exits 0 at Green 1; every element line in the em file matches `# prov:`; every census row cites a frame.

## Decisions only the maintainer can make

1. Which flow, and anonymise vs counts-only.
2. Who the human in the loop is (designer / subject-matter expert / maintainer).
3. The three §13 numbers.
4. Whether to run §11.
5. Link to the design-system inventory (RFC xmlang-0002 decision 4), so §4a's first column is real component names before the run starts.

## Not doing

- No Figma or Claude Design importer, no new CLI verb, no changes under `src/`.
- No YAML added to `xmlang-spec.md` (every fenced block is a positional `SpecExampleTests` snapshot).
- No `s:` parser work as part of this run; the shadow copy is the bridge and is named as one.
- No second flow before the first has a verdict.
