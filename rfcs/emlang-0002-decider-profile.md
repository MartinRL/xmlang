---
title: "emlang RFC 0002: The decider profile (draft for spec 1.1.0, profile decider)"
status: draft
created: 2026-09-09
targets: emlang spec 1.1.0 via RFC 0001 (`profile: decider`); consumer xmlang 0.6.0 (`during` per decider)
depends: emlang RFC 0001 (profiles); required by emlang RFC 0004 (lints)
---
# emlang RFC 0002: The decider profile

**Status: draft.** Nothing here is applied to the upstream spec or to `src/emlang`. This RFC defines `profile: decider`, the maintainer's 1:1 map between an Event Model and its deciders. It pays xmlang RFC 0001 debt 4 (phase per decider), answers xmlang RFC 0001 open objection 3 for the State-lane convention, and assigns the profile severity of the actor-identity rule whose text is in emlang RFC 0004. Dependency graph: RFC 0001 is required here; RFC 0004 requires this RFC; RFC 0003 section A stands alone. The state element is drafted in two forms, A and B, with identical rules under both; the maintainer picks on reading.

Throughout, "document" means one YAML document (the unit between `---` separators), as in RFC 0001 and in "Document Structure" ("Each document MUST independently conform"). Every rule scoped to a document is scoped to that unit, never to the file.

## Summary

Under `profile: decider`:

1. A state element exists, either as the reserved swimlane `State` on a view (form A) or as a sixth element kind `s:` (form B). Its name is the decider's name.
2. A test with `when` gives nothing, or exactly one state, optionally accompanied by the non-state views a processor reads; the state and the `then` events name one decider. A test without `when` gives events only.
3. Every state in any `given`, and every phase value pinned there, is produced by the `then` of an events-only test in the same document (by reference, not document order).
4. A Todo view in a decision test's `given` should be accompanied by the decider state the command validates against.
5. Each state may carry `phase: <Enum> (a|b|c)`; phases are namespaced by decider; a bare value shared by two deciders is a warning.
6. Fold tests and Decision Model slices are exempt from upstream's `test-missing-command` and `slice-missing-event`; without that exemption the profile is unadoptable upstream.

Nine lint rules. No schema change under form A; six schema edits under form B.

## Motivation

The maintainer's stated aim: "a 1:1 map to the decider in my dialect of emlang, ie the linter prohibiting event[] as given for gwt, only state (perhaps s: rather than v: ?), and every such state preceding gwt's".

emlang v1.0.0 says nothing about state. "Test Structure" allows `given` to hold "`e` (event), `v` (view)" and the spec's own `EmailMustBeUnique` example (section "Extended Form", lines 137-147) gives an event and then issues a command. Event Modeling's worked specification does the same: "Given: We have registered, and added a payment method" is a list of prior facts, not a folded state. A decider-only `given` is therefore a departure from the method's GWT as written, not a restatement of it. The reason for the departure: a decider reads its folded state, not an event list, so an events-given decision test exercises the fold and the decision at once and pins neither; splitting them gives each fold one test that pins the state and each decision one test that pins the events, and lets a generator arrange a decision test from state without replaying history.

Four local models already write that split, without spec support. Every one carries a header comment saying so: "emlang has no `state` element, so every command/processor slice expresses its Given as the View State / Game" (`tests/Emlang.Tests/fixtures/blindbudet.em.yaml:23-24`; the same at `mer-eller-mindre.em.yaml:24-27`, `tank-till-tusen.em.yaml:21-24`, `rfcs/0001-evidence/lob-ap.em.yaml:11`). The census of their 185 tests (`rfcs/emlang-evidence/census-gwt-state.md`, re-counted for this RFC and independently by the red team, `redteam.md` R23):

| `given` shape | blindbudet | mer-eller-mindre | tank-till-tusen | lob-ap | total |
|---|---|---|---|---|---|
| events only | 12 | 15 | 12 | 15 | 54 |
| exactly one `v: State / X` | 20 | 24 | 22 | 39 | 105 |
| several `v: State / X` | 0 | 0 | 0 | 1 | 1 |
| `v: Todo / X` | 0 | 0 | 0 | 3 | 3 |
| empty | 5 | 7 | 6 | 4 | 22 |
| mixed kinds | 0 | 0 | 0 | 0 | 0 |
| tests | 37 | 46 | 40 | 62 | 185 |

The census file reports 106 State givens; that figure includes the one three-state given at `lob-ap.em.yaml:685-692`. Every one of the 54 events-only givens belongs to a test without `when`; every test with `when` gives state, nothing, or (three times) a Todo view. Every State view that appears in a `given` has a name-level fold test in the same document (`blindbudet.em.yaml:946`, `:962`; `mer-eller-mindre.em.yaml:1291` ff.; `tank-till-tusen.em.yaml:1009` ff.; `lob-ap.em.yaml:1271`, `:1298`, `:1313`, `:1324`). The profile restates this practice where it holds and, in sections 2 and 3, states where it does not: three cross-decider decisions and 30 pinned phase values no fold test produces.

