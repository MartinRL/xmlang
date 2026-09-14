---
title: "emlang RFC 0002: The decider dialect (state element and decider rules)"
status: accepted
accepted: 2026-09-14
created: 2026-09-09
revised: 2026-09-11
targets: "the emlang dialect in this repository (src/emlang), forked from upstream emlang spec v1.0.0; consumer xmlang 0.6.0 (during per decision model)"
depends: "none (emlang RFC 0001 withdrawn 2026-09-11); required by emlang RFC 0004 (lints)"
---
# emlang RFC 0002: The decider dialect

**Status: accepted 2026-09-14, not yet implemented.** Nothing here is applied to `src/emlang` yet. This RFC defines the decider dialect of emlang: the maintainer's 1:1 map between an Event Model and its deciders, with Dynamic Consistency Boundaries (DCB, Pellegrini and Waidelich) as the consistency model. The dialect forks the upstream emlang v1.0.0 grammar: it adds a sixth element kind, `s:` (state), and makes the rules below the dialect's base rules. Upstream tools do not read dialect files, and no upstream acceptance is sought (decision 2026-09-11, `rfcs/emlang-evidence/PLAN.md`). It pays xmlang RFC 0001 debt 4 (phase per decision model), answers xmlang RFC 0001 open objection 3 for the state convention, and assigns the severity of the actor-identity rule whose text is in emlang RFC 0004. Dependency graph: RFC 0001 (profiles) is withdrawn; RFC 0004 requires this RFC; RFC 0003 section A stands alone. A pending RFC 0005 (`rfcs/emlang-0005-initiators.md`) renames `t:` to actor and automation; this RFC references it in one line and does not depend on it.

Throughout, "document" means one YAML document (the unit between `---` separators), as in upstream "Document Structure" ("Each document MUST independently conform"). Every rule scoped to a document is scoped to that unit, never to the file. "Automation" is used for what the fixtures call `⚙️ System`; that text is quoted only when citing fixture lines.

## Summary

In the dialect:

