# P2 / agent 7 — Sense C (interaction idioms): admission verdicts

Inputs: BRIEF.md incl. the LOB amendment; XSPEC v0.5.0; p1-mbui, p1-ixd, p1-derivability (DER), p1-residue (RES); p2-senseA (already landed; its E2/D5 are reused, not re-derived). `p1-lob.md` had **not** landed, so §0 is my own minimal accounts-payable slice set, marked as such. Repo evidence re-verified: `RenderModel.cs`, the three `*.xm.yaml`, `AuctionSurfaces.cs` (AS), `GameSurfaces.cs` (GS), `TankSurfaces.cs` (TS), `*Endpoints.cs` (AE/GE/TE), `PuzzleScreen.razor` (PS), `DirectionResultsScreen.razor` (DR), `ActionForm.razor`, `XmLinter.cs`, `EmParser.cs`. Where a Phase 1 line number was stale I cite the verified one (GE `Describe` is at 290-308, not 501-519; TE at 219-231).

## Lead findings

1. **Two keys pass all five tests; both are judgments about a command or a view, neither is an idiom in the rendering sense.** `confirm:` (command item, boolean) and `heading:` (view item, one field name). Everything the brief lists as an idiom proper — inline vs modal, optimistic vs confirmed, wizard chunking, posture, waiting surfaces, error display regime, auto-submit on expiry — fails test 1, test 3 or an existing Non-goal, and stays concrete.
2. **The interpreter's eight "residue chrome slots" split 1 DATA / 5 COPY / 1 TRANSPORT / 1 EMLANG-GAP, zero IDIOM** (§1a). The de-facto interaction vocabulary the live product needed is copy plus one titling judgment, which is why the planned `CommandInput` idiom enum stalled at `Keypad` (`RenderModel.cs:7`) and MEM/TTT opted out (GS:54-55, TS:48-49): input idioms are concrete and nobody wanted them as data.
3. **Enterprise design systems corroborate exactly two per-action judgments and record them as annotations on the action or entity.** SAP Fiori elements: `Common.IsActionCritical: true` on an action yields the confirmation ("Actions that require user confirmation are called critical actions"; default text "Confirm This Action?") — https://github.com/SAP-docs/sapui5/blob/main/docs/06_SAP_Fiori_Elements/adding-confirmation-popovers-for-actions-87130de.md (via search summary; sap.com returned 403). `UI.HeaderInfo` names the title field of an object page (SAP UI vocabulary, https://github.com/SAP/odata-vocabularies — not fetched, exact wording `[citation needed]`). Carbon: "Danger modal is a specific kind of transactional modal used for destructive or irreversible actions" (https://raw.githubusercontent.com/carbon-design-system/carbon-website/main/src/pages/components/modal/usage.mdx). Salesforce: a modal is "a warning mechanism to ensure the user action is intentional and not accidental", a toast the confirmation for intentional actions (https://winter-20.lightningdesignsystem.com/guidelines/messaging/overview/); `lightning-confirm` and the `destructive` button variant are per-action (https://developer.salesforce.com/docs/component-library/bundle/lightning-confirm). Fluent: alert dialogs "only in cases of potential loss, like unsaved changes or confirming destructive actions" (https://fluent2.microsoft.design/components/web/react/core/dialog/usage). Redwood ships `oj-sp-message-dialog-destructive` (https://www.oracle.com/webfolder/technetwork/redwood/message-dialog-destructive-action.pdf, via search summary; 403 on fetch). NN/g: "consider a confirmation dialog before actions that cannot be undone" and "do go to great lengths to provide undo" (https://www.nngroup.com/articles/confirmation-dialog/). Every other "recurring" LOB item (drafts, bulk, approval reason, locking conflicts, audit disclosure) is either an Event Model fact or existing xmlang vocabulary (§3).
4. **Undo does not belong in xmlang.** "VoidInvoice compensates PostInvoice" is a domain fact; recording it in the Experience Model makes xmlang assert behaviour, the MBUI dual-specification failure (p1-mbui F2). emlang gets `compensates:`; xmlang consumes it as the default that switches a command from friction to undo (§5).
5. **The remaining cross-game variance is copy, and copy is a `labels` gap, not an interaction stratum**: exception labels (RES R24; AE:199-210, GE:290-308, TE:219-231), enum value labels, templated sub-headings, per-item status tags. §4 gives exact shapes; only one (exception labels) has any interaction-design pedigree (Nielsen H9).