Two costs of leaving it unstated. First, the generator: `TestsEmitter.EmitGiven` (`src/emlang/Emlang/TestsEmitter.cs:112-133`) uses state only when `given` is exactly one State view (`:114`); anything else falls to the fold arm (`:125-132`) and, for the three-state given at `lob-ap.em.yaml:687-692` and the Todo givens at `:933`, `:945`, `:1110`, emits `new Invoice(...)` and `new Due payments(...)` inside an event-array initializer, which does not compile and raises no `SpecTestException`. Second, xmlang: its `during` resolves against a `State`-lane view's `phase` enum (`EmParser.cs:145-151`) and unions every decider's values, so the LOB model had to name the supplier lifecycle `status` to stay out of the namespace (`lob-ap.em.yaml:21-25`, `:1267`). xmlang RFC 0001 records both as emlang debts (items 4 and 7) and as objection 3.

## Proposed normative changes

All rules below apply only to documents that declare `emlang: { version: "1.1.0", profile: decider }` (RFC 0001), or, if the maintainer takes RFC 0001's recorded alternative, to documents linted with `lint.profile: decider`. Outside the profile the lint rules run at the severity given in section 8, or not at all.

### 1. The state element

The rules that follow are identical under both forms; only the element's spelling differs.

- A state element names one decider; the text after the swimlane (form A) or the element name (form B) is the decider's name
- A state element MAY appear in `steps`, `given` and `then`
- A state element's `props` document the decider's folded state; a state element in a test carries only the props that test reads or pins
- Two state elements with the same decider name in one document denote the same decider

State `props` are documentation until a generator emits the state record. Today no generator does: `SurfaceEmitter` emits records for `c`/`e`/`x` only ("'v' elements are inert for record emission", `SpecModel.cs:19-20`), and the state type is a naming convention, `Prefix + "State"` (`EmitTarget.cs:14`), single-valued per spec and hand-written. The profile therefore makes multi-decider documents legal to lint and to consume from xmlang; generating code for a multi-decider document is out of scope for this RFC.

**Form A: `v: State / <Decider>` promoted to normative text.** The swimlane `State` is reserved under the profile: a view whose swimlane, after the parser's whitespace trimming, is exactly `State` is a state element. The name is defined after normalization because both formatters rewrite `State / Invoice` to `State/Invoice` (`ast.go:84-92` with `parser.go:374-375`; `EmAst.cs:330-331` with `EmFormatter.cs:82`), so `State / X` and `State/X` are the same element. Zero schema change. Add to "Swimlanes", under the profile: "The swimlane `State` is reserved; a view in it denotes a decider's folded state."

```yaml
# proposed (form A)
emlang: { version: "1.1.0", profile: decider }
slices:
  ✍️ Submit Invoice:
    steps:
      - t: 🧾 Clerk /Invoice details
      - c: SubmitInvoice
        props: { invoiceId: Guid, expectedVersion: int }
      - x: ConcurrentEdit
      - e: Invoice / InvoiceSubmitted
        props: { invoiceId: Guid, submittedBy: Guid }
      - v: Invoice details
    tests:
      draft can be submitted:
        given:
          - v: State / Invoice
            props: { invoiceId: inv1, phase: draft, version: 1 }
        when:
          - c: SubmitInvoice
            props: { invoiceId: inv1, expectedVersion: 1 }
        then:
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv1 }
  👀 Invoice Decision Model:
    steps:
      - v: State / Invoice
        props:
          invoiceId: Guid
          phase: InvoicePhase (draft|submitted|approved|paid|voided)
          version: int
    tests:
      state folds a draft:
        given:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1 }
        then:
          - v: State / Invoice
            props: { phase: draft, version: 1 }
      state folds a submission:
        given:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1 }
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv1 }
        then:
          - v: State / Invoice
            props: { phase: submitted, version: 2 }
```

**Form B: `s:` as a sixth element kind.** Add a row to the "Elements" table: State, short `s:`, acronym `st:`, long `state:`. Amend "Emlang defines 5 element types" to 6. Amend "Test Structure": `given` allows `e`, `v`, `s`; `then` allows `e`, `v`, `x`, `s`. The name is the decider's name; a swimlane is permitted but carries no meaning.

```yaml
# proposed (form B)
emlang: { version: "1.1.0", profile: decider }
slices:
  ✍️ Submit Invoice:
    steps:
      - t: 🧾 Clerk /Invoice details
      - c: SubmitInvoice
        props: { invoiceId: Guid, expectedVersion: int }
      - x: ConcurrentEdit
      - e: Invoice / InvoiceSubmitted
        props: { invoiceId: Guid, submittedBy: Guid }
      - v: Invoice details
    tests:
      draft can be submitted:
        given:
          - s: Invoice
            props: { invoiceId: inv1, phase: draft, version: 1 }
        when:
          - c: SubmitInvoice
            props: { invoiceId: inv1, expectedVersion: 1 }
        then:
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv1 }
  👀 Invoice Decision Model:
    steps:
      - s: Invoice
        props:
          invoiceId: Guid
          phase: InvoicePhase (draft|submitted|approved|paid|voided)
          version: int
    tests:
      state folds a draft:
        given:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1 }
        then:
          - s: Invoice
            props: { phase: draft, version: 1 }
      state folds a submission:
        given:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: inv1 }
          - e: Invoice / InvoiceSubmitted
            props: { invoiceId: inv1 }
        then:
          - s: Invoice
            props: { phase: submitted, version: 2 }
```

