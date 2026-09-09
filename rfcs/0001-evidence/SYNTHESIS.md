# Synthesis: the Interaction Model question, decided

Decision record after Phases 1–3. Where the red team (p3-redteam) and an analyst disagree, the red team's re-count on the real `lob-ap` testbed wins, because it measured the same file for every candidate. Implementation cost and version mechanics come from p3-impact.

## The headline

**There is no Interaction Model stratum to add to xmlang.** There is a *derived* one: a set of navigation and enablement facts that follow from the Event Model and that the spec should state as Defaults, so that every transformer computes them the same way. What a designer adds on top of the derived stratum is small: one friction bit per command (`confirm`), one return-to-worklist name per command (`then`), and copy. Everything else the literature calls an interaction judgment turned out to be one of: (a) the Event Model itself (dialog, enablement, drafts, undo pairing, reasons); (b) geometry in disguise (posture, modal/inline, wizard containers, master-detail arrangement, headings-as-regions); (c) a design-system-wide default rather than a per-action judgment (message channel, hide-vs-disable, loading thresholds); or (d) copy (`labels`).

This is the thesis of the research article confirmed on new ground. What survived thirty years of MBUI was a convention-default (Rails `redirect_to`), one flat attribute (`hx-confirm`), and the behaviour model as the state × event table (statecharts). That is, item for item, what this inquiry admits.

## The LOB reframe and what it changed

The user clarified mid-inquiry that xmlang targets line-of-business enterprise software; the three kvissig games are a negative control. A 33-slice accounts-payable Event Model and matching Experience Model (`lob-ap.em.yaml`, `lob-ap.xm.yaml`, valid under `em parse` and `xm lint`) became the dead-weight testbed. Effect: `confirm` and `then` pass test 2 only on LOB and are correctly absent from all three games; entry-per-persona and wizard grouping still fail; a `during` namespace defect appears only with two deciders.

## Verdicts

### Admitted to xmlang (v0.6.0, additive, non-breaking)

**K2 `confirm: true|false`** on a command item. Test 2 on lob-ap: 4 of 21 composed commands (DiscardDraft, VoidInvoice, ReversePayment, DeactivateSupplier); 2 of the 4 are not derivable even with an emlang `compensates:`, because the designer's reason is consequence to others, not irreversibility. Six enterprise design systems record confirmation per action (Fiori `IsActionCritical`, Carbon danger modal, Salesforce `lightning-confirm`, Fluent alert dialog, Redwood destructive dialog, Atlassian warning). Games: 0/0/0, the control passing. Form of assent is transformer-defined. **Killed**: the compensates-derived "no confirm" default and `xm-confirm-compensable` lint (they would nag on the genuine cases). **Kept**: `xm-confirm-habituation` (warning). **Non-goal**: an `impact:` tier or any placement/style axis on confirmation; pre-registered revert if a second LOB model needs two grades of confirm on one surface.

**K1 `then: <Surface>`** on a command item, **amended**. Red team re-count on lob-ap: 2 of 21 (Submit → ClerkHome as a proxy for "back to the worklist"; Void → ApproverHome), not the 4 of 9 sense A found on its own model. Decisive attack: the real modeller wrote destinations into each slice's terminal `v:` (Approve ends in `Approval queue`, Discard in `Invoice list`), so `then` is a second home for the same fact (MBUI F2). Admitted only with: (i) `then` is a *return*, never an advance: the advance is the Event Model's terminal view; a `then` that names the surface composing the terminal view restates the default; (ii) `then` MUST apply to every persona of the composing surface (`xm-then-persona-mismatch`, error); (iii) `xm-then-restates-default` (info) as the dead-weight instrument, once slice chains are available; (iv) Non-goals: conditional destinations (`then-on-reject`), `next`/instance destinations ("advance to the next pending" is a worklist view or a command, never a destination value); (v) tripwire: if a second LOB model shows terminal `v:` and `then` systematically disagreeing, the fix is the Event Model and the key is withdrawn. Status: **provisionally admitted pending a shipped LOB transformer run**.

**Labels extension** (the largest practical gain, and it is copy, not interaction):
- Exception (`x:`) names become labelable keys, string form. 25 distinct exceptions on lob-ap; 9/16/10 hand-written `Describe` strings on the three games move into the model. Zero new reserved keys.
- `$confirm` (command level): the assent question for `confirm: true`.
- `$values` (field level): labels for declared enum values (`onHold` → "On hold"); a value not in the enum note is `xm-orphan-label`.
- ICU MessageFormat arguments in label strings, **grammar capped by lint to `{arg}` and `{arg, plural, …}`** (`select` is a conditional, F1). Arguments resolve against scalar fields of the labelled element or, for surface labels, of the surface's composed views; `xm-label-arg-missing` (error).
- **Surface headings are ICU surface labels with field arguments.** This replaces the `heading:` key sense C proposed. `InvoiceDecision: "Approve {invoiceNumber} from {supplierName}"`, `Budgivning: "{description}"`. Covers every heading in all four testbeds, adds no structural key, keeps headings where the spec already puts them ("a surface's on-screen heading is its label").

