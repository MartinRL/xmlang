# P3 / agent 8 — Red team: attack, then rule

Inputs: BRIEF.md (+ LOB amendment), XSPEC v0.5.0, p1-mbui (F1–F9), p2-senseA/B/C, p1-lob with the REAL testbed `lob-ap.em.yaml` (EM) and `lob-ap.xm.yaml` (XM), p1-derivability (DER), p1-residue (RES), p1-ixd. All three Phase-2 analysts built their own AP models; every LOB count below is re-run against the real files. Line numbers are to those files.

## 0. Lead rulings

1. **`then: <Surface>` survives, barely, and not for the reason sense A gave.** On the real testbed it does genuine work on **2 of 21** composed commands (not 4 of 9), and one of the two is a proxy for a judgment the key cannot express ("advance to the next draft"). The real EM author already encoded return-vs-advance in the slice's terminal `v:` (`ApproveInvoice` ends in `Approval queue`, EM:580; `DiscardDraft` ends in `Invoice list`, EM:418), which is exactly the F2 dual-specification the key invites. AMEND, with the strongest unresolved objection attached.
2. **`confirm: true|false` is the one clean admission.** 4 of 21 on the real testbed (XM:162, 246, 220, 67); 2 of the 4 are not derivable even with an emlang `compensates:` because the designer's reason is consequence, not irreversibility (`DeactivateSupplier` is reversible and still wanted). KEEP; kill the compensates-derived default and its lint, which nag on precisely the genuine cases.
3. **`heading: <field>` is killed as a key and absorbed into the labels extension.** On LOB it would be written identically on all 12 `Invoice details` compositions (dead weight signature), the designer's actual headings mix copy with data ("Invoice — awaiting approval", XM:524), and the wanted subtitle (`supplierName`) is not the first secondary field on any of the 12 (slope). Surface labels as ICU messages with field arguments express every heading in all four testbeds with zero new structural vocabulary and stay where the spec already puts headings (XSPEC "A surface's on-screen heading is its label").
4. **Framing: there is no Interaction Model stratum.** There is a derived one — a Defaults sub-table — plus two per-command judgments, a labels extension, and four emlang debts. That is exactly what survived MBUI (p1-mbui §3: a convention-default, one flat attribute, and the behaviour model itself), so the result is a confirmation of the article's thesis, not a new stratum.

## 1. Surviving candidates: attacks and verdicts

### 1.1 `then: <Surface>` (command item, sense A) — AMEND

**Re-run of test 2 on the real model.** Default D3 (sense A §2): destination = the surface, applicable to the viewer's persona, composing the terminal view of the command's slice in the new phase; else stay; rejection → issuing surface.

| # | Surface › command | Slice terminal `v:` (EM) | D3 default | Designer wants (XM comment / p1-lob) | Non-default? |
|---|---|---|---|---|---|
| 1 | SupplierDirectory › CreateSupplier | Supplier details (EM:104) | SupplierRecord (Admin) / SupplierRecordReadOnly (others) | new record | no — and note the key **cannot** be written here: `xm-then-persona-mismatch` forbids one name for three personas; only the default resolves per persona |
| 2–4 | SupplierRecord › Update / Deactivate / Reactivate | Supplier details | stay | stay | no |
| 5–6 | InvoiceWorklist, ClerkHome › DraftInvoice | Invoice details (EM:320), phase draft | InvoiceDraft | InvoiceDraft | no |
| 7–9 | ApproverHome › ApproveInvoices / ApproveInvoice / RejectInvoice | Approval queue (EM:683, 580, 640) | stay | stay (queue refreshes) | no |
| 10 | InvoiceDraft › EditDraft | Invoice details | stay | stay (W-9) | no |
| 11 | InvoiceDraft › SubmitInvoice | Invoice details (EM:459), phase submitted | InvoiceSubmittedClerk | "ADVANCE to the next draft" (XM:160, W-10) | **yes, as a proxy**: `then: ClerkHome` records "return to worklist"; "next draft" is an instance selection no surface name expresses |
| 12 | InvoiceDraft › DiscardDraft | Invoice list (EM:418) | InvoiceWorklist | back to list | no — the EM author put it in the slice |
| 13 | InvoiceSubmittedClerk › WithdrawSubmission | Invoice details, phase draft | InvoiceDraft | edit it | no |
| 14 | InvoiceRejectedClerk › ReopenInvoice | Invoice details, phase draft | InvoiceDraft | edit it | no |
| 15 | InvoiceApprovedClerk › SchedulePayment | Invoice details, phase scheduled | InvoiceScheduledClerk | see the waiting surface (W-19) | no |
| 16 | InvoicePaidClerk › ReversePayment | Invoice details, phase approved | InvoiceApprovedClerk | reschedule (journey BouncedPayment, XM:347-353) | no |
| 17 | InvoiceDecision › ApproveInvoice | **Approval queue** (EM:580) | ApproverHome | "ADVANCE to the next pending" (XM:232, W-11) | **no at surface level** — the default already returns to the queue; "next pending" is again instance selection |
| 18 | InvoiceDecision › RejectInvoice | Approval queue | ApproverHome | queue | no |
| 19 | InvoiceApprovedApprover › VoidInvoice | Invoice details (EM:1040), phase voided | InvoiceClosed | "return to the list" (W-11) → Approver's only list is ApproverHome | **yes** |
| 20 | InvoiceControllerOpen › HoldInvoice | Invoice details, phase onHold | InvoiceControllerHeld | held record | no |
| 21 | InvoiceControllerHeld › ReleaseHold | Invoice details, returnedToPhase | InvoiceControllerOpen | record | no |