Form B schema change, `element` (`schema.json:117-153`); `givenElement` (`:154-173`) and `thenElement` (`:190-215`) gain the same three properties and three `oneOf` arms. Before:

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

Form B breaks RFC 0001's rule that a profile document with the header removed is a valid base document: a v1.0.0 validator rejects `s:` in `element`, `givenElement` and `thenElement`; the Go CLI and `em` throw `unknown key "s"` (`parser.go:380`, `EmAst.cs:340`); the lenient local parsers drop an `s:` step without a word (`SpecModel.cs:68-78`, `EmParser.cs:109-110`) and fail the whole generator run on an `s:` in `given` or `then` with `InvalidDataException` at `TestModel.cs:101` (`compat.md`, section 1, rows 4 to 6). If B is chosen, `s:` MUST be added to the base spec (a 1.1.0 element, legal in every document) and the decider profile MUST then only require its use; the profile itself cannot introduce a kind.

#### Choosing between A and B

The case for A. Event Modeling's picture has four lanes (trigger, command, event, view) and state is a read model: the decider's own projection of its stream, drawn as a green box like any other. The five kinds each map to a box on the canvas; `s:` would be the first kind with no canvas counterpart. A breaks nothing: no schema change, no change to the Go reference tools, no `em fmt` change, and all 185 existing tests are already written in it. The generator already keys on the lane text (`TestsEmitter.cs:86`, `:114`; `EmParser.cs:147`).

The case for B. An explicit kind states what the element is; A reserves a piece of prose and depends on a trimmed string compare, and a human screen named `State` (an "Order status" page a modeller calls `State /`) becomes illegal by accident. Under B the generator, the linter and xmlang's `during` key on `EmElementType.State`, never on lane text, and `em fmt` cannot normalize the meaning away. B is also the maintainer's own first instinct ("perhaps s: rather than v:").

Consequences:

| Concern | A: reserved `State` lane | B: `s:` kind |
|---|---|---|
| `schema.json` | none | six edits: `properties` and `oneOf` in each of `element`, `givenElement`, `thenElement` (`compat.md` 2.2) |
| Upstream SPEC text | one bullet under "Swimlanes" (profile-scoped) | "Elements" table row, count 5 to 6, "Test Structure" table, all base text |
| RFC 0001 strip rule | holds | violated unless `s:` enters the base spec |
| Go reference tools | no change | four files: `parser.go:14-29` and `:253-255`, `ast.go:41-64`, `formatter.go:17-33`, `main.go:117-124` per-type colours; plus diagram templates (not fetched) |
| Local toolchain | none | eleven sites: `EmElementType` and `Display` (`EmAst.cs:13`, `:17-25`), `Prefixes` (`:66-82`), `AllowedGiven`/`AllowedThen` (`:224-227`), `EmFormatter.TypeKey` (`EmFormatter.cs:92-103`), `SpecModel.Kinds` (`SpecModel.cs:60-66`), `TestModel.Kinds` (`TestModel.cs:81-84`), `EmParser.Kinds` (`EmParser.cs:86-92`), `TestsEmitter` classification (`TestsEmitter.cs:84-90`, `:114`), `EmSpec.FindView`/`PhaseValues` (`EmParser.cs:24-28`, `:145-151`) |
| Behaviour today | passes every surface; already the generator's classification key | rejected by schema, Go, `em lint`, `em fmt`; silently dropped in `steps` by `SpecModel`/`EmParser`; `InvalidDataException` at `TestModel.cs:101` in `given`/`then` |
| Generator | keeps `Lane == "State"` checks | replaces them with a kind check; `EmitTarget.StateType` (name convention) unchanged either way |
| xmlang | `during` map keys stay `"State / Invoice"` | `during` map keys become the decider name `Invoice`; `EmParser.PhaseValues` filters on kind |
| Existing models | zero edits | 123 `- v: State /` lines rewritten to `s:` across four files (23, 27, 25, 48; mechanical) |
| Collision risk | a screen called `State` is illegal under the profile | none |
| Dual specification (`redteam.md` R11) | state props are documentation nothing checks | the same, with a dedicated kind asserting a shape nothing verifies |
| Red team verdict (`redteam.md` R10) | accept | withdraw: a base-spec kind change wearing a profile; the plan's stand-alone claim fails; first kind with no canvas counterpart. Re-open condition from `cut-lines.md` section B: a second tool needs to distinguish state from view without the profile header |

Decision: maintainer, on reading.

### 2. Given shape and one decider per decision

Amend "Test Structure" under the profile. The current text reads "If `given` is present and non-empty, its elements MUST be events or views". Add:

