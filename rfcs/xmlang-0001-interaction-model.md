---
title: "RFC 0001: Interaction judgments in xmlang (draft for v0.6.0)"
status: draft
created: 2026-09-07
targets: xmlang 0.6.0
research: https://martinrl.github.io/articles/xmlang-interaction-model-research.html
---
# RFC 0001: Interaction judgments in xmlang

**Status: draft.** Nothing here is applied to `xmlang-spec.md`. This RFC is the artifact the maintainer decides on. The research behind it is in the companion article; the evidence files are listed at the end.

## Summary

The inquiry asked whether an Interaction Model stratum belongs in xmlang, in three senses: navigation topology, dialog/state, and interaction idioms. The answer is no stratum, but four smaller things:

1. Two optional keys on command items: `confirm` and `then`.
2. An extension of `labels`: exception names as keys, `$confirm`, `$values`, and ICU MessageFormat arguments (capped grammar), which also carries surface headings.
3. A Defaults sub-table, "Navigation and enablement defaults", stating the interaction facts every transformer already derived identically from the Event Model, including the fine surface selection the v0.2 text calls inexpressible.
4. A correction to `during`: it resolves against the lifecycle of one decider, with a map form for multi-decider models.

Plus new lint rules, new Non-goals recording what was rejected, and a list of debts that belong in emlang.

Everything is additive. No existing key changes type or meaning. Version 0.6.0.

## Motivation

xmlang v0.5 records personas, surface composition, salience, phase activation, journeys, labels and tokens. It says nothing about what the user sees after a command, whether a command asks first, or which datum titles a surface. On the three shipped games this cost nothing: the runtime interpreter hard-codes the same rules three times, and three clean-room agents rebuilt the same rules from the spec pair alone. On a line-of-business model (a 33-slice accounts-payable Event Model with four roles, drafts, approvals, voids, reversals and bulk operations) a designer wanted to record four judgments the vocabulary could not hold, and hit one defect in `during`. Line-of-business software is xmlang's target; the games are a negative control.

The admission tests were fixed before evidence and are the same ones that reverted `slot:` in v0.4.0: not derivable from the Event Model; at least one genuine non-default annotation on the LOB testbed; no geometry, including disguised geometry; no second key demanded to do any work; meaningful for codegen, runtime interpreter and human transformer alike.

## Proposed normative changes

### 1. Command items: `confirm`

Add under "Command items and action prominence":

- A command item MAY contain a `confirm` key
- `confirm` MUST be a boolean; if absent it defaults to `false`; a non-boolean value MUST be reported as an error (`xm-confirm-not-boolean`)
- `confirm: true` states that the transformer MUST obtain the viewer's explicit assent before issuing the command; the form of assent (dialog, inline second step, typed re-entry) is transformer-defined and MUST NOT be expressed in the model
- The assent copy is the command's `$confirm` label (see Labels); absent, transformers MUST use the command's own label in a generic question
- `confirm` records a judgment about consequence and friction, never about geometry or degree; it is the whole of xmlang's confirmation vocabulary, and future versions MUST NOT add a tier, placement or style axis to it

```yaml
# proposed
surfaces:
  InvoiceApprovedApprover:
    for: [Approver]
    during: [approved]
    compose:
      - v: Invoice details
      - c: VoidInvoice
        prominence: overflow
        confirm: true
  SupplierRecord:
    for: [Admin]
    compose:
      - v: Supplier details
      - c: UpdateSupplier
      - c: DeactivateSupplier
        confirm: true          # reversible, and still asked first: an inactive supplier blocks drafting
```

Evidence: 4 of 21 composed commands on the LOB testbed carry a genuine `confirm: true`; two of the four (DeactivateSupplier, ReversePayment) are not derivable from irreversibility even if emlang recorded compensation, because the reason is consequence to others. Six enterprise design systems record confirmation per action. The three games carry none, correctly.

Considered and rejected: deriving "no confirm" from an emlang `compensates:` fact. The LOB designer wanted confirmation on a compensated command. xmlang MUST NOT derive `confirm` from compensation.

### 2. Command items: `then`

Add under "Command items and action prominence":