## 0. LOB testbed used for test 2 (own construction)

No `p1-lob.md` existed at writing time. The following accounts-payable slice set is mine: the smallest model with drafts, approval, correction, bulk, master data and four roles. Names below are used as if from `ap.em.yaml`.

```yaml
# ap.em.yaml — OWN CONSTRUCTION for test 2, not a shipped testbed (compact form)
slices:
  ✍️ Draft Invoice:       [t: Clerk /Invoice list, c: DraftInvoice {vendorId, invoiceNumber, amount, dueDate, lines[]}, e: AP / InvoiceDrafted, v: Invoice]
  ✍️ Amend Draft:         [t: Clerk /Invoice, c: AmendDraft {invoiceId, ...}, x: InvoiceNotDraft, e: AP / DraftAmended, v: Invoice]
  ✍️ Discard Draft:       [t: Clerk /Invoice, c: DiscardDraft {invoiceId}, x: InvoiceNotDraft, e: AP / DraftDiscarded, v: Invoice list]
  ✍️ Submit For Approval: [t: Clerk /Invoice, c: SubmitForApproval {invoiceId}, x: InvoiceNotDraft, x: MissingLines, e: AP / InvoiceSubmitted, v: Approval queue]
  ✍️ Approve Invoice:     [t: Approver /Approval queue, c: ApproveInvoice {invoiceId}, x: NotSubmitted, x: StaleVersion, e: AP / InvoiceApproved, v: Invoice]
  ✍️ Bulk Approve:        [t: Approver /Approval queue, c: BulkApprove {invoiceIds: Guid[]}, x: NoneSelected, e: AP / InvoiceApproved (n), v: Approval queue]
  ✍️ Reject Invoice:      [t: Approver /Approval queue, c: RejectInvoice {invoiceId, reason: string}, x: ReasonRequired, x: NotSubmitted, e: AP / InvoiceRejected, v: Invoice]
  ✍️ Post Invoice:        [t: ⚙️ System / Post approved, c: PostInvoice {invoiceId}, e: AP / InvoicePosted, v: Invoice]
  ✍️ Void Invoice:        [t: Clerk /Invoice, c: VoidInvoice {invoiceId, reason}, x: InvoiceNotPosted, x: AlreadyVoided, e: AP / InvoiceVoided, v: Invoice]   # compensates PostInvoice
  ✍️ Record Payment:      [t: Clerk /Invoice, c: RecordPayment {invoiceId, amount, paidAt}, e: AP / PaymentRecorded, v: Invoice]
  ✍️ Reverse Payment:     [t: Clerk /Invoice, c: ReversePayment {paymentId, reason}, e: AP / PaymentReversed, v: Invoice]                                   # compensates RecordPayment
  ✍️ Maintain Vendor:     [t: Admin /Vendor, c: UpdateVendor {vendorId, name, iban}, x: StaleVersion, e: MD / VendorUpdated, v: Vendor]
  👀 Audit Trail:         [v: Audit trail {invoiceId, entries[]}]
  # State / Invoice carries status: InvoiceStatus (draft|submitted|approved|posted|voided)
```

Personas: Clerk, Approver, Auditor (reads only), Admin. Surfaces used below: `InvoiceList`, `InvoiceDetail` (Clerk), `InvoiceDetailAuditor`, `ApprovalQueue` (Approver), `VendorDetail` (Admin).

## 1. Verdict table