- A test whose `when` is present and non-empty is a decision test; its `given` MUST be empty, or exactly one state element optionally accompanied by non-state views
- A test whose `when` is absent or empty is a fold or projection test; its `given` MUST contain events only
- An event in a decision test's `given` MUST be reported as an error (`em-given-events-in-decision`)
- A `given` that holds elements of more than one kind MUST be reported as an error (`em-given-mixed`)
- A decision test whose `given` holds more than one state element MUST be reported as an error (`em-given-multi-state`)
- In a decision test whose `given` holds a state element, every event in `then` MUST carry the swimlane of that state's decider; a `then` event on another swimlane MUST be reported as an error (`em-given-decider-mismatch`)

An empty `given` is the decider's initial state; the models use it for the empty-stream rejection (`blindbudet.em.yaml:139`, `:153`; `mer-eller-mindre.em.yaml:273`, "GameNotFound is the empty stream") and it is the conforming given for a command that creates an instance.

The last bullet is what makes the map 1:1. Without it a decision test may give one decider's state and emit another decider's events, which is the cross-decider decision the multi-state rule rejects when there are three states. Three lob-ap tests do so today. `✍️ Draft Invoice` (`:292-340`) gives `v: State / Supplier` (`:324`, `:334`) and emits `e: Invoice / InvoiceDrafted` (`:330`). `✍️ Reverse Payment` gives `State / Invoice` and emits `Payment / PaymentReversed` (`:993-1000`, event declared at `:983`). `✍️ Void Invoice` gives `State / Invoice` and emits `Invoice / InvoiceVoided` together with `Ledger / LiabilityReversed` (`:1042-1052`, events at `:1028`, `:1034`). The conforming rewrite of the DraftInvoice success case, using the initial state of the decider that owns the event:

```yaml
# proposed rewrite of lob-ap.em.yaml:322-331
      draft can be saved for an active supplier:
        given: []                      # a new invoice: the Invoice decider's initial state
        when:
          - c: DraftInvoice
            props: { supplierId: acmeId, invoiceNumber: A-2026-0042, currency: SEK, lines: [line1], costCenter: CC-100 }
        then:
          - e: Invoice / InvoiceDrafted
            props: { invoiceId: minted, supplierId: acmeId, invoiceNumber: A-2026-0042, currency: SEK, lines: [line1], createdBy: annaId }
```

The rejection case, `cannot draft against an inactive supplier` (`:332-340`), reads the supplier's status, which the model already declares as a context dependency on the Invoice state (`supplierStatus: SupplierStatus   # via SupplierContext dependency`, `:1289`; the same pattern as `ExceedsApprovalLimit`, `:37-38`). emlang has no construct for a context dependency in a test, so under the profile that test either gives `State / Invoice` with `supplierStatus: inactive` set on the initial state, treating the dependency as a state prop, or remains an error until a context construct exists. For `ReversePayment` and `VoidInvoice` the conforming forms are to put the event on the deciding stream (`Invoice / PaymentReversed`, `Invoice / LiabilityReversed`) or to move the second-stream event into a processor slice that reacts to the Invoice event. The profile does not choose.

`em-given-events-in-decision` and `em-given-mixed` fire on none of the 185 tests. `em-given-multi-state` fires once, on `lob-ap.em.yaml:685-692`: `ApproveInvoices` is given three `State / Invoice` instances and emits one `Invoice / InvoiceApproved` plus a `BulkApproval / BulkApprovalCompleted` (`:697-700`); the conforming forms are a `BulkApproval` decider whose state carries the candidate rows, or a processor that issues one `ApproveInvoice` per row. `em-given-decider-mismatch` fires three times, all in lob-ap (`:322`, `:993`, `:1042`), none in the games.

### 3. Fold by reference, at name and phase level

- Every state element that appears in any `given` MUST be the `then` of at least one fold test (a test without `when` whose `given` holds events only) in the same document; a violation MUST be reported as an error (`em-state-without-fold`)
- Every phase value pinned on a state element in a decision test's `given` MUST be pinned on that state in the `then` of at least one fold test in the same document; a violation MUST be reported as an error (`em-state-phase-without-fold`)
- The fold test MAY appear anywhere in the document; document order carries no meaning

The maintainer's phrase is "every such state preceding gwt's". Reference, not order, is the right axis for two reasons. First, all four models put the fold tests in a trailing slice explicitly marked as an appendix: `👀 Decision Model` at `blindbudet.em.yaml:920`, `mer-eller-mindre.em.yaml:1291`, `tank-till-tusen.em.yaml:1009`; `👀 Supplier Decision Model` and `👀 Invoice Decision Model` at `lob-ap.em.yaml:1261` and `:1281` under the comment "APPENDIX, not timeline steps" (`:1259`). A document-order rule would fail all 106 state-given tests today. Second, the upstream spec attaches no meaning to slice order ("Multiple Slices": "Multiple slices MAY be defined in the same document", nothing more), and xmlang RFC 0001 reads slice order as the timeline for other purposes; the fold belongs outside that timeline.