1. A state element, `s:`, is a decision model. Its fold tests define it by example: the model's query is the union of event types in every fold test whose `then` is that state, its tags are the state's identity-typed props, and its append condition is that query at the position the decision was made. Nothing is declared beyond the tests.
2. A decision test's `given` is exactly one explicit state element, always. The empty state is the state element with props absent and means the query matched no events; every non-empty state, at phase-value level, is produced by a fold test in the same document.
3. The decision model is closed: every event type in a decision test's `then` appears in some fold test of the given state. Every event in a fold test carries at least one of the state's identity props.
4. Swimlanes are canvas grouping only; the aggregate rule (`then` events share the given state's swimlane) is the rejected alternative.
5. Phase is per decision model. A Todo view in an automation's decision test should be accompanied by the decision model the command validates against.
6. Fold tests and Decision Model slices are exempt from upstream's `test-missing-command` and `slice-missing-event`, which the dialect inherits.

Eight lint rules. Six schema edits to the dialect's copy of `schema.json`.

## Motivation

The maintainer's stated aim: "a 1:1 map to the decider in my dialect of emlang, ie the linter prohibiting event[] as given for gwt, only state (perhaps s: rather than v: ?), and every such state preceding gwt's". On 2026-09-10 the maintainer fixed the consistency model as DCB (`rfcs/emlang-evidence/PLAN.md`, Decisions 2026-09-10): a decision model is defined by a query over event types and tags, and an append condition replaces the per-stream version.

The reason comes from the domain. B2B SaaS invariants are cross-entity by nature. A seat limit reads the Subscription's plan and the Members added; a unique workspace slug reads every workspace created in the tenant; an approval threshold reads the Approver's role and limit and the Invoice's amount; drafting an invoice reads whether its Supplier is active. Each of these is one decision, made over events tagged with two or three identities, and none belongs to a single entity's stream. Under a stream-per-entity model each needs either a process manager, a denormalised copy of the foreign fact on the entity, or an eventual-consistency apology. Under DCB each is one decision model: a query over the event types and tags it reads, folded into a state, with the append condition guaranteeing no matching event was appended since the state was read. The Event Model already draws it that way: the fold test says which events the decision reads, and the decision test says what it emits.

The running example is drafting an invoice against an active supplier. The current lob-ap model gives `v: State / Supplier` and emits `e: Invoice / InvoiceDrafted` (`rfcs/0001-evidence/lob-ap.em.yaml:322-331`), reading one entity and writing another. In the dialect the decision has its own model, `s: InvoiceDrafting`, folded from `Supplier / SupplierCreated`, `Supplier / SupplierDeactivated`, `Supplier / SupplierReactivated` and `Invoice / InvoiceDrafted` (lob-ap's names; the plan's "SupplierActivated" is `SupplierCreated` and `SupplierReactivated` there), tagged by `supplierId`. Section 1 writes it out.

emlang v1.0.0 says nothing about state. "Test Structure" allows `given` to hold "`e` (event), `v` (view)" and the spec's own `EmailMustBeUnique` example (section "Extended Form", lines 137-147) gives an event and then issues a command. Event Modeling's worked specification does the same: "Given: We have registered, and added a payment method" is a list of prior facts, not a folded state. A state-only `given` is therefore a departure from the method's GWT as written, not a restatement of it. The reason for the departure: a decider reads its folded state, so an events-given decision test exercises the fold and the decision at once and pins neither; splitting them gives each fold one test that pins the state and each decision one test that pins the events. Under DCB the split carries a second load: the fold tests are the only place the decision model's query is written down.

Four local models already write the split, without spec support (`tests/Emlang.Tests/fixtures/blindbudet.em.yaml:23-24`; `mer-eller-mindre.em.yaml:24-27`; `tank-till-tusen.em.yaml:21-24`; `lob-ap.em.yaml:11`). The census of their 185 tests (`rfcs/emlang-evidence/census-gwt-state.md`, re-counted for this RFC and independently by the red team, `redteam.md` R23); the games are the negative control and appear here as rows only:

| `given` shape | blindbudet | mer-eller-mindre | tank-till-tusen | lob-ap | total |
|---|---|---|---|---|---|
| events only (fold or projection test) | 12 | 15 | 12 | 15 | 54 |
| exactly one `v: State / X` | 20 | 24 | 22 | 39 | 105 |
| several `v: State / X` | 0 | 0 | 0 | 1 | 1 |
| `v: Todo / X` only | 0 | 0 | 0 | 3 | 3 |
| empty | 5 | 7 | 6 | 4 | 22 |
| mixed kinds | 0 | 0 | 0 | 0 | 0 |
| tests | 37 | 46 | 40 | 62 | 185 |

Every one of the 54 events-only givens belongs to a test without `when`; every test with `when` gives state, nothing, or (three times) a Todo view. The dialect restates this practice where it holds and states, in sections 2 and 3, where it does not: 22 empty givens that must name their state, 19 decision tests emitting event types no fold reads, and 30 pinned phase values no fold produces.

Two costs of leaving it unstated. First, the generator: `TestsEmitter.EmitGiven` (`src/emlang/Emlang/TestsEmitter.cs:112-133`) uses state only when `given` is exactly one State view (`:114`); anything else falls to the fold arm (`:125-132`) and, for the three-state given at `lob-ap.em.yaml:687-692` and the Todo givens at `:933`, `:945`, `:1110`, emits `new Invoice(...)` and `new Due payments(...)` inside an event-array initializer, which does not compile and raises no `SpecTestException`. Second, xmlang: its `during` resolves against a `State`-lane view's `phase` enum (`EmParser.cs:145-151`) and unions every decision model's values, so the LOB model had to name the supplier lifecycle `status` to stay out of the namespace (`lob-ap.em.yaml:21-25`, `:1267`). xmlang RFC 0001 records both as emlang debts (items 4 and 7) and as objection 3.

## Proposed normative changes

All rules below are the dialect's base rules and apply to every document the dialect's tools read. There is no opt-in header and no profile switch: a document either conforms to the dialect or it does not. The severities are in section 8.

### 1. The state element is a decision model

- The dialect defines six element kinds; the sixth is State, short `s:`, acronym `st:`, long `state:`
- A state element names one decision model; the element name is the model's name; a swimlane MAY be written and carries no meaning
- A state element MAY appear in `steps`, `given` and `then`
- A state element's `props` document the model's folded state; a state element in a test carries only the props that test reads or pins
- A state element's identity props are its props whose declared name ends in `Id` or `Ids`; they are the model's tags
- A fold test is a test without `when` whose `given` holds at least one event and whose `then` is exactly one state element; a fold test with an empty `given`, a non-event in `given`, or more than one element in `then` MUST be reported as an error (`em-fold-shape`)
- A decision model's query is defined by example: its event types are the union of the event types in the `given` of every fold test in the document whose `then` is that state, and its tags are the state's identity props
- A decision model's append condition is its query, evaluated at the position at which the decision's state was read; a decision's events MUST NOT be appended if any event matching the query was appended after that position
- Nothing about the query is declared outside the fold tests
- Two state elements with the same model name in one document denote the same model
- A view, `v:`, is a read model and never a decision model; the swimlane text `State` carries no meaning on a view

State `props` are documentation until a generator emits the state record. Today no generator does: `SurfaceEmitter` emits records for `c`/`e`/`x` only ("'v' elements are inert for record emission", `SpecModel.cs:19-20`), and the state type is a naming convention, `Prefix + "State"` (`EmitTarget.cs:14`), single-valued per spec and hand-written. The dialect makes documents with several decision models legal to lint and to consume from xmlang; generating code for more than one decision model per document is out of scope for this RFC.

Names are free. `s: Invoice` stays legal, and a model named after an entity is a habit, not a rule: the dialect does not care whether a model's query reads one swimlane or three. A cross-entity model takes a name for the decision, such as `s: InvoiceDrafting` or `s: ApprovalBatch`. In the dialect a swimlane is canvas grouping and nothing else; it names no stream, no consistency boundary and no owner.

The kind is explicit because the opinion is the grammar. The generator, the linter, the query derivation and xmlang's `during` key on `EmElementType.State`, never on lane text, and `em fmt` cannot normalize the meaning away. This was also the maintainer's first instinct ("perhaps s: rather than v:"). The alternative, a reserved `State` swimlane on a view, is the rejected alternative recorded below.

```yaml
# proposed: the running example
slices:
  ✍️ Draft Invoice:
    steps:
      - t: 🧾 Clerk /Invoice list
      - c: DraftInvoice
        props: { supplierId: Guid, invoiceNumber: string, lines: InvoiceLine[] }
      - x: SupplierInactive
      - e: Invoice / InvoiceDrafted
        props: { invoiceId: Guid, supplierId: Guid, invoiceNumber: string, lines: InvoiceLine[], createdBy: Guid }
      - v: Invoice details
    tests:
      draft can be saved for an active supplier:
        given:
          - s: InvoiceDrafting
            props: { supplierId: acmeId, supplierActive: true, phase: open }
        when:
          - c: DraftInvoice
            props: { supplierId: acmeId, invoiceNumber: A-2026-0042, lines: [line1] }
        then:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: minted, supplierId: acmeId, invoiceNumber: A-2026-0042, createdBy: annaId }
      cannot draft against an inactive supplier:
        given:
          - s: InvoiceDrafting
            props: { supplierId: acmeId, supplierActive: false, phase: blocked }
        when:
          - c: DraftInvoice
            props: { supplierId: acmeId, invoiceNumber: A-2026-0043 }
        then:
          - x: SupplierInactive
      cannot draft against an unknown supplier:
        given:
          - s: InvoiceDrafting                    # empty state: the query matched no events for this supplierId
        when:
          - c: DraftInvoice
            props: { supplierId: unknownId, invoiceNumber: A-2026-0044 }
        then:
          - x: SupplierNotFound
  👀 Invoice Drafting Decision Model:
    steps:
      - s: InvoiceDrafting
        props:
          supplierId: Guid                         # tag
          supplierActive: bool
          phase: DraftingPhase (open|blocked)
          draftedInvoiceIds: Guid[]                # tag
    tests:
      an active supplier is open for drafting:
        given:
          - e: Supplier / SupplierCreated
            props: { supplierId: acmeId, supplierNumber: S-1001 }
        then:
          - s: InvoiceDrafting
            props: { supplierId: acmeId, supplierActive: true, phase: open }
      a deactivated supplier blocks drafting:
        given:
          - e: Supplier / SupplierCreated
            props: { supplierId: acmeId, supplierNumber: S-1001 }
          - e: Supplier / SupplierDeactivated
            props: { supplierId: acmeId }
        then:
          - s: InvoiceDrafting
            props: { supplierId: acmeId, supplierActive: false, phase: blocked }
      reactivation reopens drafting and remembers the drafts:
        given:
          - e: Supplier / SupplierCreated
            props: { supplierId: acmeId, supplierNumber: S-1001 }
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1, supplierId: acmeId }
          - e: Supplier / SupplierDeactivated
            props: { supplierId: acmeId }
          - e: Supplier / SupplierReactivated
            props: { supplierId: acmeId }
        then:
          - s: InvoiceDrafting
            props: { supplierId: acmeId, supplierActive: true, phase: open, draftedInvoiceIds: [inv1] }
```

Read off the fold tests: the `InvoiceDrafting` query is the event types `SupplierCreated`, `SupplierDeactivated`, `SupplierReactivated`, `InvoiceDrafted`, tagged `supplierId` (and `invoiceId` through `draftedInvoiceIds`). The append condition for `DraftInvoice` is that query at the position the state was read: if the supplier is deactivated between read and append, the append fails and the decision is retried. The rejection case that section 2 of the previous draft could not express (a supplier's status is a "context dependency", `lob-ap.em.yaml:1289`, `:37-38`) is now an ordinary fold.

**Schema change.** The dialect's copy of `schema.json`: `element` (`schema.json:117-153`); `givenElement` (`:154-173`) and `thenElement` (`:190-215`) gain the same three properties and three `oneOf` arms. "Emlang defines 5 element types" becomes 6; the "Elements" table gains the row State, `s:`, `st:`, `state:`; "Test Structure": `given` allows `e`, `v`, `s`; `then` allows `e`, `v`, `x`, `s`. Before:

```json
"v": { "$ref": "#/$defs/elementName" },
"view": { "$ref": "#/$defs/elementName" },
"props": { "oneOf": [{ "type": "null" }, { "$ref": "#/$defs/props" }] }
},
"oneOf": [
  ...
  { "required": ["v"] },
  { "required": ["view"] }
],
```

After:

```json
"v": { "$ref": "#/$defs/elementName" },
"view": { "$ref": "#/$defs/elementName" },
"s": { "$ref": "#/$defs/elementName" },
"st": { "$ref": "#/$defs/elementName" },
"state": { "$ref": "#/$defs/elementName" },
"props": { "oneOf": [{ "type": "null" }, { "$ref": "#/$defs/props" }] }
},
"oneOf": [
  ...
  { "required": ["v"] },
  { "required": ["view"] },
  { "required": ["s"] },
  { "required": ["st"] },
  { "required": ["state"] }
],
```

What this costs in upstream compatibility is recorded, not avoided: a v1.0.0 validator rejects `s:` in `element`, `givenElement` and `thenElement`; the Go CLI throws `unknown key "s"` (`parser.go:380`); today's local parsers drop an `s:` step without a word (`SpecModel.cs:68-78`, `EmParser.cs:109-110`) and fail the whole generator run on an `s:` in `given` or `then` with `InvalidDataException` at `TestModel.cs:101` (`compat.md`, section 1, rows 4 to 6). The local toolchain is the dialect's reference implementation and is changed at the eleven sites listed under Implementation notes. The Go reference tools are not changed; they read upstream files, not dialect files.

### 2. Given: exactly one explicit state

Amend "Test Structure". The upstream text reads "If `given` is present and non-empty, its elements MUST be events or views". Add:

- A test whose `when` is present and non-empty is a decision test; its `given` MUST hold exactly one state element, which MAY be accompanied by non-state views the automation reads and MUST NOT be accompanied by events or by a second state element; a violation MUST be reported as an error (`em-given-not-one-state`)
- A state element with no `props` is the empty state: the model's query matched no events at the position the decision was made; the empty state needs no fold test, because the fold over no events is the identity
- Every other state element in a decision test's `given` MUST be produced by a fold test in the same document (section 3)

`given: []` appears nowhere in the dialect. "Initial state" is not a concept the dialect has: a decision either starts from the empty state, written as the named state element with props absent, or from a state some fold test produces. A rich starting state (an Organization after its onboarding todo list ran: workspace created, admin invited, plan selected, billing connected) is a state produced by a fold over those four events, and its decision tests give it like any other. Writing the name in the empty case is what makes the test say which query returned nothing: `s: InvoiceDrafting` with no props means no supplier and no draft matched, which is how `SupplierNotFound` becomes a decision the model can make (section 1, third decision test).

The one-state rule collapses the three shape lints of the previous draft (`em-given-events-in-decision`, `em-given-mixed`, `em-given-multi-state`) into one. `em-given-not-one-state` fires on 26 of 185 tests today: the 22 empty givens (5, 7, 6 in the games; lob-ap `:106`, `:123`, `:176`, `:701`), the three-state given at `lob-ap.em.yaml:685-692`, and the three Todo-only givens (`:931`, `:943`, `:1108`). `em-fold-shape` fires on none: all ten fold tests in the four documents give at least one event and pin one state.

The three-state given is the bulk approval, `✍️ Bulk Approve` (`lob-ap.em.yaml:666-700`): `ApproveInvoices` over a selection, three `State / Invoice` instances in `given`, one `Invoice / InvoiceApproved` and one `BulkApproval / BulkApprovalCompleted` in `then`, with the header comment admitting the invented stream ("the summary lives on a BulkApproval stream", `:664-665`). In the dialect it is one decision model over many tagged invoices; the three states and the invented stream both go away:

```yaml
# proposed rewrite of lob-ap.em.yaml:666-700
  ✍️ Bulk Approve:
    steps:
      - t: ✅ Approver /Approval queue
      - c: ApproveInvoices
        props: { invoiceIds: Guid[], comment: string }
      - x: NothingSelected
      - e: Invoice / InvoiceApproved                 # one per eligible invoice
      - e: Invoice / BulkApprovalCompleted           # the swimlane is canvas grouping; no BulkApproval stream
        props: { bulkApprovalId: Guid, invoiceIds: Guid[], approvedIds: Guid[], skippedIds: Guid[], skipped: SkippedInvoice[], approvedBy: Guid }
      - v: Approval queue
    tests:
      eligible invoices are approved and ineligible ones skipped with reasons:
        given:
          - s: ApprovalBatch
            props:
              invoiceIds: [inv1, inv2, inv3]
              candidates: [cand1Submitted12500ByAnna, cand2Submitted900000ByAnna, cand3Submitted100ByBo]
        when:
          - c: ApproveInvoices
            props: { invoiceIds: [inv1, inv2, inv3] }   # boId, limit 50000
        then:
          - e: Invoice / InvoiceApproved
            props: { invoiceId: inv1, approvedBy: boId }
          - e: Invoice / BulkApprovalCompleted
            props: { bulkApprovalId: minted, invoiceIds: [inv1, inv2, inv3], approvedIds: [inv1], skippedIds: [inv2, inv3], skipped: [skipInv2OverLimit, skipInv3Own], approvedBy: boId }

  👀 Approval Batch Decision Model:
    steps:
      - s: ApprovalBatch
        props:
          invoiceIds: Guid[]                           # tag: the selection
          candidates: ApprovalCandidate[]              # { invoiceId, phase: InvoicePhase, totalAmount: decimal, createdBy: Guid }
          completedBulkApprovalIds: Guid[]             # tag
    tests:
      batch folds the submitted invoices in the selection:
        given:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1, supplierId: acmeId, createdBy: annaId }
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv1, totalAmount: 12500 }
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv2, supplierId: acmeId, createdBy: annaId }
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv2, totalAmount: 900000 }
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv3, supplierId: acmeId, createdBy: boId }
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv3, totalAmount: 100 }
        then:
          - s: ApprovalBatch
            props: { invoiceIds: [inv1, inv2, inv3], candidates: [cand1Submitted12500ByAnna, cand2Submitted900000ByAnna, cand3Submitted100ByBo] }
      batch folds its own outcome:                    # closes the model: section 3 requires every then-event type to be folded
        given:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1, supplierId: acmeId, createdBy: annaId }
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv1, totalAmount: 12500 }
          - e: Invoice / InvoiceApproved
            props: { invoiceId: inv1, approvedBy: boId }
          - e: Invoice / BulkApprovalCompleted
            props: { bulkApprovalId: bulk1, invoiceIds: [inv1], approvedIds: [inv1], approvedBy: boId }
        then:
          - s: ApprovalBatch
            props: { invoiceIds: [inv1], candidates: [cand1Approved], completedBulkApprovalIds: [bulk1] }
```

The `ApprovalBatch` query, read off the folds: `InvoiceDrafted`, `InvoiceSubmitted`, `InvoiceApproved`, `BulkApprovalCompleted`, tagged by the selected `invoiceIds`. The append condition guarantees that none of the three invoices was approved, rejected or withdrawn by someone else between the read and the append, which is exactly the guarantee a per-stream `expectedVersion` cannot give across three streams.

### 3. Fold by reference: closed model, tagged events, phase-level reachability

- Every event type in a decision test's `then` MUST appear in the `given` of at least one fold test, in the same document, whose `then` is the decision test's given state; a violation MUST be reported as an error (`em-then-outside-query`)
- Every event in a fold test's `given` MUST declare, among its props in `steps`, at least one identity prop of the state the fold produces; a violation MUST be reported as an error (`em-fold-untagged-event`)
- Every non-empty state element in a decision test's `given` MUST be the `then` of at least one fold test in the same document; a violation MUST be reported as an error (`em-state-without-fold`)
- Every phase value pinned on a state element in a decision test's `given` MUST be pinned on that state in the `then` of at least one fold test in the same document; a violation MUST be reported as an error (`em-state-phase-without-fold`)
- The fold test MAY appear anywhere in the document; document order carries no meaning

The first rule closes the decision model: a decision emits only event types its own query reads, because under DCB an event outside the query is invisible to the append condition and to the next fold, so the model could neither guard against it nor observe it. The consequence is that a summary event such as `BulkApprovalCompleted` MUST be folded even when the fold does nothing useful with it (section 2's second `ApprovalBatch` fold). This rule replaces the previous draft's `em-given-decider-mismatch`; see the rejected alternative below.

The second rule is what makes the query executable: a DCB query matches events by type and tag, and an event type that carries none of the model's identity props cannot be matched to an instance. It checks the event type's declared props in `steps`, not the fixture values in the fold test, since fixtures are sparse by convention ("asserts only the props its `decide` actually reads", `blindbudet.em.yaml:25-26`).

The maintainer's phrase is "every such state preceding gwt's". Reference, not order, is the right axis: all four models put the fold tests in a trailing slice marked as an appendix (`👀 Decision Model` at `blindbudet.em.yaml:920`, `mer-eller-mindre.em.yaml:1291`, `tank-till-tusen.em.yaml:1009`; `👀 Supplier Decision Model` and `👀 Invoice Decision Model` at `lob-ap.em.yaml:1261` and `:1281`, "APPENDIX, not timeline steps", `:1259`), the upstream spec attaches no meaning to slice order ("Multiple Slices"), and xmlang RFC 0001 reads slice order as the timeline for other purposes.

The phase-level rule is the one that carries weight; the name-level rule is kept as the weaker fallback because it is the only one that reaches a model without a `phase` prop. Counted mechanically: `em-then-outside-query` fires on 19 decision tests today, 3 per game (`NextLotStarted`, `AuctionEnded` and their siblings are emitted but never folded: `blindbudet.em.yaml:791`, `:831`, `:848`) and 10 in lob-ap (`:157` `SupplierUpdated`, `:252` `SupplierReactivated`, `:322` `InvoiceDrafted` from `State / Supplier`, `:373` `DraftEdited`, `:420` `DraftDiscarded`, `:508` `SubmissionWithdrawn`, `:546` `InvoiceReopened`, `:642` `InvoiceRejected`, `:993` `PaymentReversed`, `:1042` `InvoiceVoided` and `LiabilityReversed`). `em-fold-untagged-event` fires on none: every folded event type in the four documents declares `gameId`, `invoiceId` or `supplierId`. `em-state-without-fold` fires on none. `em-state-phase-without-fold` fires 17 in lob-ap (`draft` 8, `approved` 7, `rejected` 1, `scheduled` 1; folds produce only `submitted`, `onHold`, `paid`, `:1298-1336`) and 4, 5, 4 in the games (`lobby`; folds produce only `started`).

### 4. Todo givens

- A decision test whose `given` holds a view (`v:`) SHOULD also hold the state element of the decision model the command validates against; a Todo given without an accompanying state MUST be reported as a warning (`em-given-todo`)

Event Modeling's Automation pattern is "Event(s) -> View -> Automated Trigger -> Command -> Event(s)", and "the view that the automated process monitors, is a simple todo list. For each row the automated process calls a use case, which provides a new event". A Todo in the `given` of an automation slice is therefore the method's own shape, and lob-ap models it so (`✍️ Execute Payment Run`, `:907-912`; `✍️ Remind Approvers`, `:1093-1098`). What the Todo does not carry is the decision model's guard: the automation selects the row, the decision model validates it and the append condition protects it. Written out for `lob-ap.em.yaml:931-942`, with the payment run as its own decision model:

```yaml
# proposed rewrite of lob-ap.em.yaml:931-942
      due invoice is paid by the run:
        given:
          - v: Todo / Due payments
            props: { asOf: 2026-09-30, due: [dueInv1], hasDue: true }
          - s: PaymentRun
            props: { invoiceIds: [inv1], scheduled: [inv1Scheduled20260930], paidInvoiceIds: [] }
        when:
          - c: ExecutePaymentRun
            props: { runDate: 2026-09-30 }
        then:
          - e: Payment / PaymentRunExecuted
            props: { paymentRunId: minted, runDate: 2026-09-30, payments: [paidInv1], totalAmount: 12500 }
          - e: Invoice / InvoicePaid
            props: { invoiceId: inv1, paymentRunId: minted, bankReference: ref1 }
```

`s: PaymentRun` folds `PaymentScheduled`, `PaymentRunExecuted` and `InvoicePaid`, tagged by `invoiceIds` and `paymentRunId`; both `then` events are inside its query, so section 3 is satisfied without moving either event to another swimlane. Under the one-state rule a Todo-only given is already an error (`em-given-not-one-state`); `em-given-todo` is the warning that names the fix. It fires three times today, at `lob-ap.em.yaml:933`, `:945` and `:1110`, none in the games. The `nothing due means no run` case (`:943-951`) gives the empty `s: PaymentRun`.

### 5. Phase per decision model

- A state element MAY carry a prop named `phase` whose declared type is an enum note of the form `<Enum> (a|b|c)`
- Phase values are namespaced by decision model: `Invoice.scheduled` and `InvoiceDrafting.open` are distinct values, and two models MAY declare the same bare value
- A second decision model MUST NOT be required to avoid the name `phase`

The last bullet is the debt. `lob-ap.em.yaml:1267` reads `status: SupplierStatus (active|inactive)   # NOT named phase: keeps the xmlang phase namespace to State / Invoice`, with the reason at `:21-25`. In the dialect it becomes `phase: SupplierPhase (active|inactive)`. The previous draft's `em-phase-ambiguous` is deleted: two decision models sharing a value such as `draft` or `open` is normal, and the only consumer that could confuse them, xmlang's list form of `during`, already has the map form.

How xmlang consumes it: `during` in map form keyed by decision model name, `during: { Invoice: [draft, rejected] }` (xmlang RFC 0001, Summary item 4, normative section 5); `EmSpec.PhaseValues` (`EmParser.cs:22`, computed at `:145-151`) becomes a map from model name to values; `xm-unknown-phase` and `xm-phase-uncovered` resolve per model; `xm-ambiguous-phase` stays on the consumer side for the list form.

### 6. Actor identity

- Every event in a decision test's `then` MUST carry the actor-identity prop defined by the convention in emlang RFC 0004; a violation MUST be reported as an error (`em-actor-identity`)

RFC 0004 defines the convention (which prop names who acted, so xmlang's `self:` and its selection default resolve against the same prop) and stays non-normative; this RFC assigns the severity, because a non-normative appendix cannot make a document non-conforming and a later RFC must not re-severity an earlier RFC's rules. No baseline is published here: the red team (`redteam.md` R18) shows RFC 0004's rule text needs role normalization (every model's trigger roles carry an emoji, `blindbudet.em.yaml:343`, `lob-ap.em.yaml:294`) before the rule can be run. RFC 0005's rename of `t:` to actor and automation (`rfcs/emlang-0005-initiators.md`), if adopted, gives the convention its vocabulary; this RFC does not depend on it.

### 7. Upstream's documented lints

The dialect mandates fold tests and Decision Model slices, an artefact Event Modeling's canvas does not have (all four patterns are timeline slices; the models mark it "APPENDIX, not timeline steps", `lob-ap.em.yaml:1259`). Upstream's `README.md` "Linter Rules" table (lines 70-81) documents `test-missing-command` at severity **error**, "Test without command (when)", and `slice-missing-event` at warning, "Slice without events". Every fold and projection test is a test without `when` (54 of 185 today: 12/15/12/15), and `slice-missing-event` is implemented (`linter.go`, ported at `Linter.cs:65-67`) and fires on every Decision Model and projection slice: `em lint` 0.2.0 reports 9, 11, 9 and 13 findings on blindbudet, mer-eller-mindre, tank-till-tusen and lob-ap. `test-missing-command` appears in the README only; `compat.md` section 4 finds no implementation in `linter.go`.

- In the dialect, a test without `when` whose `then` holds exactly one state or view element is a fold or projection test and MUST NOT be reported by `test-missing-command`
- In the dialect, a slice whose `steps` hold only state elements is a Decision Model slice and MUST NOT be reported by `slice-missing-event`
- Upstream SHOULD retract `test-missing-command` from the documented table or downgrade it to a warning, since a test with no `when` is the spec's own "Multiple Tests" example shape (`TodoCompleteRegistrationFlow`, line 231)

Said plainly: without these two exemptions the dialect requires an artefact that the inherited rules condemn. The dialect's linter changes one inherited rule's scope; the upstream retraction is a courtesy note, not a dependency.

### 8. Lint rules

| Rule | Severity | Fires today (blindbudet / mer-eller-mindre / tank-till-tusen / lob-ap) |
|---|---|---|
| `em-given-not-one-state` | error | 5 / 7 / 6 / 8 (22 empty givens, `lob-ap.em.yaml:685` three states, `:931`, `:943`, `:1108` Todo only) |
| `em-fold-shape` | error | 0 / 0 / 0 / 0 |
| `em-then-outside-query` | error | 3 / 3 / 3 / 10 |
| `em-fold-untagged-event` | error | 0 / 0 / 0 / 0 |
| `em-state-without-fold` | error | 0 / 0 / 0 / 0 |
| `em-state-phase-without-fold` | error | 4 / 5 / 4 / 17 |
| `em-given-todo` | warning | 0 / 0 / 0 / 3 (`lob-ap.em.yaml:933`, `:945`, `:1110`) |
| `em-actor-identity` | error | not measured; rule text in RFC 0004 needs role normalization first |

Every rule is on in every dialect document: each depends on the meaning of `given` the dialect assigns. The upstream spec's own example (`EmailMustBeUnique`, events-given with `when`) is not a dialect document.

### 9. Spec text touched (the dialect's copy of SPEC.md)

- "Elements": "Emlang defines 5 element types" becomes 6; a new table row State, `s:`, `st:`, `state:`.
- "Test Structure": the table rows `given` and `then` add `s`; after "If `given` is present and non-empty, its elements MUST be events or views", the bullets of sections 2 and 3.
- "Swimlanes": one sentence that a swimlane is canvas grouping only.
- A new section "Decision models" holding sections 1 to 7 of this RFC.
- `README.md` "Linter Rules": the exemptions of section 7.

## Rejected alternative: a reserved `State` swimlane on a view (form A)

The 2026-09-09 draft carried two forms. Form A kept the five upstream kinds and reserved the swimlane `State` on a view: `v: State / Invoice` was the state element, defined after the parser's whitespace trimming so that `State / X` and `State/X` were the same element. It cost zero schema change, no change to the Go reference tools, and all 185 existing tests were already written in it; the red team preferred it (`redteam.md` R10) and it kept the document a valid v1.0.0 document, which the 2026-09-09 plan wanted.

Rejected 2026-09-11 because state and read model then share one kind. The dialect's central concept becomes a naming convention on a view, enforced only by lints and by a trimmed string compare; the opinion is invisible in the grammar. A human screen a modeller calls `State /` becomes a decision model by accident, and `em fmt` can rewrite the text the meaning hangs on. Once the dialect stopped aiming at upstream compatibility (the decision that withdrew RFC 0001), form A's only remaining advantage, the strict-superset property, was no longer a goal. The census counts and the migration figures below still cite `v: State /` lines because that is how the fixtures are written today; each becomes an `s:` line.

## Rejected alternative: the aggregate rule

The previous draft required that every event in a decision test's `then` carry the swimlane of the given state (`em-given-decider-mismatch`): one state, one stream, one owner, the aggregate. It is rejected because the invariants the domain actually has cross entities, and the rule fails exactly on them. The four lob-ap tests it rejected are the B2B cases DCB accepts:

| lob-ap test | Reads | Writes | Under the aggregate rule | Under DCB |
|---|---|---|---|---|
| DraftInvoice, `:322` | the Supplier's status | `Invoice / InvoiceDrafted` | error: Supplier state, Invoice event | one model, `InvoiceDrafting`, query over `supplierId` (section 1) |
| ReversePayment, `:993` | the Invoice's phase and payment run | `Payment / PaymentReversed` | error: Invoice state, Payment event | one model whose folds read `InvoicePaid` and `PaymentReversed`, tagged `invoiceId` and `paymentRunId` |
| VoidInvoice, `:1042` | the Invoice's phase and amount | `Invoice / InvoiceVoided`, `Ledger / LiabilityReversed` | error: Ledger event | one model whose folds include `LiabilityReversed`; the ledger swimlane is grouping only |
| ApproveInvoices, `:685` | three Invoices' phase, amount, creator | `InvoiceApproved` per invoice, a summary | error: three states | one model, `ApprovalBatch`, tagged by the selection (section 2) |

Each of the four fires `em-then-outside-query` today for the same underlying reason (the fold tests do not yet read what the decision writes), and each is fixed by writing the fold, not by moving the event or splitting the decision. In the dialect a swimlane is canvas grouping only.

## Rejected alternative: a declared event-type list

The query could be declared instead of derived, as a props note on the state (`query: SupplierCreated | SupplierDeactivated | InvoiceDrafted`) or as a key. Rejected: it is a second home for a fact the fold tests already carry, it would drift from them with no check either way (the dual-specification failure `redteam.md` R11 names), and as a key it breaks the closed `element` schema. The cost of deriving is recorded as open objection 4: a fold test that is never written silently narrows the boundary.

## Changelog entry (draft)

### emlang dialect: state element and decider rules (forked from upstream v1.0.0)

- **State element is a decision model**: a sixth kind, `s:`/`st:`/`state:`; query by example from fold tests (event types) and identity props (tags); append condition replaces per-stream version; state props are documentation until a generator emits the record; names free, swimlanes are grouping
- **Given is exactly one explicit state**: empty state is the named element with props absent; no `given: []`; no initial state. `em-given-not-one-state`, `em-fold-shape`
- **Closed, tagged, reachable**: then-events inside the given state's query; fold events carry a state identity prop; every non-empty state and every pinned phase value produced by a fold in the document. `em-then-outside-query`, `em-fold-untagged-event`, `em-state-without-fold`, `em-state-phase-without-fold`
- **Todo givens** should be accompanied by the decision model. `em-given-todo`
- **Phase per decision model**: namespaced; shared bare values are normal; `em-phase-ambiguous` deleted
- **Actor identity**: error; convention in RFC 0004. `em-actor-identity`
- **Upstream lints**: fold tests exempt from `test-missing-command`, Decision Model slices exempt from `slice-missing-event`
- **Rejected**: a reserved `State` swimlane on a view (form A); the aggregate rule (`em-given-decider-mismatch`); a declared event-type list
- **Upstream compatibility**: dropped; upstream tools do not read dialect files

## Migration

lob-ap (`rfcs/0001-evidence/lob-ap.em.yaml`): the 48 `- v: State /` lines become `s:` (mechanical). Header wording at `:21-25` (phase namespace) and `:45` ("version int (monotonic per stream; expectedVersion on commands)") is rewritten for DCB: a global position replaces the per-stream version, and the append condition replaces `expectedVersion`; `ConcurrentEdit` (`:36`) becomes the append condition failing. The four empty givens (`:106`, `:123`, `:176`, `:701`) become named empty states (`s: Supplier`, `s: ApprovalBatch`). The bulk approval is rewritten per section 2. The ten `em-then-outside-query` findings are fixed by fold tests that read what the decisions write, four of them by the `InvoiceDrafting`, `PaymentRun` and cross-entity models of sections 1, 4 and the rejected alternative. The 17 `em-state-phase-without-fold` findings need four fold tests (`draft`, `approved`, `rejected`, `scheduled`). The three Todo givens gain an `s: PaymentRun` or `s: ApprovalReminder` state. `status` at `:1267` becomes `phase`.

Games (`blindbudet`, `mer-eller-mindre`, `tank-till-tusen`; negative control, counted only): 23, 27 and 25 `- v: State /` lines become `s:`; 5, 7, 6 empty givens become `s: Game`; one `lobby` fold each; three fold additions each for the `Next*` and `*Ended` event types.

No dialect document is a v1.0.0 document once it carries an `s:` element. A project that needs upstream tooling keeps its models upstream; the dialect offers no export.

Every fixture writes spaced lanes and both formatters emit `State/Game`, so the first `em fmt -w` on any of the four files is a whole-file textual diff with no semantic change, and every line number cited in this RFC and in the census moves (`compat.md`, section 3). Format the fixtures in one commit before adopting the dialect rules, or accept that the citations are to the pre-format text.

## Implementation notes (reference implementation)

- The dialect is ahead of its reference implementation. The local runtime and the lob-ap header are stream-per-entity today: `version` per stream and `expectedVersion` on commands (`lob-ap.em.yaml:45`, `:36`), `Decider.Fold` over one event array (`TestsEmitter.cs:127`, `:175`), one `StateType` per generated decider (`EmitTarget.cs:14`). No DCB store is used or abstracted anywhere in `src/emlang`. This RFC states the target; it does not claim the tools meet it.
- Generator target: for each decision model, emit `Evolve(state, event)` and `Decide(state, command, context)` as today (`DeciderEmitter.cs:33-51`), plus a `Query` (the event types read off the fold tests and the tag props read off the identity props) and an append condition (`Query` at the read position), against an abstract DCB store interface with two operations, read by query returning events and position, and append with condition. Whether a .NET DCB store exists to bind that interface to is not verified (open objection 6).
- Rules 2 to 7 operate on `EmDocument` (`EmAst.cs:56`); implement them in `Linter` (`Linter.cs:19-32`) unconditionally. `LintSeverity` already has `Error` (`Linter.cs:3`); `Add` hard-codes `Warning` (`:92`) and needs a severity parameter.
- Query derivation and the closed-model check need a per-document pass: for each state name, collect the fold tests (`when` absent, `given` all events, `then` one state), union the event types, collect pinned `phase` values, and read identity props (`*Id`, `*Ids`) off the state's `steps` declaration (props-richest occurrence, `EmParser.cs:129-133`); then check every decision test's `then` event types and given phase values against it, and every folded event type's declared props against the identity props.
- `TestsEmitter.EmitGiven` (`TestsEmitter.cs:112-133`) is called only from `EmitDecideGwt` (`:100`). In the dialect a decision test's `given` is one state, possibly with views, so the fold arm (`:125-132`) and the empty arm (`:123-124`) are unreachable and can be deleted; the arm at `:114` selects the single state element, treats absent props as the empty state (`StateType.Initial` today, the identity fold under DCB) and ignores accompanying views, which are the automation's input. `EmitBody` (`:84-90`) and `EmitGiven` (`:114`) test `Lane == "State"` today; they test `Kind == 's'` instead.
- The new kind touches the kind tables at `EmAst.cs:66-82`, `EmParser.cs:86-92`, `SpecModel.cs:60-66`, `TestModel.cs:81-84`; `AllowedGiven`/`AllowedThen` at `EmAst.cs:224-227`; `EmFormatter.TypeKey` at `EmFormatter.cs:92-103`; and `EmSpecShape.verified.txt` re-approves once.
- `EmParser.PhaseValues` (`EmParser.cs:145-151`) becomes `IReadOnlyDictionary<string, IReadOnlyList<string>>` keyed by decision model; `EmParser.Merge` (`:72-76`) merges per key; xmlang's `during` resolver reads the map form. One `EmSpecShape` re-approval.
- A local bug to fix before the dialect rules ship, not a spec rule: Go and `EmAst` split the swimlane at the first `/` (`ast.go:85-90`, `EmAst.cs:327`), while `EmParser.Split`, `SpecModel` and `TestModel` split at the last (`EmParser.cs:37`, `SpecModel.cs:83`, `:89`, `TestModel.cs:93`). For a name with two slashes such as `State / Order/Line`, `em lint` sees lane `State` and name `Order/Line`, while `xm` and the generator see lane `State / Order` and the element stops being a state for them (`PhaseValues` filters `Lane == "State"`, `EmParser.cs:147`). With the `s:` kind the state check no longer depends on the lane, but the divergence still affects every event, view and trigger origin, so the fix is to split at the first `/` in the three last-slash sites.
- `SurfaceEmitter` filters on `c`/`e`/`x` (`SpecModel.cs:19-20`) and is unaffected. Go reference parity is not a goal: the Go tools read upstream files; `compat.md` lists what they would need (kind in parser, AST, formatter and diagram templates) should anyone want it.

## Non-goals

- **Upstream emlang v1.0.0 compatibility.** A dialect document with an `s:` element is not a v1.0.0 document, and the dialect offers no export or profile header to make it one. Decision 2026-09-11; RFC 0001 (profiles) withdrawn on the same day.
- **`compensates:` / `reverses:`** (xmlang RFC 0001 debt 5). Dead weight on every testbed: lob-ap models Void and ReversePayment as second events (`lob-ap.em.yaml:28-33`) and xmlang derives nothing from a compensation fact (xmlang RFC 0001 section 1). Re-open when a second consumer needs the fact.
- **`params:` as a key** on views (debt 3). Deferred to the `(@param)` props-note convention and its lint in RFC 0004.
- **A `when:` predicate language** on tests or states. A state's props are the whole precondition.
- **Per-stream version or `expectedVersion` as spec constructs.** The append condition is derived from the query; a model that wants optimistic locking writes a `version` prop like any other.
- **A declared query** (event-type list or tag list) on the state element. Rejected above; the fold tests are the declaration.
- **Navigation semantics** (what the actor sees next, trigger origins, destinations). Trigger origin is RFC 0003; destinations are xmlang's.
- **Code generation for more than one decision model per document.** Legal to lint and to consume; the reference generator's single `StateType` is unchanged by this RFC.
- **An `s: Todo`** or any second decision-model kind. A Todo is a projection an automation reads.

## Open objections (recorded, not resolved)

1. All 185 tests were written by one team on four models, three of them games that xmlang treats as a negative control; "restates practice" rests on one practice, and sections 2 and 3 show the practice does not fully hold even there (26 givens without one state, 19 decisions outside their query, 30 unproduced phase values). No second modeller has written under these rules; a second B2B testbed is intended (`PLAN.md`, Decisions 2026-09-10).
2. Red team R10 (`redteam.md`): Event Modeling's canvas has four lanes and no state box, so `s:` is the first kind with no canvas counterpart, and a new kind is a base-spec change upstream would have to accept. Answer recorded 2026-09-11: the dialect forks the grammar and seeks no upstream acceptance; the canvas objection stands as an objection to drawing, not to the grammar, and the Decision Model slices already have no canvas counterpart under any form.
3. Red team R1 (`redteam.md`): a rule set that is all lints may be a linter configuration, not a spec construct, and `.emlang.yaml` already exists to hold it. Answered 2026-09-11 by withdrawing RFC 0001: the rules are the dialect's base rules, not a switchable profile, and the `s:` kind is grammar, which no config file can carry. The objection stands for the severity table, which remains configuration.
4. Query by example narrows silently. A fold test nobody wrote removes an event type from the model's query with no diagnostic: the decision stops guarding against it and the append condition stops protecting against it. `em-then-outside-query` catches the case where the model itself emits the type; it cannot catch a type another model emits that this one should have read. A declared list would catch it and was rejected for drift; neither option is free.
5. The closed model forces trivial folds. Every summary or notification event a decision emits must be read back by a fold that may do nothing with it (section 2's `completedBulkApprovalIds`), which is ceremony the aggregate rule did not have.
6. The dialect is ahead of its reference implementation: the local runtime is stream-per-entity, no DCB store interface exists in `src/emlang`, and whether a production-grade DCB store is available for .NET was not verified in this pass. A dialect no tool can execute end to end is spec text about tests, not about running software, until that changes.
7. Identity props are found by name (`*Id`, `*Ids`). A model whose identities are named otherwise (`slug`, `email`, `sku`) has no tags and no executable query; the convention is a naming rule imported from these four models.

## Evidence

- Census and re-count: `rfcs/emlang-evidence/census-gwt-state.md` (sections 1 to 5); the re-count scripts for the given-shape table, the one-state baseline (5/7/6/8), the closed-model baseline (3/3/3/10), the tag check (0/0/0/0), the phase-level baseline (4/5/4/17) and the `em lint` `slice-missing-event` counts (9/11/9/13) live in the session scratchpad and are not committed; the red team reproduced the given-shape table independently (`rfcs/emlang-evidence/redteam.md` R23).
- Decisions: `rfcs/emlang-evidence/PLAN.md`, "Decisions (2026-09-10, Martin): all-in on decider + DCB"; the 2026-09-09 decisions (both forms drafted, profile RFC first); 2026-09-11: `s:` canonical, grammar forks, RFC 0001 withdrawn.
- DCB: Pellegrini and Waidelich, Dynamic Consistency Boundaries (dcb.events); not fetched in this pass, terms used as the maintainer fixed them (query over event types and tags, append condition).
- Red team: `rfcs/emlang-evidence/redteam.md` R4 (superseded by the closed-model rule), R5, R6, R7, R8 (superseded by deletion), R9, R11, R12, R13, R14, R21, R22, R23 (applied); R10 (recorded in Open objections 2); Event Modeling quotations (Automation pattern, worked GWT) are taken from its header.
- Design pass: `rfcs/emlang-evidence/cut-lines.md` (section B); compatibility runs against schema, Go `emlang` 1.0.0, `em` and the codegen path: `rfcs/emlang-evidence/compat.md` (sections 1, 2.2, 3, 4).
- Models: `rfcs/0001-evidence/lob-ap.em.yaml` (header `:11-25`, `:36`, `:45`; CreateSupplier `:84-130`; DraftInvoice `:292-340`; Bulk Approve `:662-700`; Execute Payment Run `:907-951`; ReversePayment `:983`, `:993-1000`; VoidInvoice `:1028-1052`; Remind Approvers `:1093-1117`; `status` workaround `:1267`; decision models `:1261`, `:1281`; folds `:1271`, `:1298`, `:1313`, `:1324`; `supplierStatus` context prop `:1289`); `tests/Emlang.Tests/fixtures/blindbudet.em.yaml` (`:22-26`, `:791`, `:831`, `:848`, `:920-986`), `mer-eller-mindre.em.yaml` (`:24-27`, `:1291`), `tank-till-tusen.em.yaml` (`:21-24`, `:1009`).
- Implementation: `src/emlang/Emlang/Linting/EmAst.cs`, `src/emlang/Emlang/EmParser.cs`, `src/emlang/Emlang/SpecModel.cs`, `src/emlang/Emlang/TestModel.cs`, `src/emlang/Emlang/EmitTarget.cs`, `src/emlang/Emlang/Linting/Linter.cs`, `src/emlang/Emlang/TestsEmitter.cs`, `src/emlang/Emlang/DeciderEmitter.cs`, `src/emlang/Emlang/Linting/EmFormatter.cs`, `src/emlang/Emlang.Cli/Program.cs`.
- Upstream: `SPEC.md` sections "Elements", "Swimlanes", "Extended Form", "Multiple Slices", "Multiple Tests", "Tests", "Test Structure", "Document Structure"; `schema.json` `element`, `givenElement`, `thenElement`; Go `README.md` "Linter Rules" (lines 70-81) and "Configuration".
- Consumer: xmlang `rfcs/xmlang-0001-interaction-model.md`, sections 5 and 6, "Debts that belong in emlang" items 3 to 7, "Open objections" item 3.
