# P2 / agent 5 — Sense A (navigation topology): admission verdicts

**Revised for the 2026-09-07 amendment** (LOB is the target; games are a negative control). `p1-lob.md` was not present when this was written, so §0 constructs a minimal accounts-payable Event Model; every LOB claim below is against **my own construction** and must be re-checked when p1-lob lands.

Inputs: BRIEF.md (+amendment), XSPEC v0.5.0 draft, p1-mbui, p1-ixd, p1-derivability (DER), p1-residue (RES). Game claims were re-verified in the fixtures (EM-*), the xm files (XM-*) and the interpreter (`AS/GS/TS` = `Presentation\{Auction,Game,Tank}Surfaces.cs`, `AE/GE/TE` = `*Endpoints.cs`, under `C:\code\GitHub\kvissig.se\src\MerEllerMindre.Web\`). Where a Phase 1 line number was wrong the verified one is cited.

## Lead finding

**Sense A admits one key: a post-command destination `then:` on a composed command item, with a derived default.** On the games it is dead weight (0/0/0), which is the negative control behaving: a single-stream kiosk has exactly one surface per viewer state, so the fine selector (Defaults row D4) already decides where every command lands. On the AP model it does genuine work three to four times (submit → back to the inbox, approve/reject → back to the queue, void → back to the inbox), because in LOB many surfaces are valid for one state and the Event Model's terminal view names the *data effect*, not where the clerk goes. Everything else in sense A is either a Defaults-table row (entry and menu by document order; stay-on-edit; create→detail; id-lineage links; fine selection; interstitials) or belongs to emlang (the trigger origin as a resolvable reference; the true trigger set for Next/End). Wizard grouping is rejected: in LOB a long form that must survive partial completion is a draft, i.e. slices, and a form that need not is concrete.

## 0. The LOB testbed used here (own construction)

One stream `Invoice`, `State / Invoice` with `phase: InvoicePhase (draft|submitted|approved|rejected|posted|voided)`. Personas: **Clerk** (role AP clerk), **Approver** (cost-centre manager), **Auditor** (read-only), **Admin** (vendor master data). Views (data nouns): `AP dashboard`, `Invoice inbox` (cross-invoice list), `Approval queue`, `Invoice` (header + lines), `Audit trail`, `Vendor list`, `Vendor`. State-change slices, each `t: role / origin` → `c:` → `e:` → terminal `v:` in the fixtures' convention:

| # | Slice | Trigger origin | Command | Terminal view |
|---|---|---|---|---|
| 1 | Capture invoice | Clerk / Invoice inbox | `CaptureInvoice` | `Invoice` |
| 2 | Edit header | Clerk / Invoice | `EditInvoiceHeader` | `Invoice` |
| 3 | Add line | Clerk / Invoice | `AddInvoiceLine` | `Invoice` |
| 4 | Submit for approval | Clerk / Invoice | `SubmitForApproval` (`x: NoLines`, `x: NotDraft`) | `Approval queue` |
| 5 | Approve | Approver / Invoice | `ApproveInvoice` (`x: AlreadyDecided`) | `Invoice` |
| 6 | Reject | Approver / Invoice | `RejectInvoice {reason}` | `Invoice inbox` |
| 7 | Revise | Clerk / Invoice | `ReviseInvoice` (rejected → draft) | `Invoice` |
| 8 | Post | Clerk / Invoice | `PostInvoice` | `Invoice` |
| 9 | Void | Clerk / Invoice | `VoidInvoice` (compensating) | `Invoice` |
| 10 | Approve many | Approver / Approval queue | `ApproveInvoices {invoiceIds[]}` | `Approval queue` |
| 11 | Create vendor | Admin / Vendor list | `CreateVendor` | `Vendor` |
| 12 | Flag overdue | ⚙️ System / Due dates | `FlagOverdue` | `Invoice inbox` |

xm surfaces (order = importance, per XSPEC): `Dashboard` (for Clerk, Approver), `Inbox` (Clerk; composes `Invoice inbox` + `CaptureInvoice`), `Queue` (Approver; `Approval queue` + `ApproveInvoices`), `InvoiceDraft` (Clerk; `during: [draft, rejected]`; `Invoice` + `EditInvoiceHeader`, `AddInvoiceLine`, `SubmitForApproval`), `InvoiceReview` (Approver; `during: [submitted]`; `Invoice` + `ApproveInvoice`, `RejectInvoice`), `InvoiceLocked` (Clerk; `during: [submitted, approved, posted]`; `Invoice` + `PostInvoice`, `VoidInvoice`), `InvoiceAudit` (Auditor; `Invoice` + `Audit trail`), `AuditSearch` (Auditor; `Invoice inbox`), `Vendors` (Admin; `Vendor list` + `CreateVendor`), `VendorDetail` (Admin; `Vendor`). The AP shapes the amendment asks for are present: many commands per view (Invoice ×7), many surfaces per phase (three `Invoice` surfaces in `submitted`), a compensating command, bulk, a read-only persona, list→detail→edit round trips.

## 1. Verdict table

Test 2 = genuine non-default annotations, **LOB / BB / MEM / TTT**. The LOB count is against the default rule named in the "T1" column; §3 shows the hunt.

| # | Candidate (source) | T1 derivable? | T2 LOB / BB / MEM / TTT | T3 geometry | T4 slope | T5 neutral | Placement | Admit? |
|---|---|---|---|---|---|---|---|---|
| C1 | `personas.X.entry: Surface` (ixd #23/#31, mbui §4A, RES R1) | YES against D1: first surface in document order applicable to the persona with no `during` | **0** / 0 / 0 / 0 (corner case only, §3) | pass | soft slope toward `nav:` | pass | Defaults D1 | **No** |
| C2 | composed `c:` → `then: <Surface>` (ixd #55, mbui §4A, RES R4) | **NO** when the persona's surface for the slice's terminal view ≠ where she should go | **4** / 0 / 0 / 0 | pass (names a surface) | pass with ceiling (one name, never a condition) | pass | **xmlang vocabulary** | **Yes** |
| C3 | `steps:` field partition on a command (ixd #35) | NO | 1 / 0 / 0 / 0 (`CaptureInvoice`) | contested (a step is a page) | **fails** (step labels, review step, per-step validation) | pass | emlang (drafts) or concrete | **No** |
| C4 | `surfaces.X.related: [Y]` (ixd #17) | YES against D9: id-lineage links | 0 / 0 / 0 / 0 | pass | fails (edge list) | pass | Defaults D9 | **No** |
| C5 | root `navigation: hub \| sequence \| flat` (ixd #3/#29/#50) | YES (graph shape, DER row 21) | 0 (AP is all three at once) / 0 / 0 / 0 | pass | pass | weak | Non-goal | **No** |
| C6 | per-persona menu `personas.X.nav: [..]` / `reach: global` (ixd #15) | YES against D6: surfaces without `during`, for the persona, in document order | 0 / 0 / 0 / 0 (see C1 corner) | borderline (chrome) | pass | pass | Defaults D6 | **No** |
| C7 | `journeys.X.walk: true` (ixd #19/#30) | n/a | 0 / 0 / 0 / 0 | **fails** (stepper/breadcrumb is chrome) | pass | pass | concrete | **No** |
| C8 | command-less interstitial `surfaces.X.next: Y` (RES R9, DER §4b) | PARTIAL→YES against D5 (gated-command rule) | 0 (LOB confirmations are toasts or two-exit pages) / 0 / **1** / 0 | pass | fails at two exits | pass | Defaults D5 + tripwire | **No (closest call in games)** |
| C9 | pacing `system \| host \| self` (RES R9/R17/R18) | NO: the EM asserts the wrong trigger | n/a / **3/3/3** | pass | pass | pass | **emlang** E2 + lint L1 | Not xmlang |
| C10 | back / escape hatch / play again (ixd #32, RES R5/R11) | YES: terminal → entry; back is the platform's | 0 / 0 / 0 / 0 | pass | pass | pass | Defaults D6 | **No** |
| C11 | stream-boundary full navigation (RES R2/R3) | YES: subsumed by `then` default (create → terminal view) | 0 / 0 / 0 / 0 | transport | — | — | Defaults D3 | **No** |
| C12 | master-detail / drilldown (ixd #36/#37) | relation YES (D9); arrangement is geometry | 0 / 0 / 0 / 0 | **fails** | — | — | Non-goal | **No** |
| C13 | trigger origin resolvable (DER §4a) | the derivability enabler | 12/12 LOB triggers name an origin; 3/3/3 game mismatches | pass | pass | pass | **emlang** E1 | Not xmlang |
| C14 | unidentified viewer (RES R8) | YES as default | 0 / 0 / 0 / 0 | pass | pass | pass | Defaults D7 | **No** |
| C15 | cross-model site IA (RES R12) | outside the pair | — | — | — | — | Non-goal | **No** |
| C16 | fine selection in a cell (RES R6, DER row 5) | rule YES, evaluation via decider | 0 / 0 / 0 / 0 (`AlreadyDecided` fits the same rule) | pass | pass | pass | Defaults D4 | **No** |

## 2. Admitted

### C2 — `then:` on a composed command item

**Proposed normative text** (under "Command items and action prominence"):

- A command item MAY contain a `then` key naming the surface the viewer is presented after the command is accepted
- `then` MUST be a single surface name declared under `surfaces`; an unknown name MUST be reported as an error (`xm-dangling-ref`)
- The named surface MUST apply to every persona the composing surface applies to (`for:` compatibility); a mismatch MUST be reported as an error (`xm-then-persona-mismatch`)
- If `then` is absent, the destination is the default stated under Defaults; a `then` equal to that default SHOULD be reported as an info finding (`xm-then-restates-default`)
- `then` is never conditional: a command whose destination depends on the outcome is two commands in the Event Model, and a rejected command always re-presents the issuing surface (see Defaults). This is a considered rejection of per-command transition rules with conditions, the point at which every MBUI dialog model became a programming language (p1-mbui F1)

**Defaults-table row** (Navigation defaults, D3):

| Absent | Default |
|---|---|
| `then` on a command item | The surface, applicable to the viewer's persona, that composes the terminal view of the command's slice; if that is the issuing surface, or no such surface exists, the issuing surface (stay). A rejected command (an `x:` outcome) always re-presents the issuing surface |

**YAML, LOB first** (own construction; the `# proposed` lines are the non-defaults):

```yaml
surfaces:
  Inbox:
    for: [Clerk]
    compose:
      - v: Invoice inbox
      - c: CaptureInvoice              # default: → InvoiceDraft (terminal view Invoice)

  InvoiceDraft:
    for: [Clerk]
    during: [draft, rejected]
    compose:
      - v: Invoice
      - c: AddInvoiceLine              # default: stay (terminal view is on this surface)
      - c: EditInvoiceHeader           # default: stay
      - c: SubmitForApproval
        then: Inbox                    # proposed — default would be stay (no Clerk surface composes Approval queue)

  InvoiceReview:
    for: [Approver]
    during: [submitted]
    compose:
      - v: Invoice
      - c: ApproveInvoice
        then: Queue                    # proposed — default is stay (Invoice, now approved)
      - c: RejectInvoice
        then: Queue                    # proposed — default is stay (no Approver surface composes Invoice inbox)
        prominence: secondary

  InvoiceLocked:
    for: [Clerk]
    during: [submitted, approved, posted]
    compose:
      - v: Invoice
      - c: PostInvoice                 # default: stay (show posting result)
      - c: VoidInvoice
        then: Inbox                    # proposed — the object is dead; default is stay
        prominence: overflow

  Queue:
    for: [Approver]
    compose:
      - v: Approval queue
      - c: ApproveInvoices             # bulk; default: stay — correct
```

Four genuine non-defaults out of nine composed commands; the other five restate the default, which is what a default is for. The "return to worklist after decide" convention is the Fiori/Lightning worklist pattern as I know it; I could not fetch the guideline text — `[citation needed]`.

**Games** (negative control): every composed command's default equals live behaviour — `PlaceBid` → `Bid progress` → Väntan (EM-BB:344, 361; `AE:159-166`), `StartAuction` → `Lot card` → Budgivning, `AskNextLot` → Budgivning, `EndAuction` → Slutställning; MEM `AskNextQuestion` → `Question card`, composed on two surfaces, disambiguated by D4 (`GS:39-40`); TTT identical. **Zero `then` in all three files**, and `xm-then-restates-default` would fire on any attempt. The last-bidder case (lands on Rundresultat because the reveal gear ran synchronously, `AE:163-166`) is not a `then` matter: the destination is *seeded* by `then`/default and *refined* by D4 within the new state's cell. `then` is the LOB complement of D4: D4 decides when the state determines the surface; `then` decides when it does not.

**Lint rules**

| Rule | Severity | Meaning |
|---|---|---|
| `xm-then-persona-mismatch` | error | A `then` surface does not apply to every persona of the composing surface |
| `xm-then-restates-default` | info | A `then` names the surface the default rule would pick (the dead-weight metric, as `xm-slot-restates-default` was) |
| `xm-then-needs-instance` | warning | The `then` surface composes a view keyed by an identity that none of the command's props carries (e.g. `then: VendorDetail` from a command without `vendorId`); the transformer cannot know which instance to show |

**Strongest objection I could not answer.** `then` is xmlang's first edge. Cameleon's `(transition_task, next_PS)` rules and Figma's `Navigate to` both started unconditional and grew conditions; "one name, never a condition" is a rule in the spec, not in the syntax, and the pressure to add `then-on-reject:` (Rails' `render :new` path) will come on the first LOB product with a validation surface. Second: the LOB non-defaults are my construction; if p1-lob's authors compose an `Inbox` for the Approver too, `RejectInvoice`'s default becomes the inbox and the count drops to three. Third: `then` picks a surface but not an instance; the `xm-then-needs-instance` lint is a heuristic over prop names, exactly the kind of convention DER §2 warns is not yet load-bearing.