The phase-level rule is the one that carries weight. The name-level check passes on all four models (`em-state-without-fold` fires on none of 185), but it demonstrates only that some fold reaches the decider, not that the state a decision starts from is reachable. Counted mechanically: phase values pinned in decision givens that no fold test in the document produces are, in lob-ap, `draft` (8 givens), `approved` (7), `rejected` (1) and `scheduled` (1), 17 of 40 state givens, against folds that produce only `submitted`, `onHold` and `paid` (`:1298-1336`); in the games, `lobby` in 4, 5 and 4 givens, against folds that produce only `started` (`blindbudet.em.yaml:946-986`). So `em-state-phase-without-fold` fires 17/4/5/4 today, and the fix is one fold test per unproduced value (four in lob-ap, one per game). After the `status` to `phase` rename of section 5, `Supplier.active` (`:325`) joins the lob-ap list, since the Supplier fold produces only `inactive` (`:1271-1279`).

The name-level rule is kept as the weaker fallback because it is the only one that reaches a decider without a `phase` prop, and because a state pinned without a phase value in a given (the games pin `players`, `currentLotIndex`, `lots` beside `phase`) still needs some fold to name it. Both rules are scoped to the document, not the file, so a fold in a sibling YAML document does not count; a model that spans documents repeats the fold or merges the documents.

### 4. Todo givens

- A decision test whose `given` holds a non-state view (form A: any swimlane other than `State`; form B: any `v:`) SHOULD also hold the state element of the decider the command validates against; a Todo given without an accompanying state MUST be reported as a warning (`em-given-todo`)

Event Modeling's Automation pattern is "Event(s) -> View -> Automated Trigger -> Command -> Event(s)", and "the view that the automated process monitors, is a simple todo list. For each row the automated process calls a use case, which provides a new event". A Todo in the `given` of an automation slice is therefore the method's own shape, not a smell, and lob-ap models it so (`✍️ Execute Payment Run`, `:907-912`; `✍️ Remind Approvers`, `:1093-1098`). What the Todo does not carry is the decider's guard: the processor selects the row, the decider validates it. The rule asks for both, per row, since the use case is called per row. Written out for `lob-ap.em.yaml:931-942`:

```yaml
# proposed rewrite of lob-ap.em.yaml:931-942
      due invoice is paid by the run:
        given:
          - v: Todo / Due payments
            props: { asOf: 2026-09-30, due: [dueInv1], hasDue: true }
          - v: State / Invoice
            props: { invoiceId: inv1, phase: scheduled, version: 6 }
        when:
          - c: ExecutePaymentRun
            props: { runDate: 2026-09-30 }
        then:
          - e: Payment / PaymentRunExecuted            # em-given-decider-mismatch: see section 2
            props: { paymentRunId: minted, runDate: 2026-09-30, payments: [paidInv1], totalAmount: 12500 }
          - e: Invoice / InvoicePaid
            props: { invoiceId: inv1, paymentRunId: minted, bankReference: ref1 }
```

The rewrite exposes a second finding: `PaymentRunExecuted` is on the `Payment` swimlane while the given state is `Invoice`, so section 2's one-decider rule fires on the corrected test too. That is the model telling the truth about a payment run being its own decider, and the profile leaves the split to the modeller. `em-given-todo` fires three times today, at `lob-ap.em.yaml:933`, `:945` and `:1110`, none in the games. The `nothing due means no run` case (`:943-951`) has no invoice to give; under the rule it stays a warning unless a `PaymentRun` decider is introduced.

### 5. Phase per decider

- A state element MAY carry a prop named `phase` whose declared type is an enum note of the form `<Enum> (a|b|c)`
- Phase values are namespaced by decider: `Invoice.scheduled` and `Supplier.active` are distinct values even when spelled alike
- A bare phase value declared by the `phase` enum of more than one decider in the same document SHOULD be reported as a warning (`em-phase-ambiguous`)
- A second decider MUST NOT be required to avoid the name `phase`

The last bullet is the debt. `lob-ap.em.yaml:1267` reads `status: SupplierStatus (active|inactive)   # NOT named phase: keeps the xmlang phase namespace to State / Invoice`, with the reason at `:21-25`. Under the profile it becomes `phase: SupplierPhase (active|inactive)`; no value collides with `InvoicePhase` (`:1286`). `em-phase-ambiguous` is a warning, not an error, because the namespace makes a shared value legal by definition and the only consumer that could confuse two `draft`s, xmlang's list form of `during`, already has the map form (xmlang RFC 0001, Summary item 4, normative section 5, `xm-ambiguous-phase`); a consumer's convenience is not a spec error. The warning remains because a shared bare value forces every list-form `during` in the consumer onto the map form. It fires on none of the four models.

How xmlang consumes it: `during` in map form, `during: { "State / Invoice": [draft, rejected] }` under form A or `{ Invoice: [draft, rejected] }` under form B; `EmSpec.PhaseValues` (`EmParser.cs:22`, computed at `:145-151`) becomes a map from decider name to values; `xm-unknown-phase` and `xm-phase-uncovered` resolve per decider; `xm-ambiguous-phase` is the consumer-side twin of `em-phase-ambiguous`.