- A command item MAY contain a `then` key naming the surface the viewer is returned to after the command is accepted
- `then` MUST be a single surface name declared under `surfaces`; an unknown name MUST be reported as an error (`xm-dangling-ref`)
- The named surface MUST apply to every persona the composing surface applies to; a mismatch MUST be reported as an error (`xm-then-persona-mismatch`)
- `then` expresses a return, never an advance. The advance is the Event Model's: the terminal view of the command's slice names what the actor sees next (see Defaults). A `then` naming the surface that composes that terminal view restates the default and SHOULD be reported as an info finding (`xm-then-restates-default`)
- `then` is never conditional and never selects an instance. A command whose destination depends on the outcome is two commands in the Event Model; a rejected command always re-presents the issuing surface; "the next item" is a worklist view or a command, not a destination

```yaml
# proposed
surfaces:
  InvoiceDraft:
    for: [Clerk]
    during: [draft, rejected]
    compose:
      - v: Invoice details
      - c: EditDraft                  # default: stay
      - c: SubmitInvoice
        then: ClerkHome               # return to the worklist; default would show the submitted record
      - c: DiscardDraft
        confirm: true                 # default destination is already the list: the slice ends in Invoice list
```

Evidence: 2 of 21 composed commands on the LOB testbed (SubmitInvoice → ClerkHome, VoidInvoice → ApproverHome); 0 of 0 on the games. The status of this key is **provisionally admitted**. The strongest objection is stated in the companion article: the LOB modeller wrote destinations into each slice's terminal `v:`, so `then` is a second home for a fact the Event Model already carries. Pre-registered withdrawal: if a second LOB model shows terminal views and `then` systematically disagreeing, the fix is the Event Model and this key is removed by the same rule that removed `slot:`.

### 3. Labels

Amend the Labels section:

- Every other key of a label map MUST be the exact name of a command, view, surface, journey, persona, **or exception (`x:` element)**; an exception key takes the string form only
- Reserved keys gain:
  - `$confirm` (command level) — the assent question shown when the command is composed with `confirm: true`
  - `$values` (field level) — a map from the field's declared enum values (emlang's parenthesized-note convention) to labels; a key that is not a declared value MUST be reported as an error (`xm-orphan-label`)
- A label string MAY be an ICU MessageFormat message restricted to simple arguments `{name}` and plural arguments `{name, plural, …}`; `select` and nested conditionals MUST NOT be used and MUST be reported as an error (`xm-label-grammar`). An argument MUST name a scalar field of the labelled element or, for a surface label, of one of the surface's composed views; an unresolvable or ambiguous argument MUST be reported as an error (`xm-label-arg-missing`)
- A surface's on-screen heading remains its label. A heading that carries data is a surface label with an argument; this specification defines no `heading` or `subheading` key

```yaml
# proposed
labels:
  en:
    ConcurrentEdit: Someone else changed this record. Reload to see their changes.
    ReasonRequired: Give the requester a reason.
    NotEnoughPlayers: At least two players are needed.
    VoidInvoice:
      $self: Void invoice
      $confirm: "Void {invoiceNumber}? Posted amounts are reversed and this cannot be undone."
    Invoice details:
      phase:
        $self: Status
        $values: { draft: Draft, submitted: Awaiting approval, onHold: On hold, approved: Approved, paid: Paid, voided: Voided }
    InvoiceDecision: "Approve {invoiceNumber} from {supplierName}"
    Budgivning: "{description}"
    Väntan: "{done} of {total} players have bid"   # resolves only once Bid progress carries the counts (emlang debt 8)
```

Evidence: 25 distinct exception names on the LOB testbed; 35 hand-written error strings across the three games' endpoint code that this replaces; two enums needing value labels; 14 record surfaces on the LOB testbed and 3 game surfaces wanting a data argument in their heading.

### 4. Defaults: "Navigation and enablement defaults"

Add a sub-table to Defaults. These rows state what all three shipped interpreters and all three clean-room probes computed identically; stating them buys cross-transformer determinism at zero vocabulary, as the v0.4.0 command-order default did.