### E1, E2 — admitted to emlang, not xmlang

**E1: the trigger origin resolves.** `t: 🧑‍🎓 Player /Lot` (EM-BB:343) names a screen as text; the parser keeps the lane and drops the name (DER row 3). Resolving the origin to a `v:` (or declaring it a bare form) flips DER rows 1, 2, 3, 12 and 21 to YES with zero new keys, and in the AP model gives every command its issuing view, which is what D3's "stay" needs. On the fixtures a strict resolver flags `Lot`→`Lot card`, `Auction catalog`→`Pack catalog` (EM-BB:121 vs 100), `Question`→`Question card` (EM-MEM:426, 525 vs 344); only TTT's `Puzzle` matches (EM-TTT:351 vs 275).

**E2: who paces is a trigger fact.** All three EMs make Next/End `⚙️ System` (EM-BB:778, 815; EM-MEM:1134, 1173; EM-TTT:855, 894); all three UIs give the host a button (`AE:168-174`, `AS:177-180`; XM-*/finding 3). The honest fix is a second trigger, `t: 🧑‍🏫 host / Round results`, on those slices. LOB analogue: an approval that is normally automatic below a threshold but manual above it is two triggers on one slice, not an xm key. xmlang's side is one lint:

| Rule | Severity | Meaning |
|---|---|---|
| `xm-command-trigger-mismatch` (L1) | warning | A command composed on a `for:`-scoped surface has no Event Model trigger whose role matches any listed persona's `role` (fires 6× on the games today) |