**Defaults sub-table: "Navigation and enablement defaults"** (zero vocabulary; states what every interpreter and probe already computed identically):
- D-entry: a persona's entry surface is the first surface in document order applicable to the persona with no `during`; a persona with none enters via the bare form of its first triggered command.
- D-menu: a persona's global navigation is the set D-entry ranges over, in document order; `during`-bound surfaces are reached via destinations and links; a terminal-phase surface offers return to entry.
- D-destination: after an accepted command, the surface applicable to the viewer's persona composing the terminal view of the command's slice, in the new phase; if none or if it is the issuing surface, stay; a rejected command re-presents the issuing surface. `then` overrides.
- D-selection (the "inexpressible" selector, corrected): when `during`×`for` yields one `during`-bound surface per persona per cell (the LOB shape), that surface. When a cell holds several (synchronous multi-actor products: the games, parallel approvals), the viewer stands on the terminal view of the slice whose event fired last *for her*: her own command's event, a System event, or another persona's event advance her; a same-persona peer's event does not. Ties broken by offerable commands, then declaration order. This reproduces all three shipped selectors and all three clean-room probes. Non-normative note explaining the derivation. The `during` section's "deliberately NOT expressible … hand-written selector" wording is corrected; the `when:` Non-goal is rewritten: rejected because *redundant*, not merely dangerous.
- D-enablement: a composed command is rendered only to personas whose `role` matches one of its trigger roles; within a surface, a command the Event Model shows would be rejected for this viewer in the current state independent of typed input is rendered disabled with the blocking exception's label, never hidden by state alone. Input-validation and concurrency rejections are reported after the attempt. Note: ships together with emlang E2 (host triggers), or Next/End vanish from the games' host surfaces.
- D-required: a command prop is required iff some rejection scenario rejects its absence.
- D-bulk: a command whose prop is a list of a composed view's item identity is offered over the selection.
- D-links: a view field typed as another view's identity links to the surface composing that view for the persona (Dymitruk's information completeness).
- D-unidentified: a viewer resolving to no persona sees only surfaces with no `for:`, commands withheld, and is routed to the bare join/entry form where one exists.

**`during` namespace fix (R5)**. The spec defines `during` against "the system's lifecycle"; with two deciders (Invoice and Supplier) `EmParser.PhaseValues` unions every `State /` view's `phase` enum and the second decider had to avoid the word `phase`. Redefine `during` against the lifecycle of one decider; accept a map form `during: { Invoice: [draft, rejected] }` beside the list form; `xm-ambiguous-phase` (error) when a bare value exists in two deciders; `xm-phase-uncovered` per decider. Additive.

**Lints**: `xm-then-persona-mismatch` (error), `xm-then-restates-default` (info), `xm-confirm-not-boolean` (error), `xm-confirm-habituation` (warning), `xm-label-arg-missing` (error), `xm-command-trigger-mismatch` (warning; fires 6× on the games, 0 on lob-ap), `xm-command-phase-mismatch` (**info** until emlang has a scenario-coverage lint; it fires on coverage gaps today), `xm-ambiguous-phase` (error), `xm-cell-ambiguous` (info).

### Deferred, not dead

**Status criticality per enum value** (`negative|critical|positive|neutral|information`, Fiori `Criticality`, Carbon status indicator; 4 design systems). A closed semantic tier, not copy; not derivable; passes all five tests at n=1 (`InvoicePhase`). A token-naming convention (`color.status.<value>`) does the work today. Re-open when a second LOB model wants a semantic state the token path cannot carry.

### Rejected, with Non-goals text so they stay rejected