| Absent | Default |
|---|---|
| Entry surface | A persona's entry surface is the first surface in document order that applies to the persona and carries no `during`; a persona with no such surface enters through the bare form of its first triggered command |
| Global navigation | The surfaces the entry rule ranges over, in document order; `during`-bound surfaces are reached through destinations and links; a terminal-phase surface offers a return to the entry surface |
| Destination after a command | The surface applicable to the viewer's persona that composes the terminal view of the command's slice, in the resulting phase; if there is none, or it is the issuing surface, the issuing surface (stay); a rejected command re-presents the issuing surface. `then` overrides |
| Selection within a `during` × `for` cell | If the cell holds one `during`-bound surface for the persona, that surface. If several, the surface composing the terminal view of the slice whose event fired most recently for the viewer: the viewer's own command's event, a System-triggered event, or another persona's event advance the viewer; an event from a peer of the viewer's own persona does not. Ties are broken by offerable commands, then declaration order |
| Enablement | A composed command is rendered only to personas whose `role` matches one of the command's trigger roles. A command the Event Model shows would be rejected for this viewer in the current state independent of typed input (a rejection scenario whose `given` differs from a success scenario's while its `when` props match) is rendered disabled with the blocking exception's label, never hidden by state alone. Input-validation and concurrency rejections are reported after the attempt |
| Required input | A command prop is required iff some rejection scenario rejects its absence |
| Bulk actions | A command whose prop is a list of a composed view's item identity is offered over the viewer's selection of that view |
| Links | A view field typed as another view's identity links to the surface composing that view for the persona |
| Unidentified viewer | Sees only surfaces with no `for:`, with commands withheld, and is routed to the bare entry form where one exists |

Add a non-normative note after `during`:

> Fine selection within a cell was declared inexpressible in v0.2 and implemented as a hand-written selector in every product. It is derivable. Read the phase's slices in order; each state-change slice ends in the view the actor sees next. A viewer's position is the terminal view of the last slice whose event fired for her: her own command's event moves her (PlaceBid → the waiting surface), a System processor's event moves everyone (LotRevealed → the results surface), another persona's event moves her (StartAuction → the bidding surface), and a peer's event does not. This reproduces the selectors of all three testbed games and all three clean-room probes. On a line-of-business model with one record surface per persona per phase the rule is trivially true. Where a surface is entered by no event at all, the transition is navigation or a missing command, never a predicate.

Amend the `during` section: replace "Fine selection within one cell of that lattice is deliberately NOT expressible … a hand-written selector in the concrete stratum" with "Fine selection within one cell is derived from the Event Model; see Defaults. This is a considered rejection of a general `when:` predicate language, which would be redundant."

Note on shipping: the enablement row's role clause hides Next/End from the games' host surfaces until their Event Models carry a host trigger beside the System trigger (emlang debt 2). The row and the fixture fix ship together.

### 5. `during` resolves per decider

Amend the `during` section:

- `during` states when in the lifecycle of **the decider the surface is about** the surface is active
- `during` MUST be either a list of phase values (valid when the Event Model declares exactly one `State`-lane view with a `phase` enum, or when every listed value is declared by exactly one such view) or a map from the name of a `State`-lane view to a list of its phase values
- A bare phase value declared by more than one `State`-lane view MUST be reported as an error (`xm-ambiguous-phase`)
- `xm-unknown-phase` and `xm-phase-uncovered` resolve per decider

```yaml
# proposed
surfaces:
  InvoiceDraft:
    during: { "State / Invoice": [draft, rejected] }
    compose: [{ v: Invoice details }, { c: EditDraft }]
  SupplierInactive:
    during: { "State / Supplier": [inactive] }   # requires the supplier decider to name its prop `phase` (emlang debt 4); the testbed says `status` today
    compose: [{ v: Supplier details }, { c: ReactivateSupplier }]
```

Evidence: the LOB testbed has two deciders; the reference parser unions every `State /` view's phase enum, and the supplier lifecycle had to be renamed `status` to avoid a namespace collision. Single-decider documents are unaffected.

### 6. Lint rules

Origin cross-check (moved here from emlang RFC 0003 on 2026-09-11; origin is an experience fact, so emlang keeps it as free text):

- A trigger origin, the text after the swimlane separator of an emlang `t:`, MAY name a surface; when it does, a surface of that name MUST exist and MUST admit a persona whose `role` matches the trigger role, else `xm-origin-mismatch` (warning)  `# proposed`
- An origin matching no surface is free text and is reported at most once per trigger; it never fails the model  `# proposed`
- The check reads `compose` and `for:` only; it derives nothing from the origin (entry and destination stay as the Defaults state them)  `# proposed`