Objection: the lint is the Experience Model socially steering the Event Model, which the one-way rule forbids technically. It only reports; a human edits emlang.

## 3. Rejected: the failing test, and the Non-goal line

**C1 entry per persona.** Hunt on LOB against D1 ("first surface in document order applicable to the persona with no `during`"): Clerk → Dashboard (listed first, for Clerk) ✓; Approver → Dashboard ✗ wanted Queue — but listing `Queue` before `Dashboard` fixes it without changing Clerk's entry, because Clerk is not in Queue's `for:`; Auditor → AuditSearch (first Auditor surface) ✓; Admin → Vendors ✓. The only case document order cannot express is two personas sharing two stream-less surfaces and wanting them in opposite order (a Supervisor who is a Clerk-plus); that is the corner, and it is a `nav:` list, not an `entry:`, which is the slope. Games: Katalog is first and `during`-less in all three files → 0/0/0. Fails test 1 against a default the spec's own doctrine (list order = importance) supports. Non-goal: *Entry surfaces — the most important `during`-less surface for the persona, i.e. document order; see Defaults D1.*

**C3 wizard steps.** LOB: `CaptureInvoice` (vendor, number, dates, currency, lines, cost centre, attachment) would plausibly be stepped: 1 non-default. Test 4 fails: steps need labels (labels key elements; a step is not one), a review step, and per-step validation, each a second construct. And placement is wrong: the amendment's own list pairs "draft/save-and-continue" with wizard — a form that must survive partial completion is a draft, which is slices 1-3 of §0, exactly as MEM's two-stage round is two slices gated by `x: DirectionNotRevealed` (EM-MEM:534). A form that need not survive is concrete. Games: 0/0/0 (max two typed props per command). Non-goal: *Wizard steps — intermediate state is slices in the Event Model; chunking one command's fields is concrete.*