**Count: 2 / 21** (rows 11, 19), against sense A's 4/9 and p1-lob's 3/24. p1-lob's third case (row 17) is not a `then:` case: the EM's terminal view already carries the return. Row 19 is arguably derivable ("the new phase's surface for this persona composes no command → nothing to do here → entry surface"), which would leave **1 / 21**. Test 2 passes on the letter (≥1), not on the spirit.

**F2 (duplicates the behaviour model) — the decisive attack.** The real modeller used the slice's terminal `v:` as the destination: Approve/Reject/Bulk end in `Approval queue`, Discard ends in `Invoice list`, everything else in `Invoice details`. In Event Modeling the terminal view of a state-change slice is what the actor sees next (DER row 1); `then:` is a second place to write the same fact. When they disagree — `then: ClerkHome` against a slice ending in `Invoice details` — the pair is inconsistent and nothing says which wins. Sense A's defence ("the terminal view names the data effect, `then` names where she goes") is not how the one shipped-style LOB model was written.

**Slope.** The lob-ap pressure is not `{onSuccess, onReject}` — every command carries `x: ConcurrentEdit` and "re-present the issuing surface" is right for all 21; W-16's "reload and re-apply" is concrete. The pressure is **instance**: both ★ navigation wishes (W-10, W-11) are "next item of the list I came from", which needs the row/selection binding (W-3/4) as a second key. p1-lob §2 already logged this slope for `next`; it applies to `then: <Surface>` too, because `then: InvoiceDraft` from SubmitInvoice would make `xm-then-needs-instance` infer the *wrong* instance (the just-submitted invoice, whose phase no longer matches `during: [draft]`).

**F4/F6.** A default that a designer must know to predict (per-persona resolution, phase-after-command) is a heuristic between spec and pixels. `xm-then-restates-default` mitigates but does not remove it.

**Verdict: AMEND.** Admit `then: <Surface>` only with: (i) normative text that `then` MUST name a surface applicable to every persona of the composing surface (already proposed) *and* that a `then` naming a surface that does not compose the slice's terminal view is a return, never an advance — the advance is the Event Model's; (ii) `then: next` and any instance selection as a Non-goal; (iii) the tripwire that if a second LOB model shows the terminal `v:` and `then:` systematically disagreeing, the fix is the Event Model and the key is withdrawn.

### 1.2 `confirm: true|false` (command item, sense C) — KEEP

**Test 2 on the real model:** DiscardDraft (XM:162), ReversePayment (XM:220), VoidInvoice (XM:246), DeactivateSupplier (XM:67): **4 / 21**. Games 0/0/0 (RES R20), the negative control passing.