| Rule | Severity | Meaning |
|---|---|---|
| `xm-confirm-not-boolean` | error | `confirm` is not `true` or `false` |
| `xm-confirm-habituation` | warning | More than half of the commands composed on one surface carry `confirm: true` |
| `xm-then-persona-mismatch` | error | A `then` surface does not apply to every persona of the composing surface |
| `xm-then-restates-default` | info | A `then` names the surface the destination default would pick (the dead-weight metric) |
| `xm-label-arg-missing` | error | A label argument resolves to no scalar field of the labelled element or, for a surface, of its composed views, or resolves ambiguously |
| `xm-label-grammar` | error | A label uses ICU constructs beyond simple and plural arguments |
| `xm-ambiguous-phase` | error | A bare `during` value is declared by more than one `State`-lane view |
| `xm-command-trigger-mismatch` | warning | A command composed on a `for:`-scoped surface has no Event Model trigger whose role matches any listed persona's `role` |
| `xm-command-phase-mismatch` | info | A command is composed on a surface active in a phase for which the Event Model has no success scenario for that command; info until emlang has a scenario-coverage lint |
| `xm-cell-ambiguous` | info | Two surfaces in one `during` × `for` cell compose the same view and neither composes a command; suppressible |
| `xm-origin-mismatch` | warning | An Event Model trigger origin (`t: Role /origin`) names a surface, and no surface of that name admits a persona with that `role`; origin text matching no surface at all is reported once per trigger |

### 7. Non-goals (additions)

- **A `when:` predicate language** — rewritten: rejected because redundant, not only dangerous. Fine selection is derived (Defaults); anything else `when:` would express is a missing phase value, a missing command (an intent with no event), or a decider guard.
- **Entry surface keys** — document order expresses the entry surface (Defaults); the judgment is positional, which is this specification's "order = importance" doctrine.
- **Wizard steps and command-prop grouping** — a multi-step interaction with intermediate state is several slices (a draft and its edits); chunking one command's fields is concrete; groups need labels, so the first instance demands a new element kind.
- **Conditional and instance destinations** — `then-on-reject`, `then: next`: a destination that depends on outcome is two commands; "the next item" is a worklist view or a command.
- **A `heading` or `subheading` key** — a heading is the surface's label, with an argument when it carries data; a field promoted out of its tier into a region is `slot:` again.
- **Posture** (read-only/editable, sovereign/transient) — editable follows from composing a command; the rest is a container property.
- **Presentation modality** (modal, drawer, inline, page edit) — placement.
- **Undo pairing** — which command compensates which is an Event Model fact; xmlang decides friction and prominence only.
- **Per-command hide/disable** — hide-by-role is `for:` plus the enablement default; disable-by-state is the enablement default; a switch restates a design-system-wide rule.
- **Confirmation tiers** (`impact:`, type-to-confirm) — degree of friction is the transformer's rendering of consequence; `prominence: overflow` with `confirm: true` is the strong form.
- **Message channel, loading thresholds** — fixed once per design system in every system surveyed, never per action; timing is already a Non-goal.
- **Related-surface edges, topology archetypes, master-detail arrangement, site IA across Event Models** — identity lineage derives the relation; arrangement is geometry; whole-product shape is a graph property of the Event Model.

### 8. Deferred, with a re-open condition

**Status criticality per enum value** — a closed semantic tier (`negative | critical | positive | neutral | information`) per declared enum value, which tokens then bind. Not derivable, not geometry, recorded per value by four design systems. One instance across four testbeds; a token-naming convention (`color.status.<value>`) carries it today. Re-open when a second LOB model needs a semantic state the token path cannot express.

## Changelog entry (draft)

### v0.6.0 — interaction judgments, and the derived interaction stratum

Driven by the Interaction Model inquiry (companion article) and its line-of-business testbed.