**C4 related surfaces.** LOB hunt against D9 ("a view field typed as another view's identity links to the surface composing that view for the persona"): Invoice→Vendor (`vendorId`) derived; Invoice→Audit trail (`invoiceId`) derived; Inbox→InvoiceDraft/Locked (row id) derived; Dashboard→Inbox is a menu item (D6), not a contextual link. 0 non-defaults; and an edge list is the sitemap whose next question is "and back?". Games: the only cross-link is Slutställning → catalog (`AS:247`, `GS:241`, `TS:234`), which is D6. Non-goal: *Related-surface edges — identity lineage derives them; the rest is global navigation.*

**C5 topology enum.** The AP model is a hub (Dashboard), two worklists with master-detail, and a linear approval loop at once; one value per product cannot hold it, and DER row 21 computes the graph anyway. Games: one linear loop each. Non-goal: *Whole-product navigation archetypes — a graph property of the Event Model.*

**C6 menu / global reach.** D6 gives Clerk [Dashboard, Inbox], Approver [Queue, Dashboard], Auditor [AuditSearch], Admin [Vendors]; stream-bound surfaces (those with `during`) are reached only via links and `then`. 0 non-defaults except the C1 corner. Games: one menu item (Katalog) each, no in-stream global navigation (`GameShell.razor:11` and sisters). Objection: "no `during`" as the stream-less marker is a heuristic; a list surface that legitimately uses `during` (a phase-filtered worklist) would drop out of the menu. Noted, not solved.