- **A `when:` predicate language** — redundant: fine selection is derived (D-selection); anything else it would express is a missing phase value, a missing command (an intent with no event), or a decider guard.
- **Entry surface per persona** — document order expresses it (D-entry); lob-ap as written got it wrong for 2 of 4 personas and a reorder fixed all 4. The cost is that the judgment is positional; accepted, it is the spec's own "order = importance" doctrine.
- **Wizard steps / command prop grouping** — a multi-step interaction with intermediate state is slices (draft + edit); chunking one command's props is concrete; groups need names, so the first instance demands a new labelable element (slope). Six design systems name it, but what they name is container choice (modal / tearsheet / page, step thresholds).
- **`next` / instance destinations** — "advance to the next pending" is a worklist view or a command; never a destination value; presupposes the launching list (slope).
- **`heading:` as a key** — dead weight (identical on all 12 `Invoice details` compositions), a one-field `slot: header`, and the first surface wanted `subheading:`. Folded into ICU surface labels.
- **Posture** (read-only/editable, sovereign/transient) — editable iff the surface composes a command; Cooper's postures are containers.
- **Presentation modality** (modal, drawer, inline, page edit) — placement.
- **Undo pairing** — which command compensates which is behaviour; emlang.
- **Per-command hide/disable** — hide-by-role is `for:` plus D-enablement; disable-by-state is D-enablement; a switch restates a design-system-wide rule.
- **Optimistic vs confirmed feedback, polling, message channel, loading thresholds** — timing and transport, already Non-goals; message channel is fixed once per design system in all seven systems surveyed.
- **Confirmation tiers / `impact:`** — a rendering of consequence; `prominence: overflow` plus `confirm: true` is the strong form.
- **Related-surface edges, topology enums, master-detail arrangement, site IA across models** — id-lineage derives the relation; arrangement is geometry; whole-product archetypes are a graph property.

### Belongs in emlang (debts named, not solved here)

1. **Trigger origin resolves to a view** (`t: Role /View` as a reference or a declared bare form). 18/18 resolve on lob-ap, 1/12 on the games (`Lot` vs `Lot card`). Makes destination, source, entry and topology derivations references instead of heuristics.
2. **True trigger sets**: `t: host /Round results` beside `t: ⚙️ System / Next lot` where the shipped UI gives the host a button. All three games' Event Models say System; all three UIs say host.
3. **View parameters**: mark `phaseFilter`, `query`, `asOf` as inputs to a projection; every xmlang tier on them today is a category error.
4. **Phase per decider**: a multi-decider model must not force the second decider to avoid the word `phase`.
5. **`compensates:` / `reverses:`** on domain merits (Void↔Post-style pairs); xmlang MUST NOT derive `confirm` from it.
6. **Scenario coverage lint** (`em-phase-transition-uncovered`): without it `xm-command-phase-mismatch` is noise.
7. **Actor identity on events** as a stated convention, so D-selection's "fired for her" and `self:` resolve against the same prop.
8. **Information completeness on result views** (games): `Round scores` lacks `description` / `questionText`; ICU heading arguments cannot resolve until the view carries the datum.

## The three objections the article must state honestly

1. **Dual specification of destination.** The only LOB modeller we have wrote navigation into the slice's terminal `v:`; `then:` is a second home for the same fact, and "return, never advance" is a rule in prose, exactly where Cameleon's transition rules and Figma's `Navigate to` stood before they grew conditions.
2. **One constructed testbed, never rendered.** Every LOB count rests on `lob-ap.*`, written by this team for this inquiry; counts moved by a factor of two between the analysts' own models and the real file. No transformer has produced lob-ap's default experience, so there is no "live design equals default" proof of the kind that settled the geometry experiment. `confirm` and `then` are provisionally admitted; `xm-confirm-habituation` and `xm-then-restates-default` are the dead-weight instruments.
3. **Derivability is borrowed from conventions emlang does not own.** The Defaults rows rest on the reference parser's conventions (State lane, trigger text, slice order as timeline, identity props). Until emlang adopts them, "derivable" means "derivable by one implementation".

## Implementation (from p3-impact)

Version 0.6.0, all additive. Zero existing snapshots change (Verify omits null/false/empty); extend spec example blocks 3, 4, 6 instead of adding blocks so `SpecExampleTests` indices hold. Shared prerequisite P0: project slice chains from `Emlang.Linting.EmAst` into `EmSpec` (size M) to unlock `xm-then-restates-default`, `xm-command-phase-mismatch`, `xm-command-trigger-mismatch`. Slice 1 (no P0, ~2 days): exception labels (replaces 35 hand-written strings in three games today), `confirm` + `$confirm`, `then` with the persona lint only, ICU surface headings with the capped grammar, spec 0.6.0 text. Slice 2: P0 and its lints. Slice 3: `during` namespace, add `lob-ap.*` as the fourth CI fixture. Do not build a selection engine in the interpreter: state D-selection as prose and check compliance with a generated selector test once P0 exists.

## Files

Evidence: `im/p1-mbui.md`, `im/p1-ixd.md`, `im/p1-derivability.md`, `im/p1-residue.md`, `p1-lob.md`, `lob-ap.em.yaml`, `lob-ap.xm.yaml`. Analyses: `im/p2-senseA.md`, `im/p2-senseB.md`, `im/p2-senseC.md`. Rulings: `im/p3-redteam.md`, `im/p3-impact.md`.