### 6. Actor identity

- Every event in a decision test's `then` MUST carry the actor-identity prop defined by the convention in emlang RFC 0004; a violation MUST be reported as an error under the profile (`em-actor-identity`)

RFC 0004 defines the convention (which prop names who acted, so xmlang's `self:` and its selection default resolve against the same prop) and stays non-normative; this RFC assigns the profile severity, because a non-normative appendix cannot make a document non-conforming and a later RFC must not re-severity an earlier profile's rules. No baseline is published here: the red team (`redteam.md` R18) shows RFC 0004's rule text needs role normalization (every model's trigger roles carry an emoji, `blindbudet.em.yaml:343`, `lob-ap.em.yaml:294`) before the rule can be run, and the count belongs to RFC 0004 once it is.

### 7. Upstream's documented lints

The profile mandates a Decision Model slice, an artefact Event Modeling's canvas does not have (all four patterns are timeline slices; the models mark it "APPENDIX, not timeline steps", `lob-ap.em.yaml:1259`). Upstream's `README.md` "Linter Rules" table (lines 70-81) documents `test-missing-command` at severity **error**, "Test without command (when)", and `slice-missing-event` at warning, "Slice without events". Every fold and projection test is a test without `when` (54 of 185 today: 12/15/12/15), and `slice-missing-event` is implemented (`linter.go`, ported at `Linter.cs:65-67`) and fires on every Decision Model and projection slice: `em lint` 0.2.0 reports 9, 11, 9 and 13 findings on blindbudet, mer-eller-mindre, tank-till-tusen and lob-ap. `test-missing-command` appears in the README only; `compat.md` section 4 finds no implementation in `linter.go`, and RFC 0004 records the `slice-missing-event` counts without proposing a change.

- Under the profile, a test without `when` whose `then` holds exactly one state or view element is a fold or projection test and MUST NOT be reported by `test-missing-command`
- Under the profile, a slice whose `steps` hold only state elements is a Decision Model slice and MUST NOT be reported by `slice-missing-event`
- Upstream SHOULD retract `test-missing-command` from the documented table or downgrade it to a warning, since a fold test is the spec's own "Multiple Tests" example shape (`TodoCompleteRegistrationFlow`, a test with no `when`, lines 231-232)

Said plainly: without these two exemptions the profile requires an artefact that upstream's documented rules condemn, and it is unadoptable upstream. With them, upstream's implemented linter changes one rule's scope.

### 8. Lint rules

| Rule | Under `profile: decider` | Outside the profile | Fires today |
|---|---|---|---|
| `em-given-events-in-decision` | error | off | none of 185 |
| `em-given-mixed` | error | warning | none of 185 |
| `em-given-multi-state` | error | off | `lob-ap.em.yaml:685` |
| `em-given-decider-mismatch` | error | off | `lob-ap.em.yaml:322`, `:993`, `:1042` |
| `em-state-without-fold` | error | off | none of 185 |
| `em-state-phase-without-fold` | error | off | 17 (lob-ap), 4 (blindbudet), 5 (mer-eller-mindre), 4 (tank-till-tusen) |
| `em-given-todo` | warning | off | `lob-ap.em.yaml:933`, `:945`, `:1110` |
| `em-phase-ambiguous` | warning | warning | none of 4 models |
| `em-actor-identity` | error | off | not measured; rule text in RFC 0004 needs role normalization first |

`em-given-events-in-decision` is off outside the profile because the upstream spec's own example is events-given with `when`. `em-given-mixed` and `em-phase-ambiguous` stay on as warnings because they indicate a confused model under any reading.

### 9. Upstream text touched

- "Swimlanes" (form A only): one profile-scoped bullet reserving `State`.
- "Elements" (form B only): "Emlang defines 5 element types" becomes 6; a new table row State, `s:`, `st:`, `state:`.
- "Test Structure": the table rows `given` and `then` (form B adds `s`); after "If `given` is present and non-empty, its elements MUST be events or views", the six profile bullets of section 2.
- "Profiles" (new in RFC 0001): a subsection "decider" holding sections 1 to 7 of this RFC.
- `README.md` "Linter Rules": the exemptions and the retraction of section 7.

## Changelog entry (draft)

### profile `decider` 1 (against spec 1.1.0)

- **State element**: form A (reserved `State` swimlane) or form B (`s:`/`st:`/`state:` kind); decision recorded on adoption; state props are documentation until a generator emits the record
- **Given shape**: decision tests give nothing or one state (plus the views a processor reads); fold and projection tests give events; the state and the `then` events name one decider. `em-given-events-in-decision`, `em-given-mixed`, `em-given-multi-state`, `em-given-decider-mismatch`
- **Fold by reference** at name and phase level, scoped to the document. `em-state-without-fold`, `em-state-phase-without-fold`
- **Todo givens** should be accompanied by the decider state. `em-given-todo`
- **Phase per decider**: namespaced by decider; shared bare values warn. `em-phase-ambiguous`
- **Actor identity**: error under the profile; convention in RFC 0004. `em-actor-identity`
- **Upstream lints**: fold tests exempt from `test-missing-command`, Decision Model slices exempt from `slice-missing-event`