T2 counts are genuine non-default annotations: **LOB / BB / MEM / TTT**. "—" = not reached.

| # | Candidate | T1 derivable? | T2 non-defaults | T3 geometry | T4 slope | T5 neutral | Placement | Admit? |
|---|---|---|---|---|---|---|---|---|
| S1 | `c: X` → `confirm: true\|false` (Cooper excise, Nielsen H5, Fiori IsActionCritical, Carbon danger, Fluent alert) | NO: irreversibility and consequence are not in the EM (DER row 8, "Low-Medium" heuristic); Fiori could not derive it either and made it an annotation | **3 / 0 / 0 / 0** (VoidInvoice, ReversePayment, BulkApprove; the games confirm nothing: `ActionForm.razor:15-18`, RES R20) | pass: names *whether*, not *where*; the modal is the transformer's rendering | pass: a default question exists (Fiori "Confirm This Action?"); custom copy is `labels`, not a structural key | pass: hx-confirm / dialog / "ask first" | **xmlang**, with an emlang default hook (§5) | **Yes** |
| S2 | `v: V` → `heading: field` — a view field is the surface's heading (RES R37; Fiori UI.HeaderInfo) | NO: which field titles the surface is not in the EM; the spec default is the surface label (XSPEC L109) | **3 / 1(+2) / 2(+3) / 0** — parenthesised counts need the EM completeness fix in §5 | borderline, argued pass: the accessible *name* of the surface, a role like `self:`, not a region (contrast `slot: header`, removed v0.4.0) | pass once the subtitle is defined as the first secondary field (no `subheading:`) | pass | xmlang (salience/naming, not idiom) | **Yes, with the §2 objection** |
| S3 | `c: X` → `undo: Y` compensating pairing (Cooper undo, Tidwell Safe Exploration, ixd #44) | NO from the EM today; YES the moment emlang records compensation | 2 / 0 / 0 / 0 (Void↔Post, Reverse↔Record) | pass | pass | pass | **emlang** (`compensates:`); xmlang consumes | Not xmlang |
| S4 | `posture: read-only\|editable`; Fiori display-vs-edit mode; Cooper postures | YES: editable iff the surface composes a `c:` writing the view (DER row 17) | 0 / 0 / 0 / 0 (every instance restates composition) | Cooper sovereign/transient is size and dwell (https://en.wikipedia.org/wiki/Application_posture) | — | — | Defaults; Non-goal | **No** |
| S5 | inline vs modal; page edit vs inline edit (ixd #33; Figma "Open overlay") | NO | LOB has instances | **fails**: placement in disguise; the spec made "surface" modality-neutral on purpose (L100) | — | — | concrete | **No** |
| S6 | `feedback: optimistic\|confirmed` | weak heuristic (zero `x:` → optimistic safe, DER row 22) | 0 / 0 / 0 / 0 (RES R30 all confirmed; Salesforce: a toast confirms *every* intentional action = a default) | pass | pass | pass | Non-goal already (timing/transport, XSPEC L276) | **No** |
| S7 | `steps:` wizard chunking of one command's fields | NO for grouping (DER row 13) | LOB yes (DraftInvoice header/lines) | contested: a step is a page | fails (step labels, per-step validation) | pass | concrete; multi-step *with state* = slices + drafts (emlang); agrees with p2-senseA C3 | **No** |
| S8 | draft / save-and-continue | YES: a `Draft*` event exists or not. Fiori: "A draft is an interim version of a business entity", created "in the background" (https://raw.githubusercontent.com/SAP-docs/sapui5/main/docs/06_SAP_Fiori_Elements/draft-handling-ed9aa41.md) | 0 (restates the EM) | — | — | — | **emlang** (events); autosave cadence is timing (Non-goal) | **No** |
| S9 | bulk selection action | YES: a command whose prop is a list of the composed view's item ids is a bulk action over the selection (`BulkApprove {invoiceIds[]}`) | 0 | pass | — | — | Defaults row | **No** |
| S10 | approval/rejection with mandatory reason | YES: `reason: string` + `x: ReasonRequired` | 0 | — | — | — | emlang (already there) | **No** |
| S11 | optimistic-locking conflict display | *that* it can happen: YES (`x: StaleVersion`); *how* shown: concrete; copy: labels | copy only | — | — | — | labels (§4) + concrete | **No** |
| S12 | waiting-surface marker / pacing `system\|host\|self` (RES R9/R17/R18) | YES: trigger role of the next slice in the persona's journey relative to the viewer's role (⚙️ → wait on system; other human role → wait on that persona; own role → act, or dismiss if no command is composed). Confirms and sharpens p2-senseA E2/D5 | 0 after the emlang trigger fix | pass | pass | pass | emlang E2 + Defaults D5 (p2-senseA) | **No** (already placed) |
| S13 | error display regime (banner / inline / swallowed; Fiori popover / dialog / strip) | audience derivable (DER row 15); regime concrete; copy labels | regime 0/0/0/0 (RES R23; live swallows in-game errors, AE:161-166 — a defect, not a judgment) | regime is placement | — | — | concrete + labels; Defaults row | **No** |
| S14 | auto-submit on expiry (RES R58; PS:170-186) | NO today; derivable once the EM models expiry as a System slice | 0 / — / — / 1-of-1 (no counter-instance) | pass | fails (needs "which field is the clock") | pass | concrete; emlang deadline construct (DER §4a) | **No** |
| S15 | disclosure timing / name hidden behind image (RES R43) | partly YES by composition; the logo substitution is per *pack* (`loggor-*`, GS:317-320) = content-conditional = a `when:` | 0 | pass | fails | — | existing composition + concrete; Non-goal | **No** |
| S16 | audit-trail disclosure: a whole view item on-demand | NO — but the gap is only that tiers are per-field, not per-view-item | LOB 1 (Audit trail on InvoiceDetail) | pass | pass | pass | salience, not idiom; flagged to whoever owns tiers | out of scope |
| S17 | empty / loading / error states | `$empty` exists; loading is timing; error is S13 | — | — | — | — | existing + Non-goal | **No** |
| S18 | exception labels, `$confirm` copy, enum value labels, ICU arguments | copy is never derivable | LOB yes; games 3/3 for exceptions | pass | see §4 | pass | **labels extension**, not interaction | §4 |

### 1a. The eight residue slots (`RenderModel.cs:33-43`), classified

| Slot | Live use | Class | Where it goes |
|---|---|---|---|
| `Heading` | `lot.Description` (AS:118), `questionText` (GS:170), TTT results none (TS:163-168) | **DATA** | S2 `heading:` |
| `Sub` | "Väntar på att {host} startar…" (AS:89), "{done} av {n} klara" (AS:146) | COPY, templated | §4 ICU arguments; the counts are an EM information-completeness gap |
| `Footer` | "Behöver minst 2 spelare · N ansluten(a)" (AS:76); "Väntar på att värden går vidare…" (AS:171) | COPY: a withheld command's rejection explained; waiting copy whose audience is derivable (S12) | §4 exception labels |
| `PollPath` | 1 s poll (RES R28) | TRANSPORT | Non-goal (XSPEC L276) |
| `PlayAgainHref` | terminal → catalog (AS:247) | NAVIGATION default | p2-senseA D6 |
| `ShareText` | marketing copy (AS:248) | COPY, static | a surface label reserved key if ever wanted; not interaction |
| `Steps` | "Så funkar det" (AS:251-258) | CONTENT, static | concrete ("neither data nor judgment about data", AS:249-250) |
| `Source` | per-card citation (GS:176) | **EMLANG GAP**: `Question card` has no `source` prop (EM-MEM:382-389), so it "rides beside the bag" (RM:40-42) | add the prop; then it is an on-demand field, existing vocabulary |

Not one slot is an idiom. That is the empirical answer to lead (a).

## 2. Admitted items

### S1 — `confirm` (command item)

Normative text (Surfaces › Command items):

- A command item MAY contain a `confirm` key
- `confirm` MUST be a boolean; if absent it defaults to `false`
- `confirm: true` states that the transformer MUST obtain the viewer's explicit assent before issuing the command; the form of assent (dialog, inline second step, typed re-entry) is transformer-defined and MUST NOT be expressed in the model
- The assent copy is the command's `$confirm` label (see Labels); absent, transformers MUST use the command's own label in a generic question
- Where the Event Model declares that another command compensates this one, transformers MUST NOT add confirmation on their own initiative (see Defaults)
- `confirm` records a judgment about consequence and friction, never about geometry; it is the whole of xmlang's confirmation vocabulary and future versions MUST NOT add a placement or style axis to it

```yaml
# ap.xm.yaml — proposed (LOB, own construction)
surfaces:
  InvoiceDetail:
    for: [Clerk]
    compose:
      - v: Invoice
        heading: invoiceNumber          # proposed (S2)
        fields:
          primary: [vendorName, amount, dueDate, status]
          secondary: [lines]
      - v: Audit trail                  # S16: whole-view on-demand is not expressible today
      - c: AmendDraft
      - c: SubmitForApproval
      - c: VoidInvoice
        prominence: overflow
        confirm: true                   # proposed — irreversible, high consequence
      - c: ReversePayment
        prominence: overflow
        confirm: true                   # proposed
      - c: DiscardDraft
        prominence: overflow            # no confirm: low consequence here (a judgment; Fiori would confirm every delete)
  ApprovalQueue:
    for: [Approver]
    compose:
      - v: Approval queue
        self: assigneeId
      - c: ApproveInvoice
      - c: BulkApprove
        confirm: true                   # proposed — not irreversible, but n-at-once (Fiori confirms multi-delete by default)
      - c: RejectInvoice
        prominence: secondary
  VendorDetail:
    for: [Admin]
    compose:
      - v: Vendor
        heading: name                   # proposed (S2)
      - c: UpdateVendor
```

Games (negative control): **no `confirm` appears in any of the three files.** BB `EndAuction` and MEM/TTT `EndGame` are irreversible but the live design confirms nothing (RES R20), and that is right for a party game: 0/0/0 annotations, and the key costs the small product nothing. Simple stays simple.

Lint:

| Rule | Severity | Meaning |
|---|---|---|
| `xm-confirm-not-boolean` | error | `confirm` is not `true`/`false` |
| `xm-confirm-compensable` | info | `confirm: true` on a command the Event Model marks as compensated by another; undo exists, friction may be excise (suppressible — Cooper; NN/g) |
| `xm-confirm-habituation` | warning | More than half of the commands composed on one surface carry `confirm: true` ("if you cry wolf too many times, people will stop paying attention", https://www.nngroup.com/articles/confirmation-dialog/) |

Defaults row: **`confirm` absent → no confirmation is requested. A command the Event Model declares as compensated by another SHOULD be offered without confirmation, and its compensating command SHOULD be offered wherever its effect view is composed.**

Strongest unanswered objection: `confirm` is a boolean with one working value, so on any product it is either sparse (fine) or a smell (the habituation lint). The real risk is not slope but *placement drift*: SAP put `IsActionCritical` beside the domain model, which suggests some teams will want "critical" to be a domain fact. I keep it in xmlang because the same `VoidInvoice` warrants confirmation on a clerk's surface and none on an auditor's read-only replay — it varies per surface, which a domain fact cannot.

### S2 — `heading` (view item)

Normative text (Surfaces › View items):

- A view item MAY contain a `heading` key naming one field on the referenced view whose *value* is the surface's on-screen heading and accessible name, in place of the surface's label
- `heading` MUST be a string naming a field on the referenced view; an unresolvable name MUST be reported as an error (`xm-heading-field-missing`)
- At most one view item per surface MAY carry `heading`; a second MUST be reported as an error (`xm-heading-conflict`)
- A field named in `heading` is rendered as the heading and not again in its tier; listing it in a tier as well SHOULD be reported as a warning (`xm-heading-in-tier`)
- `heading` names *which datum titles the surface*, a naming judgment like `self`; it is not a region, and this specification defines no subtitle key: where a transformer renders a subtitle it SHOULD use the first field of the `secondary` tier

```yaml
# blindbudet.xm.yaml — proposed
  Budgivning:
    compose:
      - v: Lot card
        heading: description            # proposed — live: AS:118
        fields: { primary: [unit], secondary: [lotIndex, totalLots], on-demand: [gameId] }
  RundresultatVärd / RundresultatSpelare:
      - v: Round scores
        heading: description            # proposed — REQUIRES EM: Round scores gains `description` (live reads it from Lot card, AS:170)

# mer-eller-mindre.xm.yaml — proposed
  Riktningsfråga, Skillnadsfråga:
      - v: Question card
        heading: questionText           # proposed — live: GS:257; the xm comment L223-224 already says so in prose
  Riktningsavslöjande, RundresultatVärd / RundresultatSpelare:
      - v: Direction reveal / Round scores
        heading: questionText           # proposed — REQUIRES EM: both views lack questionText (EM-MEM:989-990, 1027-1028)

# tank-till-tusen.xm.yaml — proposed: NO heading anywhere. Pussel titles with the `target`
# field's own label ("Nå så nära du kan", PS:16-18) and round results carry no heading
# (TS:163-168) — the latter is expressible today only as an empty label string.
```

Non-default count: LOB 3 (Invoice, Vendor; the Approval queue deliberately *not*), BB 1 today / 3 after the EM fix, MEM 2 / 5, TTT 0. Two games promote a field, one does not: genuine variance, not a restated default.

Strongest unanswered objection: this is `slot: header` for one field, and the v0.4.0 changelog says the only real placement judgments were "per-FIELD region splits… below the key's granularity" (XSPEC L311). My answer — a heading is a *name*, the aria name of the page, not a region, and the spec already treats the heading as a labels concern (L109, L215) — is an argument, not a measurement. If game #4 or the LOB model ever wants `heading` on two fields, or a `subheading:`, the key is on the slope and should be reverted by the same rule as `slot:`.

## 3. Rejected items: failing test and Non-goals text

- **S3 undo pairing** — fails placement, not a test: xmlang asserting "Y reverses X" duplicates behaviour (p1-mbui F2). *Non-goal: Undo pairing — which command compensates which is an Event Model fact; xmlang decides only friction (`confirm`) and prominence.*
- **S4 posture** — T1: editable ⇔ composes a command (DER row 17). *Non-goal: Posture — read-only follows from composing no command; sovereign/transient is a container property.*
- **S5 inline vs modal / inline edit** — T3. *Non-goal: Presentation modality (modal, drawer, inline, page) — placement.*
- **S6 optimistic vs confirmed** — already a Non-goal; add the design-system default: *a non-blocking confirmation of an intentional command is the default feedback; a blocking one is `confirm`.*
- **S7 wizard chunking** — T1 no, T3 contested, T4 fails; with drafts modelled (S8) each step is a slice. *Non-goal: Wizard steps — chunking one command's fields is concrete; a multi-step interaction with intermediate state is several slices.*
- **S8 drafts / S9 bulk / S10 mandatory reason** — T1 YES. Defaults rows: *a command whose prop is a list of the composed view's item ids is offered over the selection; a `Draft*`/`*Amended` event pair means edits persist without a submit.*
- **S11, S13 error regimes** — regime is placement; audience derivable (DER row 15). Defaults row: *a rejection raised by the viewer's own command MUST be shown to the viewer with the exception's label; a rejection of a System-triggered command reaches no viewer.* (The live interpreter violates the first clause: AE:161-166 ignores the Result.)
- **S12 pacing** — derivable as in the table; placed by p2-senseA (E2, D5). Sense C adds: the same derivation fixes the *audience* of waiting copy ("Väntar på att värden går vidare…", AS:171, is exactly "next slice's trigger role = host ≠ viewer").
- **S14 auto-submit on expiry** — T4 fails; T2 has no counter-instance. *Non-goal: Client behaviour at a deadline — timing; model the expiry as a System slice and it becomes derivable.*
- **S15 disclosure by content** — a per-pack condition is a `when:`. Already a Non-goal.

## 4. The labels gap

What `labels` cannot express today, verified against `XmLinter.LabelableFields` (`XmLinter.cs:145-155`: commands, views, surfaces, journeys, personas only) and the reserved-key list (XSPEC L211-213: `$self`, `$empty`):

| Gap | Live evidence | Proposal | Passes? |
|---|---|---|---|
| **Exception copy** | `Describe` switches: AE:199-210 (9 cases), GE:290-308 (16), TE:219-231 (10); also the withheld-command footer AS:76 | A label-map key MAY be the exact name of an `x:` element, string form only. `EmSpec` needs `FindException` (today only `FindView`/`FindCommand`, `EmParser.cs:24-31`). Exception names are bare and document-global (`GameNotFound` appears in 5 MEM slices), so one label per name matches how the live code already works | T1 yes (copy), T2 3/3 games + LOB, T3-5 yes. **Zero new reserved keys**, one new labelable element kind |
| **Confirmation copy** | none live (no confirm) | Reserved key `$confirm` at command level: the assent question for `confirm: true` | Only meaningful with S1; a labels key, not a structural one |
| **Enum value labels** | `direction: Direction (mer\|mindre)` (EM-MEM:431) rendered raw "mer"/"mindre" (DR:23); LOB `status (draft\|submitted\|…)` needs "Awaiting approval" | Reserved key `$values` at field level: a map from declared enum values to strings; a value not in the enum note → `xm-orphan-label` | T2: games 0 (raw values are already Swedish words), LOB yes. Admit on LOB evidence |
| **Templated copy** | "Väntar på att {host} startar…" (AS:89), "{done} av {n} klara" (AS:146), "Om {MER} är 100, var hamnar {MINDRE}?" (XM-MEM L191-192 calls it residue) | Label strings MAY be ICU MessageFormat messages; arguments MUST be scalar fields of the labelled element (`xm-label-arg-missing`, error). ICU is an external standard, like DTCG and BCP 47, so it is not an inner platform | Partial: `{host}` and `{questionText}` resolve; `{done}`/`{n}` are *counts of lists* that `Bid progress` does not carry — an EM information-completeness gap, not a labels gap. Plural (`ansluten(a)`) is exactly ICU's job. Slope risk: ICU `select` is a conditional; cap by lint to argument + plural if the team wants |
| **Per-item status tags** | "bjuder…/gissar…/räknar…", "klar", "du · klar" (AS:139-141, GS:128-131, TS:135-138) | Considered `$each` (copy per list item). **Not proposed**: the tag encodes list *membership* (pending vs submitted), which the field label already names; the live tag is a rendering of that | Rejected: restates the field label |

Is any of this interaction? Only exception copy has an interaction-design pedigree (Nielsen H9; Fiori's message popover shows "the message of the current object" with text that "originates from the backend", https://raw.githubusercontent.com/SAP-docs/sapui5/main/docs/06_SAP_Fiori_Elements/using-messages-239b192.md). The rest is content. None of it is an Interaction Model.

```yaml
# ap.xm.yaml labels — proposed
labels:
  en:
    VoidInvoice:
      $self: Void invoice
      $confirm: "Void invoice {invoiceNumber}? Posted amounts are reversed and this cannot be undone."   # proposed: $confirm + ICU argument
    StaleVersion: Someone else changed this record. Reload to see their changes.   # proposed: exception label
    ReasonRequired: Give the requester a reason.
    InvoiceNotPosted: Only posted invoices can be voided.
    Invoice:
      status:
        $self: Status
        $values: { draft: Draft, submitted: Awaiting approval, approved: Approved, posted: Posted, voided: Voided }   # proposed
# blindbudet.xm.yaml — proposed additions
  sv:
    NotEnoughPlayers: Det behövs minst 2 spelare.      # proposed: exception label (was AE:204 + AS:76)
    AlreadyBid: Du har redan lagt ett bud på den här lotten.
```

## 5. Emlang-side findings

| Construct | Rationale | How xmlang consumes it |
|---|---|---|
| **`compensates: <Command>`** on a command (or `reverses: <Event>` on an event) | Which command undoes which is behaviour (LOB: Void↔Post, Reverse↔Record). No game has one (DER row 9); the LOB model has two. Cooper/NN/g make undo the preferred alternative to confirmation, so the fact drives a UX default | Defaults: a compensated command is offered without confirmation and its compensator is offered where its effect view is composed; lint `xm-confirm-compensable`. xmlang never names the pair |
| **No `irreversible:` marker** | Tempting as the derivation source for `confirm`, but "uncompensated" already says it, and a UI-flavoured boolean on the domain model is the MBUI dual-spec smell. Fiori's `IsActionCritical` shows vendors *do* put it there; I recommend against | `confirm` stays an xmlang judgment with default `false` |
| **Trigger set is true (p2-senseA E2)** | `t: 🧑‍🏫 host /Round results` beside `t: ⚙️ System / Next lot` (EM-BB:778, EM-MEM:1134, EM-TTT:855 vs AS:177-180) | Pacing and waiting-copy audience become derivable (S12); lint `xm-command-trigger-mismatch` (p2-senseA L1) |
| **Information completeness on result views** | `Round scores` lacks `description` (BB) / `questionText` (MEM); `Question card` lacks `source`; `Bid progress` lacks the counts the sub-heading needs. The method's own rule ("each field must be represented so that the blueprint has the source of and destination", DER §3) is violated by the shipped screens | Makes `heading:` referenceable on five more surfaces, turns `Source` into an on-demand field, and lets ICU arguments resolve |
| **Deadline as a modeled condition** (TTT) | 60 s + 3 s grace live in comments and a `DateTimeOffset` prop (EM-TTT:34-41, 317); a `✍️ Expire Round` System slice makes "what happens at T" a modeled event | S14 auto-submit stays concrete, but the *outcome* (non-submitters score 100) becomes a visible slice, not a comment |

## 6. Verdict on H-C

H-C ("a few idioms are data — destructive confirmation, undo via compensating command, posture — and most are concrete residue") is **half right in its list and right in its shape**. Destructive confirmation survives, but only under the LOB re-scoping: it is a per-surface judgment that five enterprise design systems record per action and that the Event Model cannot derive; on the three games it is correctly absent everywhere, which is the negative control passing, not the key failing. Undo does *not* survive as xmlang vocabulary: the pairing is a domain fact that belongs in emlang as `compensates:`, with xmlang consuming it as the default that removes friction. Posture falls to derivability (editable ⇔ composes a command) and to geometry (Cooper's postures are containers). The one datum H-C did not predict, `heading:`, is not an idiom but a naming judgment the interpreter needed on every game and two of three games make non-default. Everything the literature calls an idiom in the rendering sense — inline vs modal, optimistic feedback, wizard chunking, waiting surfaces, error regimes, expiry behaviour, input controls — is concrete or already a Non-goal, and the remaining cross-game variance is copy, a four-item `labels` extension. So sense C, as data, is two keys, one labels extension and one emlang construct; there is no Interaction Model here, only two judgments that were hiding in the concrete stratum because nobody had a place to write them down.