- **Added `confirm:`** (command item, boolean): the viewer's explicit assent before a command. Form of assent is transformer-defined. Rejects tiers, placement and derivation from compensation
- **Added `then:`** (command item, surface name): return to a surface after an accepted command. Return, never advance; never conditional; never an instance. Provisionally admitted; `xm-then-restates-default` is its dead-weight metric
- **Labels**: exception names are labelable; reserved `$confirm` (command) and `$values` (field); ICU MessageFormat arguments restricted to simple and plural; surface headings carry data through arguments. Replaces a proposed `heading:` key
- **Defaults**: new sub-table "Navigation and enablement defaults" (entry, global navigation, destination, selection within a cell, enablement, required input, bulk, links, unidentified viewer). **Corrects v0.2**: fine selection within a cell is derived, not inexpressible; the `when:` Non-goal is rewritten as redundant
- **`during` resolves per decider**: map form added; `xm-ambiguous-phase`. Fixes a namespace collision in multi-decider models
- **Lint rules v0.6**: ten rules listed above
- **Non-goals**: twelve entries recording what the inquiry rejected
- **Deferred**: status criticality per enum value

## Migration

None required. All v0.5 documents remain valid v0.6 documents. Single-decider documents keep the list form of `during`. Products that hand-write exception copy MAY move it into `labels`; products that hand-set a data heading MAY move it into a surface label with an argument.

## Implementation notes (reference implementation)

- Additive members on `XmCommandItem` (`Then`, `Confirm`) and `XmLabelEntry` (`Confirm`, `Values`); Verify omits null/false/empty, so no existing snapshot changes until an example uses a key. Extend spec example blocks 3, 4 and 6 rather than adding blocks, so `SpecExampleTests` indices hold.
- Shared prerequisite for `xm-then-restates-default`, `xm-command-phase-mismatch` and `xm-command-trigger-mismatch`: project slice chains (trigger roles and origins, command, exceptions, events, terminal view, success-scenario phases) from `Emlang.Linting.EmAst` into `EmSpec`. One `EmSpecShape` re-approval.
- Suggested order: (1) exception labels, `confirm` + `$confirm`, `then` with the persona lint, ICU arguments with the capped grammar, spec text; (2) slice chains and the lints they unlock; (3) `during` per decider, and add the LOB testbed as a fourth CI fixture; (4) `$values`.
- Do not build a selection engine in the runtime interpreter. State the selection default as prose; the existing selectors comply as written; check compliance with a generated selector test once slice chains exist.

## Debts that belong in emlang

Named here so the derivations above become references instead of heuristics. None is proposed for xmlang.

1. ~~Trigger origin resolves to a view~~ Reassigned to xmlang on 2026-09-11 as `xm-origin-mismatch` (§6): origin is an experience fact; emlang keeps it free text (emlang RFC 0003, Rejected alternatives). Census then: 18/20 resolved on the LOB testbed, 1/23 on the games.
2. True trigger sets: paid by emlang RFC 0003 (several `t:` per slice = the command's trigger set). The host-beside-System fixture edit on the games was dropped on 2026-09-11 (games are the negative control); a dual initiator is expressed through emlang RFC 0005 when an LOB model shows one.
3. View parameters: mark filter and query props as inputs to a projection, not projected data
4. Phase per decider: a multi-decider model must not force the second decider to avoid the word `phase`
5. `compensates:` or `reverses:` on domain merits; xmlang derives nothing from it
6. Scenario coverage lint: every command has a success scenario in every phase it may succeed in
7. Actor identity on events as a stated convention, so selection and `self:` resolve against the same prop
8. Information completeness on result views: the games' result views lack the datum their headings show

## Open objections (recorded, not resolved)

1. `then` is a second home for a fact the Event Model's terminal view already carries; "return, never advance" is a rule in prose.
2. Every LOB count rests on one constructed testbed, never rendered by a transformer; there is no "live design equals default" proof of the kind that settled the geometry experiment.
3. The Defaults rows are derivable only under conventions the reference parser holds and the emlang spec does not state (State lane, trigger text, slice order as timeline, identity props).

## Evidence

All inquiry files are copied to `rfcs/0001-evidence/`: the brief and decision record (`BRIEF.md`, `SYNTHESIS.md`); evidence (`p1-mbui.md`, `p1-ixd.md`, `p1-derivability.md`, `p1-residue.md`, `p1-lob.md`, `lob-ap.em.yaml`, `lob-ap.xm.yaml`); per-sense analyses (`p2-senseA.md`, `p2-senseB.md`, `p2-senseC.md`); rulings (`p3-redteam.md`, `p3-impact.md`). The LOB testbed files are candidates for `tests/Emlang.Tests/fixtures/`. Line references inside the reports point at the scratchpad layout (`im/…`), which maps one-to-one onto this folder.