**C7 journeys as walkthrough.** A stepper or breadcrumb is chrome (test 3). Journeys stay test material; note that XM-MEM's journey order carries Reveal-before-Difference, which document order gets wrong (EM-MEM `✍️ Submit Difference` L523 precedes `✍️ Reveal Direction` L771) — a reason to keep journeys, not to render them.

**C8 command-less interstitial.** Games: the genuine instance is MEM's mellansteg (`GS:41-42`, `GE:218-235`, `DirectionResultsScreen.razor:1-5, 31-34`), 0/1/0 — but its exit is derivable in three EM-only steps: `Direction reveal` is the terminal view of the slice producing `QuestionDirectionRevealed` (EM-MEM:771-795); `SubmitDifference` is gated on it (`x: DirectionNotRevealed`, EM-MEM:534); no gear waits on the reveal, so only the viewer can leave. The judgment that the reveal deserves its own surface is already recorded by composing it as one. LOB: the analogous "Submitted ✓" page is either a toast (concrete) or a page with two exits ("view invoice" / "back to inbox"), and two exits is the graph slope; `then: Inbox` covers the common case. Defaults row D5, tripwire in §5.

**C12, C15:** panel arrangement; outside the model pair. Non-goals: *Master-detail / drilldown — arrangement; the relation is D9.* *Site IA across Event Models — outside the pair.*