**Derivability probe (compensates + terminal phase).** Suppose emlang had `compensates:`. Void → `voided` terminal, no outgoing command, plus `Ledger / LiabilityReversed` (EM:1034) → derivably irreversible ✓. Discard → `discarded` terminal (EM:402 comment) ✓. DeactivateSupplier → compensated by ReactivateSupplier (EM:235) → derived *no confirm* ✗ wanted. ReversePayment → returns to `approved` (EM:969-970), reversible by SchedulePayment → derived *no confirm* ✗ wanted. **2 / 4 genuine after the best derivation**, and they are the interesting two: the designer's reason is consequence to others (an inactive supplier blocks drafting, EM:332-340; a reversal moves money), not reversibility. `confirm` is therefore not `irreversible:` in disguise.

**Slope.** The lob-ap designer wanted "the strongest confirmation in the product" on Void (XM:246) and p1-lob proposes Carbon's tiers `routine | destructive | irreversible`. That is the slope signal: boolean → tier → type-to-confirm. Ruling: the boolean holds; degree of friction (type-to-confirm vs dialog) is a rendering of consequence the transformer chooses, and `prominence` already gives the designer overflow-plus-confirm as the strong form. Pre-register: if a second LOB model needs two grades of `confirm: true` on one surface, the tier is on the slope and the key is reverted by the `slot:` rule.

**Placement attack (Fiori puts `IsActionCritical` on the domain annotation).** Sense C's defence — it varies per surface — has **no instance on lob-ap**: all four confirmed commands are composed exactly once. The defence that stands is the reversed F2: a UX-friction boolean on the domain model makes emlang assert UI. Keep in xmlang.

**Inner platform (`format:` argument).** `hx-confirm` and Fiori `IsActionCritical` are flat closed attributes in systems that shipped; a boolean is the opposite of an inner platform.

**Kill the compensates coupling.** Sense C's Defaults row ("a compensated command SHOULD be offered without confirmation") and `xm-confirm-compensable` are contradicted by the real designer on DeactivateSupplier; the lint would fire on a genuine judgment. Remove both; `$confirm` copy stays in labels; `xm-confirm-habituation` (warning) stays.

### 1.3 `heading: <field>` (view item, sense C) — KILL as a key, AMEND into labels

**Test 2 on the real model.** The designer would write `heading: invoiceNumber` on all 12 `Invoice details` compositions and `heading: name` on both `Supplier details` compositions: 14 annotations, identical per view. That is the geometry experiment's dead-weight signature — every annotation says the same thing a per-view default could say — and "which field titles this entity" is per-*view*, which is the emlang placement Fiori chose (`UI.HeaderInfo` beside the entity).

**Geometry.** A field promoted out of its tier into the title is `slot: header` for one field; the v0.4.0 changelog names per-field header promotion as *the* placement judgment below `slot:`'s granularity (XSPEC changelog, "secondary scalars as header chrome"). Sense C's answer (it is the accessible name, like `self:`) is fair but is an argument, not a measurement.

**Slope, measured.** The designer's actual headings are copy *plus* state: "Invoice — awaiting approval", "Invoice — rejected" (XM:524-534). A data heading loses the phase unless a second line exists; the wanted second line is `supplierName`, which is *not* the first secondary field on any of the 12 compositions (XM:155, 171, 183 …). `subheading:` is demanded on the first LOB surface.

**What replaces it.** Sense C §4 already proposes ICU MessageFormat labels with field arguments. Extend it to surface labels: `InvoiceDecision: "Approve {invoiceNumber} from {supplierName}"`, `InvoiceDraft: "Invoice {invoiceNumber} — draft"`, BB `Budgivning: "{description}"`, MEM `Riktningsfråga: "{questionText}"`, TTT: no argument. Arguments resolve against scalar fields of the surface's composed views; an ambiguous or missing name is `xm-label-arg-missing` (error). This records every heading judgment in all four testbeds, is written only where wanted, adds no structural key, keeps headings where the spec puts them, and inherits ICU's standing as an external standard (like DTCG, BCP 47). Cap ICU at arguments + plural by lint; `select` is a conditional (F1).

### 1.4 Labels extension (exception names; `$confirm`; `$values`; ICU) — KEEP