## Migration

Games (`blindbudet`, `mer-eller-mindre`, `tank-till-tusen`): add the header; one `em-state-phase-without-fold` finding per pinned `lobby` (4, 5, 4 givens), fixed by one fold test each (`AuctionOpened` alone folds to `phase: lobby`); no other findings under form A. Under form B, rewrite `v: State / Game` to `s: Game` (23, 27 and 25 lines, givens, steps and folds together).

lob-ap: add the header; errors: one `em-given-multi-state` (`:685`), three `em-given-decider-mismatch` (`:322`, `:993`, `:1042`), 17 `em-state-phase-without-fold` fixed by four fold tests (`draft`, `approved`, `rejected`, `scheduled`); warnings: three `em-given-todo` (`:933`, `:945`, `:1110`); rename `status` to `phase` at `:1267` (optional, and the reason the debt exists; adds one `active` fold). Under form B the same rewrite as the games (48 lines).

Documents outside the profile: none. A profile document with the header removed is a valid v1.0.0 document under form A. Under form B it is valid only once `s:` is in the base spec.

Independent of A or B: every fixture writes spaced lanes (`State / Game`, `Clerk /Invoice details`) and both formatters emit `State/Game`, so the first `em fmt -w` on any of the four files is a whole-file textual diff with no semantic change, and every line number cited in this RFC and in the census moves (`compat.md`, section 3). Format the fixtures in one commit before adopting the profile, or accept that the citations are to the pre-format text.

## Implementation notes (reference implementation)

- Rules 2 to 7 operate on `EmDocument` (`EmAst.cs:56`); implement them in `Linter` (`Linter.cs:19-32`) gated on the header from RFC 0001 (or on `lint.profile` from `.emlang.yaml`, `Program.cs:63-71`, if that route is taken). `LintSeverity` already has `Error` (`Linter.cs:3`); `Add` hard-codes `Warning` (`:92`) and needs a severity parameter.
- Fold by reference needs a per-document pass after the sub-document is parsed: collect state names and pinned `phase` values in any `then` of a `when`-less events-only test, then check every decision `given`.
- `TestsEmitter.EmitGiven` (`TestsEmitter.cs:112-133`) is called only from `EmitDecideGwt` (`:100`). Under the profile a decision test's `given` is empty, one state, or one state plus non-state views, so the fold arm (`:125-132`) is unreachable and can be deleted; the arm at `:114` should select the single state element and ignore accompanying views, which are the processor's input, not decider state. The two uncompilable emissions (three-state, Todo) become lint errors before generation. `EmitBody` (`:84-90`) and `EmitGiven` (`:114`) test `Lane == "State"`; under form B they test `Kind == 's'`.
- Form B touches the kind tables at `EmAst.cs:66-82`, `EmParser.cs:86-92`, `SpecModel.cs:60-66`, `TestModel.cs:81-84`; `AllowedGiven`/`AllowedThen` at `EmAst.cs:224-227`; `EmFormatter.TypeKey` at `EmFormatter.cs:92-103`; and `EmSpecShape.verified.txt` re-approves once.
- `EmParser.PhaseValues` (`EmParser.cs:145-151`) becomes `IReadOnlyDictionary<string, IReadOnlyList<string>>` keyed by decider; `EmParser.Merge` (`:72-76`) merges per key; xmlang's `during` resolver reads the map form. One `EmSpecShape` re-approval.
- A local bug to fix before the profile ships, not a spec rule: Go and `EmAst` split the swimlane at the first `/` (`ast.go:85-90`, `EmAst.cs:327`), while `EmParser.Split`, `SpecModel` and `TestModel` split at the last (`EmParser.cs:37`, `SpecModel.cs:83`, `:89`, `TestModel.cs:93`). For a name with two slashes such as `State / Order/Line`, `em lint` sees lane `State` and name `Order/Line`, while `xm` and the generator see lane `State / Order` and the element stops being a state for them (`PhaseValues` filters `Lane == "State"`, `EmParser.cs:147`). The divergence affects every event, view and trigger origin, so the fix is to split at the first `/` in the three last-slash sites; no `em-state-name-slash` rule is proposed.
- `SurfaceEmitter` filters on `c`/`e`/`x` (`SpecModel.cs:19-20`) and is unaffected by either form; `EmitTarget.StateType` (`EmitTarget.cs:14`) stays single-valued, which is why multi-decider codegen is out of scope.
- Go reference parity, from `compat.md`: under form A the reference tools need no change and `emlang fmt` already normalizes the lane text; under form B they need the kind in parser, AST, formatter and diagram templates.

## Non-goals