**C13, C14, C16:** emlang (E1) and defaults (D7, D4). The three game selectors are structurally identical (`AS:19-37`, `GS:23-43`, `TS:21-37`) and all three C1 clean-room probes rewrote the same one (RES §4): the convergence signature of a derivation. In LOB the same rule handles four-eyes approval (`x: AlreadyDecided` → the approver who has decided sees the locked variant).

## 4. Defaults-table candidates (zero vocabulary)

Proposed sub-table "Navigation defaults". Each row is what the interpreter hard-codes identically in all three games (RES §3), restated so it also holds on the AP model.

| Absent | Default |
|---|---|
| D1 entry | A persona's entry surface is the first surface in document order that applies to the persona and carries no `during`; a persona with no such surface enters through the bare form of its first triggered command (games: Katalog / bare join, RES R1, R62) |
| D2 stream boundary | Subsumed by D3: a command whose event opens or joins a stream lands on the surface composing its terminal view, inside the stream (RES R2-R3; `AE:90,128`, `GE:127,167`, `TE:77,116`) |
| D3 destination | As stated under C2 above: terminal-view surface for the persona, else stay; rejection → stay (RES R4, R31) |
| D4 selection within a cell | If the round's System-triggered event has fired → the surface composing that slice's terminal view; else if the viewer's issuing the cell's command would raise an `Already…` exception → the surface composing the Todo-fed progress view; else the input surface (`AS:31-37`, `GS:33-43`, `TS:31-37`). Evaluation calls the decider, which every transformer has |
| D5 interstitial | A `during`-bound surface composing no command, whose view is not a Todo-fed progress view, is left by the viewer's own act to the surface composing the command gated (`x:`) on the event that produced its view (MEM mellansteg) |
| D6 menu, back, play again | A persona's global navigation is the surfaces D1 ranges over, in document order; `during`-bound surfaces are reached only via D3/D9 links; back is the host platform's; a terminal-phase surface offers a return to the entry surface (RES R5, R11; `SurfaceRenderer.razor:64-66`) |
| D7 unidentified viewer | Sees only surfaces with no `for:`, commands withheld (RES R8; live deviates, `AE:159-166`; the C1 probes split three ways, hence a stated row) |
| D8 unknown stream | Not found, not a surface (RES R25) |
| D9 links | A view field typed as another view's identity is a link to the surface composing that view for the persona (ixd #65, Dymitruk's information completeness) |

Second lint, answering RES R61's hand-written reachability test:

| Rule | Severity | Meaning |
|---|---|---|
| `xm-cell-ambiguous` (L2) | info | Two or more surfaces in one `during`×`for` cell compose no command and no progress view, so D4/D5 select neither; suppressible |

## 5. Verdict on H-A

**H-A survives with its residue renamed.** Navigation is mostly derivable on both testbeds: entry and menu fall out of document order plus `for:` (D1, D6); stay-on-edit and create→detail fall out of the slice's terminal view (D3); contextual links fall out of identity lineage (D9); in-cell selection falls out of the decider's `Already…` guards and gear events (D4). Of the three residues the hypothesis named, entry surface fails against a default the spec's own ordering doctrine supports, and wizard grouping is either drafts (emlang) or concrete. The residue that is real is **return-vs-advance, and only in LOB**: `then:` does work four times on a nine-command AP model and zero times on three games, which is the shape a good key should have under the amendment — dead weight where the product is simple, load-bearing where the state does not determine the surface. Two facts the hypothesis did not name belong to emlang (E1 origin resolution, E2 true trigger sets) and would make D1-D3 references rather than heuristics. Pre-registered tripwires: (a) if p1-lob composes surfaces such that every `then` in §2 restates the default, C2 falls to dead weight and is withdrawn; (b) if any LOB product needs a destination that depends on outcome or state, `then` has met F1 and is withdrawn rather than extended; (c) if game #4 or a LOB flow has a command-less surface with two exits, D5 is falsified and C8 is re-run. Uncertainty: the entire LOB count rests on a model I wrote; D1/D6's "no `during` means stream-less" is a heuristic that a phase-filtered worklist would break.