Real-model evidence: 25 distinct `x:` names in the EM, all hand-copy today (W-12, XM:544-545); `$values` for `InvoicePhase` (9 values) and `SupplierStatus` (2); ICU as above. Games: exception copy 3/3 (RES R24). Nothing here is interaction; it is the largest practical gain of the whole inquiry and it is copy.

### 1.5 Defaults-table rows — KEEP, trimmed

- **D1 entry, D6 menu** (document order): hold on lob-ap after reordering (§2a).
- **D3 destination**: holds as the rule `then:` overrides (§1.1).
- **Fine selector "terminal view of the slice whose event fired last for her"** (sense B B2): on lob-ap every `during`-bound cell has exactly one record surface per persona (12 compositions, no two in the same cell for the same persona), plus the `during`-less surfaces (ClerkHome, InvoiceHistory …) which no slice's terminal view ever selects. The per-viewer event cursor is never exercised: the row degenerates to "one `during`-bound surface per cell per persona; `xm-cell-ambiguous` for the rest", which sense B itself conceded. AMEND: state the LOB form as the row; the per-viewer cursor becomes the tie-break clause for synchronous multi-actor cells (games; parallel approvals if a model has them — lob-ap does not).
- **Enablement "disabled with the guard's label"** (B4): consistent with the 5-system rule (p1-lob §3), and lob-ap shows the stronger LOB pattern — per-viewer enablement projected into the read model (`pending` vs `blocked`, EM:714-715). KEEP. Add the W-8 row: a composed command is rendered only to personas whose `role` matches one of the command's trigger roles (SupplierDirectory › CreateSupplier, XM:54). Caveat: on the game fixtures this row hides Next/End from the host until E2 lands; the row and E2 ship together.
- **Required-prop default** (from §2d): a command prop is required iff some rejection scenario rejects its emptiness (`reason` + `x: ReasonRequired`, EM:628-633; `comment` has none → optional). Zero vocabulary.

### 1.6 Lints — KEEP with severity changes

- `xm-command-phase-mismatch`: on the real model it fires on `VoidInvoice` in `scheduled` (XM:238; only `approved` has a success GWT, EM:1045) and on `HoldInvoice` in `submitted` (XM:250; success GWT only `approved`, EM:763) — both are scenario-coverage gaps, not composition errors — and it is blind to the supplier decider because its lifecycle is `status`, not `phase` (EM:1267). Severity **info** until emlang's coverage lint (sense B §5.1) exists.
- `xm-command-trigger-mismatch`: 0 fires on lob-ap, 6 on the games. KEEP.
- `xm-then-*`: follow §1.1; `xm-then-needs-instance` is a heuristic over prop names and must stay a warning.

### 1.7 emlang-side findings — KEEP, one demotion

Verified on the real model: all 18 human triggers name an existing view (EM:84 … 1018 → Supplier list, Supplier details, Invoice list, Invoice details, Approval queue), so E1 (trigger origin resolves) is 18/18 here and 1/12 on the games. `compensates:` goes to emlang on domain merits only — its UX payload is demoted (§1.2). View filter parameters (W-20) and the phase namespace (W-1) stand (§4).

## 2. The rejections: attacks and verdicts

**(a) Entry surface per persona — KEEP the rejection, correct the count.** p1-lob's 2/4 measured DER row 12 (first slice's origin view), not sense A's D1 (first `during`-less surface in document order applicable to the persona). Against D1 as the file is written: Clerk → SupplierDirectory ✗, Approver → ApproverHome ✓, Controller → SupplierDirectory ✗, Admin → SupplierDirectory ✓ (XM:46, 92, 103, 119). Reordering to ClerkHome, ApproverHome, ControllerHome, SupplierDirectory, … gives 4/4, because Admin is in none of the Homes' `for:`. Document order can express it; test 1 fails. Honest cost: the judgment is encoded positionally and a reader must know the rule; and a designer's file *as written* was wrong, so the default is only as good as the reorder. That is the spec's own "list order = importance" doctrine, accepted.