- **`compensates:` / `reverses:`** (xmlang RFC 0001 debt 5). Dead weight on every testbed: zero undo pairs in the three games; lob-ap models Void and ReversePayment as second events (`lob-ap.em.yaml:28-33`) and xmlang derives nothing from a compensation fact (xmlang RFC 0001 section 1, "xmlang MUST NOT derive `confirm` from compensation"). Re-open when a second consumer needs the fact.
- **`params:` as a key** on views (debt 3). Deferred to the `(@param)` props-note convention and its lint in RFC 0004; a key would break the closed `element` schema for one consumer.
- **A `when:` predicate language** on tests or states. A state's props are the whole precondition.
- **A context-dependency construct** in tests. Section 2 shows the need (`SupplierContext`, `ApproverContext`); it is a base-spec question, not a profile rule.
- **Navigation semantics** (what the actor sees next, trigger origins, destinations). Trigger origin is RFC 0003; destinations are xmlang's.
- **Multi-decider code generation.** Legal to lint and to consume; the reference generator's single `StateType` is unchanged by this RFC.
- **A `State / Todo`** or any second decider kind. A decider is named by its state element; a Todo is a projection.

## Open objections (recorded, not resolved)

1. All 185 tests were written by one team on four models, three of them games that xmlang treats as a negative control; "restates practice" rests on one practice, and sections 2 and 3 show the practice does not fully hold even there (three cross-decider decisions, 30 unproduced phase values). No second modeller has written under these rules.
2. Under form A, reserving `State` as a swimlane collides with any model whose human screen is called `State`; the profile makes an existing legal name illegal by prose.
3. A profile whose rules are all lints may be a linter configuration, not a spec construct, and RFC 0001 records `.emlang.yaml` `lint.profile` as the alternative. The answer given here is that the profile narrows the meaning of `given`, which RFC 0001 section 2 permits and which is spec text: in v1.0.0 a `given` is "Pre-conditions (events, views)" (`schema.json:72`); in the profile a decision test's `given` is the decider's state and a fold test's `given` is its history, and a generator or a reader relies on that meaning whether or not a linter runs. The profile widens nothing: every profile document is a valid v1.0.0 document. The objection stands for the severity table, which is configuration, and it stands in full if the documents never leave the project that holds the config file.

## Evidence

- Census and re-count: `rfcs/emlang-evidence/census-gwt-state.md` (sections 1 to 5); the re-count scripts used for the given-shape table, the phase-level baseline (17/4/5/4), the decider mismatches and the `em lint` `slice-missing-event` counts (9/11/9/13) live in the session scratchpad and are not committed; the red team reproduced the given-shape table independently (`rfcs/emlang-evidence/redteam.md` R23).
- Red team: `rfcs/emlang-evidence/redteam.md` R4, R5, R6, R7, R8, R9, R11, R12, R13, R14, R21, R22, R23 (applied); R10 (recorded in the consequences table); Event Modeling quotations (Automation pattern, worked GWT) are taken from its header.
- Design pass: `rfcs/emlang-evidence/cut-lines.md` (section B); plan and decisions: `rfcs/emlang-evidence/PLAN.md`; compatibility runs against schema, Go `emlang` 1.0.0, `em` and the codegen path: `rfcs/emlang-evidence/compat.md` (sections 1, 2.2, 3, 4; probe files `p3-s-steps`, `p4-s-given`, `p5-s-then`, `p6-two-t`).
- Models: `rfcs/0001-evidence/lob-ap.em.yaml` (header `:11-25`; DraftInvoice `:292-340`; multi-state given `:685-692`; Execute Payment Run `:907-951`; ReversePayment `:983`, `:993-1000`; VoidInvoice `:1028-1052`; Todo givens `:933`, `:945`, `:1110`; `status` workaround `:1267`; decision models `:1261`, `:1281`; folds `:1271`, `:1298`, `:1313`, `:1324`); `tests/Emlang.Tests/fixtures/blindbudet.em.yaml` (`:22-26`, `:920-986`), `mer-eller-mindre.em.yaml` (`:24-27`, `:1291`), `tank-till-tusen.em.yaml` (`:21-24`, `:1009`).
- Implementation: `src/emlang/Emlang/Linting/EmAst.cs`, `src/emlang/Emlang/EmParser.cs`, `src/emlang/Emlang/SpecModel.cs`, `src/emlang/Emlang/TestModel.cs`, `src/emlang/Emlang/EmitTarget.cs`, `src/emlang/Emlang/Linting/Linter.cs`, `src/emlang/Emlang/TestsEmitter.cs`, `src/emlang/Emlang/DeciderEmitter.cs`, `src/emlang/Emlang/Linting/EmFormatter.cs`, `src/emlang/Emlang.Cli/Program.cs`.
- Upstream: `SPEC.md` sections "Elements", "Swimlanes", "Extended Form", "Multiple Slices", "Multiple Tests", "Tests", "Test Structure", "Document Structure"; `schema.json` `element`, `givenElement`, `thenElement`; Go `README.md` "Linter Rules" (lines 70-81) and "Configuration".
- Consumer: xmlang `rfcs/0001-interaction-model.md`, sections 5 and 6, "Debts that belong in emlang" items 3 to 7, "Open objections" item 3.