**(b) Wizard / grouping — KEEP the rejection.** One instance on lob-ap (`DraftInvoice`, ten props in header/lines/attachments, XM:90, W-2). The EM prop order already carries the sequence (EM:297-304). Groups need names, and labels key elements, so a group is a new labelable element kind — slope, on the first instance. The 6-system recurrence (p1-lob §3) is mostly *container* choice (modal / tearsheet / page, step thresholds), which is geometry or a design-system default. Multi-step *with state* is draft + EditDraft, already slices.

**(c) Status criticality per enum value — AMEND the rejection: it is a tier, not `$values`, and it is deferred, not dead.** `$values` is copy ("On hold"); criticality is a closed semantic classification (`negative | critical | positive | neutral | information`) that tokens then bind — the same shape as salience tiers. Not derivable (onHold critical vs neutral is judgment). Test 2: exactly one instance across four testbeds (`InvoicePhase`; `SupplierStatus` trivially), games 0. Recurs in 4 systems as a per-value app judgment. It passes all five at n=1, but a token-naming convention already does the work today (XM:560-587, `color.status.<value>`). Ruling: record as the strongest deferred candidate with a re-open condition (a second LOB model wanting a semantic state that the token path cannot carry), not as a Non-goal.

**(d) Mandatory reason on reject — KEEP the rejection (emlang).** `reason: string # MANDATORY` + `x: ReasonRequired` (EM:628, 633, 746, 750, 978, 1023, 1027). The UX residue "ask before commit" is derivable from typed input (W-7) and required-ness from the rejection scenario (§1.5). Correct.

**(e) `stay | return | next` vs `then: <Surface>` — `then: <Surface>` wins; `next` KILL.** `return` is ambiguous in LOB (to the queue? the list? the dashboard?) — a surface name is not. `next` presupposes the launching list (slope, p1-lob §2) and neither shape expresses the actual W-10/W-11 wish. Fiori's own data supports this split: `stayOnCurrentPageAfterSave` is stay-vs-return, while "Create Next" is a separate *action* (p1-lob §3, [8]). "Advance to the next pending" is a command-or-view concern (a worklist whose first row is the next item), i.e. Event Model or concrete idiom — never a destination value.

## 3. Corrected test-2 table (real lob-ap; kept items only)

| Item | Population on lob-ap | Genuine non-default | Of which not derivable by any proposed default | Games (control) | Claimed in Phase 2 |
|---|---|---|---|---|---|
| `then: <Surface>` | 21 composed commands | 2 (Submit → ClerkHome as proxy; Void → ApproverHome) | 1–2 (Void is arguably "no-command surface → entry") | 0/0/0 | 4/9 (A), 3/24 (lob) |
| `confirm:` | 21 composed commands | 4 | 2 even with `compensates:` | 0/0/0 | 3 (C), 4 (lob) |
| Labels: exception copy | 25 `x:` names | 25 (all copy) | n/a | 9/16/10 | agrees |
| Labels: `$values` | 2 enums | 2 (9 + 2 values) | n/a | 0 | agrees |
| Labels: ICU surface headings (replaces `heading:`) | 24 surfaces | 14 record surfaces want a data argument | n/a | 1/2/0 | `heading:` 3 (C) |
| `during` phase namespace | 2 deciders | 1 forced rename (EM:1267) | n/a | 0 | new (lob W-1) |
| Criticality tier (deferred) | 2 enums | 1 | 1 | 0 | rejected (A, C) |

## 4. What must go to emlang

- **Trigger origin as a resolvable reference** (E1): `t: role /View` parsed as a view reference; 18/18 resolve on lob-ap, 1/12 on the games. Enables D3 "stay" and the trigger-mismatch lint.
- **True trigger sets** (E2): add `t: host /Round results` beside `⚙️ System` on Next/End (EM-BB:778, 815 and sisters); required before the W-8 role-render default can ship.
- **View parameters** (W-20): mark `phaseFilter`, `query`, `asOf` … as inputs to a projection, not projected data; today every tier annotation on them is a category error (XM:87-88, 126).
- **Lifecycle prop marking / phase namespace** (W-1): a multi-decider model needs `during` to resolve against a *named* State view's phase; the xmlang half is `during: { "State / Invoice": [draft] }`-style resolution, the emlang half is not forcing the second decider to avoid the word `phase` (EM:22-25, 1267).
- **`compensates:`** on domain merits (Void↔Post-style pairs; Reactivate↔Deactivate; Reopen↔Reject); xmlang must not derive `confirm` from it.
- **Scenario coverage lint** (`em-phase-transition-uncovered`): every command with a success GWT in every phase it may succeed in; without it `xm-command-phase-mismatch` is noise (§1.6).
- **Information completeness on result views** (games): `Round scores` lacks `description`/`questionText`; needed for ICU heading arguments to resolve.

## 5. Ruling on the framing

**The five tests.** Test 2 is the weak link: it is author-dependent (three analysts on three self-made AP models got 4, 3 and 3 for keys that score 2 and 4 on the real file), and the real file is itself a constructed testbed by the same team, never rendered by any transformer. The geometry experiment had a live product whose design *was* the default; this inquiry has no equivalent proof for LOB. Test 1 returns PARTIAL on nearly every row (DER §2) because derivability rests on emlang *conventions* the emlang spec does not state — slice order as timeline, trigger-origin text, State-lane `phase`, actor-identity props. So most "derivable, therefore a default" verdicts are "derivable once emlang adopts E1/E2/W-1/W-20". Tests 3–5 did their job: they killed inline/modal, posture, wizard containers, `stage:`, `steps:`, and `heading:`.

**Is an Interaction Model a stratum?** No. The evidence supports: two command-item keys (`then`, `confirm`), a labels extension (copy, including headings), a Defaults sub-table ("Navigation and enablement defaults", ~8 rows), three lints, one spec-resolution fix (`during` namespace), one deferred tier (criticality), and four emlang debts. Nothing in that list is an authored model of interaction; the sub-table is a *derived* one, and its content mirrors exactly what survived MBUI: a convention-default (Rails `redirect_to`), one flat closed attribute (`hx-confirm`), and the behaviour model as the state × event table (statecharts = the Event Model). Garrett's structure plane (p1-ixd §2) turns out to be the Event Model plus edges the Event Model already implies.

**Named section in the spec?** Not "Interaction Model". Recommend: (i) two bullets under "Command items"; (ii) a Defaults sub-table; (iii) the labels extension; (iv) a Non-goals block recording the corpse pile so it is not re-litigated: `when:` (now: redundant, not merely dangerous), wizard steps, `next`/instance destinations, posture, presentation modality, undo pairing, per-command hide/disable, `heading:`. The article's honest sentence is: *the interaction stratum exists, and it is derived; what a designer adds to it is a friction bit and a return-to-worklist name.*

## 6. The three strongest unresolved objections the article must state

1. **Dual specification of destination.** The one LOB modeller we have wrote navigation into the slice's terminal `v:`; `then:` is a second home for the same fact with no rule for disagreement. This is F2 in miniature, on xmlang's first edge. The mitigation (return-only semantics, restates-default lint, withdrawal tripwire) is a rule in prose, not in the syntax — the exact position Cameleon's transition rules and Figma's `Navigate to` were in before they grew conditions.
2. **One constructed testbed, never rendered.** Every LOB count rests on `lob-ap.*`, written by this team for this inquiry. The counts moved by a factor of two between the analysts' models and the real file; they may move again on a shipped product. No transformer has produced the lob-ap default experience, so there is no "live design equals default" proof of the kind that settled the geometry experiment. The honest status of `then:` and `confirm:` is *provisionally admitted pending a shipped LOB transformer run*, with `xm-then-restates-default` and `xm-confirm-habituation` as the dead-weight instruments.
3. **Derivability is borrowed from conventions emlang does not own.** The Defaults rows — the largest output — are computable only under the reference parser's conventions (State lane, trigger text, slice order, identity props). If emlang does not adopt E1/E2/W-1/W-20, "derivable" means "derivable by one implementation", which is F8 (standard without implementers) relocated one layer down; and the lints that keep the keys honest (`xm-command-phase-mismatch`) are noise until emlang's coverage lint exists.

A fourth, smaller: `confirm` on lob-ap is per-command in practice (each confirmed command is composed once), so the "it varies per surface, therefore xmlang" argument has no instance; the placement rests on the reversed-F2 principle alone.
