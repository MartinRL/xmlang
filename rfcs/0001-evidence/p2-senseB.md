# P2 / agent 6 — Sense B: dialog / state model

Scope: what the user can do at each moment (enablement), fine surface selection inside one `during` × `for` cell, transitions, and the rejected `when:` predicate. Evidence: xmlang spec v0.5.0 (`C:\code\GitHub\xmlang\xmlang-spec.md`, cited as XSPEC), the four Phase 1 reports, the three fixtures (BB/MEM/TTT `.em.yaml`), the three kvissig `.xm.yaml`, the three `*Surfaces.cs` selectors, the three C1 probes, and `Blindbudet.Domain/Decider.Impl.cs`. Per the BRIEF amendment, test 2 is judged on a LOB model first; `p1-lob.md` had not landed, so §2 uses **my own minimal accounts-payable construction** (marked as such); the games are the negative control.

## Lead findings

1. **Fine surface selection is derivable, and the spec is wrong about it in an interesting way.** XSPEC's `during` section says fine selection "is deliberately NOT expressible" and "a hand-written selector in the concrete stratum". Six independent implementations (AS:21-37, GS:23-42, TS:21-36; C1 run1:598-609, run2:690-704, run3:489-497) computed the *same* function from the Event Model, differing only on the unknown-viewer branch. The function is: within the active cell, the viewer stands on the terminal view of the slice whose event fired last *for that viewer*. That is the Event Model's own storyboard read as a per-viewer cursor. It is not a `when:`; it is a Defaults row (§4).
2. **Per-viewer enablement is not derivable as a predicate, but the correct default behaviour is statable without the predicate.** The Event Model carries guard *names* (`x: AlreadyBid`, `x: NotEnoughPlayers`) and decider-true examples (BB L401-425); the predicate is decider code (`Decider.Impl.cs:154,171-175`). The interpreter's enablement rule `players.Count >= 2` (AS:64) is a *hand copy* of that guard — MBUI failure F2 in miniature. A Defaults row can say what to do with a guard that would reject ("do not offer as actionable; show it disabled with the guard's label") without xmlang ever holding the predicate.
3. **Hide-vs-disable is a design-system rule, not a per-command judgment.** Live games hide (R14); all three C1 probes disable; Fiori/Lightning-style guidance is "hide what the role may never do, disable what the state currently forbids". xmlang already expresses the first half (`for:`); the second half is a Defaults row. As per-command vocabulary it fails test 2 on both LOB and games.
4. **The one thing that survives MBUI's two attacks is the closed state × event table, and emlang already is that table** (phase enum + decider-true GWT). xmlang can add nothing to it except a lint that checks compositions against it (`xm-command-phase-mismatch`, §2).
5. **Nothing here needs a resolvable predicate in emlang.** The cursor uses events, not guards; enablement uses the decider at runtime (or the GWT by a human). Two emlang-side items help: a coverage lint that makes the phase table trustworthy, and the already-flagged trigger fix for host-paced gears. Neither is a construct.
6. **The genuine residue in this sense is a transition with no event** (MEM's tap-through R9; in LOB an edit mode entered without a lock, a wizard step, a filter panel). That is navigation (sense A), not dialog; and the honest fix is often to model the intent as a command (`BeginEditing` → `EditingStarted`), after which the cursor derivation covers it.

## 1. Verdict table

Tests: 1 derivability (pass = NOT derivable), 2 dead weight (pass = ≥1 genuine non-default on LOB; games = control), 3 geometry leak, 4 slope, 5 transformer neutrality. "—" = not reached.

| # | Candidate | 1 | 2 (LOB / games) | 3 | 4 | 5 | Placement | Admit? |
|---|---|---|---|---|---|---|---|---|
| B1 | `when:` predicate for fine selection (Non-goal) | FAIL: derivable (§4) | — | pass | FAIL: a predicate language grows by definition (p1-mbui F1) | pass | none | **Reject** (keep Non-goal; strengthen reason) |
| B2 | Fine selection = per-viewer cursor along the slice chain | FAIL as vocabulary: derivable | n/a (default) | pass | pass | pass (all six transformers wrote it) | **xmlang Defaults row** + non-normative note | **Admit as Default** |
| B3 | `stage:` / `after: <Command>` on a surface (explicit ordering within a cell) | FAIL: restates B2 | fail / fail (every value equals the chain position) | pass | fail (needs a viewer-identity key to work) | pass | none | **Reject** |
| B4 | Enablement default: a command whose actor/state guard would reject is shown disabled with the guard's label | not vocabulary | n/a (default) | pass | mild: wants exception labels (sense C) | pass (runtime dry-runs the decider; codegen emits the call; human reads GWT) | **xmlang Defaults row** | **Admit as Default** |
| B5 | `guards: {X: hide\|disable\|defer}` per composed command | pass (judgment) | weak / fail: one product-wide rule; LOB design systems set it per system, `for:` already is `hide` | pass | fail: demands exception labels *and* a guard-classification to know which guards are pre-evaluable | pass | none (B4 covers it) | **Reject** |
| B6 | Persona binding: `personas.X.identity: hostPlayerId` (R7) | pass | fail / fail: LOB persona = auth claim; games all `hostPlayerId` | pass | pass | fail: meaningless to a human transformer (authn plumbing) | concrete stratum | **Reject** |
| B7 | `posture: read-only\|editable` per surface | FAIL: composes a `c:` or not (p1-deriv row 17) | — | — | — | — | derivable | **Reject** |
| B8 | Explicit phase-transition table in xmlang | FAIL: derivable from `phase` enum + GWT (p1-deriv row 6) | — | — | — | — | emlang lint (§5) | **Reject** |
| B9 | Guard as resolvable predicate in emlang (lead d) | n/a (emlang) | — | — | FAIL: moves F1 into emlang; GWT already are the predicate's examples, the decider its body | — | none | **Reject** |
| B10 | Guard classification actor/state/input in emlang | derivable from GWT given-shape (§5) | — | — | — | — | emlang lint, no construct | **Reject as construct** |
| B11 | Lint `xm-command-phase-mismatch` | zero vocabulary | LOB: fires on `Approve` composed on a draft surface | pass | pass | pass | **xmlang lint** | **Admit** |
| B12 | Command-less transition (R9; LOB edit mode, wizard step) | pass | LOB: yes (view→edit without lock) / MEM: yes | pass | ? | pass | **sense A**, with an emlang alternative (§5) | Handed to agent A |
| B13 | Next/End mutual exclusion (R15) | FAIL: Todo gate + guard; covered by B4 | — | — | — | — | Default (B4) | **Reject** |
| B14 | Spectator / no-persona viewer (R8) | covered by B2 note | fail / fail (all games identical; LOB: unauthenticated → login, concrete) | — | — | — | B2 note | **Reject** |
| L1 | Garrett conceptual model, Nielsen error prevention, configurators (p1-ixd #1, #20, #59), Dubberly types (#53) | FAIL: they *are* the Event Model | — | — | — | — | emlang | **Reject** |

Net: zero new vocabulary; two Defaults rows; one lint; one emlang lint; one handover to sense A.

## 2. Admitted items

### 2.1 LOB construction (my own; not from `p1-lob.md`)

Accounts-payable invoice, one stream, `State / Invoice` with `phase: InvoiceStatus (draft|submitted|approved|posted|voided)`; roles Clerk, Approver, Auditor, Admin. Slices (terminal view in parentheses): `CreateInvoice`→`InvoiceDrafted` (Invoice detail); `EditInvoice` x: `LockedByAnotherUser`, `StaleVersion` → `InvoiceEdited` (Invoice detail); `SubmitForApproval` x: `MissingLines` → `InvoiceSubmitted` (Invoice detail); `ApproveInvoice` x: `CannotApproveOwnInvoice`, `ExceedsApprovalLimit`, `AlreadyApprovedByYou` → `ApprovalGiven` (Approval progress) and, via a System gear `PostWhenFullyApproved` gated by `Todo / Outstanding approvals`, → `InvoicePosted` (Invoice detail); `RejectInvoice {reason}` → `InvoiceRejected` (Invoice detail, phase draft); `VoidInvoice {reason}` (Admin) x: `NotPosted` → `InvoiceVoided` (Invoice detail). Two-approver parallel approval makes `submitted` the LOB analogue of the games' `started`: one cell, three surfaces (act / wait for co-approver / result).

### 2.2 Normative text (proposed additions to XSPEC Defaults)

> **Surface selection within a cell.** When `during` × `for` yields more than one surface for a viewer, conforming transformers SHOULD select the surface that composes the terminal view of the slice whose event fired most recently *for that viewer* on the stream. An event has fired for the viewer if it carries no actor identity, or carries the viewer's identity, or was produced by a `System`-triggered or other-persona-triggered command; an event carrying another actor's identity from the viewer's own persona MUST NOT advance the viewer. If several surfaces in the cell compose that view, the transformer SHOULD prefer the one whose composed commands are not all withheld (see Enablement), then the first declared. A viewer resolving to no persona stands at the start of the chain. This derivation replaces, and MUST NOT be overridden by, a predicate vocabulary (see Non-goals).

> **Enablement.** A composed command is offered as actionable only in phases where the Event Model shows it can succeed (`during` is the coarse filter, `xm-command-phase-mismatch` the check). Within an active surface, a command that the Event Model shows would be rejected for this viewer in the current state independent of typed input — a rejection scenario whose `given` differs from the success scenario's while its `when` props match — SHOULD NOT be rendered as actionable; it SHOULD be rendered disabled, accompanied by the label of the blocking exception when one exists, and otherwise omitted. Commands the viewer's persona may never issue are absent by `for:`; input-validation and concurrency rejections (`StaleVersion`) are reported after the attempt.

### 2.3 Defaults-table rows

| Absent | Default |
|---|---|
| (fine selection within a `during`×`for` cell) | The surface composing the terminal view of the last slice fired for the viewer; ties broken by offerable commands, then declaration order |
| (enablement of a composed command) | Actionable iff the Event Model shows it can succeed now for this viewer; otherwise disabled with the blocking exception's label; never hidden by state alone (`for:` hides by role) |

### 2.4 YAML — LOB first (`# proposed` marks what the defaults now determine; no new keys)

```yaml
# ap-invoice.xm.yaml — my construction; the Event Model sketch is in §2.1
xmlang: "0.5"
model: ap-invoice.em.yaml
personas:
  Clerk:    { role: Clerk }
  Approver: { role: Approver }
  Auditor:  { role: Auditor }
  Admin:    { role: Admin }
surfaces:
  Draft:
    during: [draft]
    for: [Clerk]
    compose:
      - v: Invoice detail
      - c: EditInvoice          # proposed default: disabled + label "LockedByAnotherUser" while Bea holds the lock;
                                #   StaleVersion is a concurrency rejection → reported after the attempt
      - c: SubmitForApproval    # proposed default: disabled + "MissingLines" until a line exists
  ApprovalAct:
    during: [submitted]
    for: [Approver]
    compose:
      - v: Invoice detail
      - c: ApproveInvoice       # proposed default: disabled + "CannotApproveOwnInvoice" for the clerk-approver;
                                #   disabled + "ExceedsApprovalLimit" for a junior approver — same surface, no when:
      - c: RejectInvoice
        prominence: secondary
  ApprovalWait:
    during: [submitted]
    for: [Approver]
    compose:
      - v: Approval progress    # proposed default (cursor): selected iff the viewer's own ApprovalGiven is the
        self: approvedBy        #   latest event fired for the viewer — the LOB "Väntan"
  Posted:
    during: [posted]
    compose:
      - v: Invoice detail
      - c: VoidInvoice          # for: Admin would hide it by role; composed here for the Admin persona only via
        prominence: overflow    #   a for:-scoped twin in practice — shown unscoped to exercise the lint below
  AuditTrail:
    for: [Auditor]
    compose:
      - v: Audit trail
# lint (proposed): xm-command-phase-mismatch — warning — `VoidInvoice` on a surface active in `posted` is fine;
# composing `ApproveInvoice` on Draft would fire: no success scenario with given phase: draft.
```

Genuine work the defaults do on this LOB model: three commands on one surface (`ApprovalAct`) carry three *different* per-viewer enablement outcomes for three viewers without a single annotation; the two-approver cell selects `ApprovalWait` for the approver who has acted and `ApprovalAct` for the one who has not, again with no annotation. Test 2 for the *defaults* is moot (defaults are not vocabulary); what matters is that the LOB shapes the amendment names — approver cannot approve own invoice, edit only in draft, locked by another user — are all covered by `during`, `for:`, and the two rows, so no vocabulary is *missing*.

### 2.5 YAML — games (negative control)

```yaml
# blindbudet.xm.yaml, started-phase cell — unchanged text; comments show the defaults resolving it
  Budgivning:            # proposed default (cursor): terminal view of AuctionStarted / NextLotStarted → Lot card
    during: [started]
    compose: [{ v: Lot card }, { c: PlaceBid }]
  Väntan:                # proposed default (cursor): terminal view of the viewer's own BidPlaced → Bid progress
    during: [started]
    compose: [{ v: Bid progress, self: submittedPlayerIds }]
  RundresultatSpelare:   # proposed default (cursor): terminal view of System's LotRevealed → Round scores
    during: [started]
    for: [Spelare]
    compose: [{ v: Round scores, self: playerProfits.playerId }]
  LobbyVärd:
    during: [lobby]
    for: [Värd]
    compose:
      - v: Roster
      - c: StartAuction  # proposed default (enablement): disabled + label of NotEnoughPlayers until 2 players
                         #   (live hides and hand-writes the footer copy, AS:64,76; C1 probes disabled)
```

MEM: the cursor puts a player who has submitted a direction, after `QuestionDirectionRevealed`, on `Riktningsavslöjande` (terminal view `Direction reveal`, MEM L795) — exactly GS:42. `Skillnadsfråga` is never selected by the cursor, exactly as `GameSurfaces.Select` never returns it; it is entered by the tap-through GET (GE:428-446), which is sense A residue. TTT: `ScoreRound` firing on a poll (TE:652) yields `RoundScored` → `Round scores` → results, matching TS:33-35 even when the viewer never submitted.

### 2.6 Lint rules (proposed)

| Rule | Severity | Meaning |
|---|---|---|
| `xm-command-phase-mismatch` | warning | A command is composed on a surface whose `during` includes a phase in which the Event Model has no success scenario for that command (or the surface has no `during` and the command succeeds only in some phases) |
| `xm-cell-ambiguous` | info | Two surfaces in one `during`×`for` cell compose the same view and neither composes a command; the cursor cannot distinguish them (MEM `Riktningsavslöjande` vs a hypothetical twin) |

### 2.7 Strongest unanswered objections

*Against B2:* the derivation rests on three conventions the emlang spec does not state — slice element order is a timeline (p1-deriv row 1), an event prop carries actor identity (the games' `playerId`; `self:` names it on views only), and same-persona peers do not advance you. In LOB, where surfaces are reached by navigating documents, the cursor is needed only in synchronous multi-actor cells (parallel approvals, live collaboration); elsewhere it degenerates to "the one surface in the cell". A seven-line pure function per product may be cheaper than a paragraph every transformer must implement identically. Counter: every transformer wrote the seven lines anyway, and they diverged only where the paragraph would have settled it (R8).

*Against B4:* "disabled with reason" contradicts all three shipped games (they hide), and it needs exception labels to be complete — a labels extension from sense C. Also, a runtime can dry-run the decider only for button commands; for commands with typed input it must trust the GWT given-shape classification (§5), which is a heuristic over scenario authoring discipline.

## 3. Rejected items

| Item | Failing test | Non-goals text (proposed) |
|---|---|---|
| B1 `when:` | 1 and 4 | **A `when:` predicate language** — not only dangerous but redundant: fine selection within a `during`×`for` cell is derived from the Event Model (see Defaults, surface selection); six independent transformers computed the same selector. Anything a `when:` would express is either that derivation, a missing `phase` value, a missing command (an intent with no event), or a guard that belongs in the decider |
| B3 `stage:`/`after:` | 1 | folded into the `when:` entry: an explicit stage restates the slice chain |
| B5 per-command hide/disable | 2, 4 | **Enablement posture per command** — hide-by-role is `for:`, disable-by-state is the Defaults row; a per-command switch restates a product-wide design-system rule |
| B6 persona binding | 2, 5 | **Viewer-to-persona resolution** — authentication and authorization plumbing; `self:` names where identity lives on a view, nothing maps a viewer to a persona |
| B7 posture | 1 | (already covered by "read-only vs editable is whether a surface composes a command") |
| B8 transition table | 1 | **A dialog or transition model** — the Event Model's `phase` enum and decider-true scenarios are the state × event table; xmlang consumes it through `during` and checks against it, never restates it |
| B9 predicate in emlang | 4 (in emlang) | not xmlang text; recorded in §5 |
| B13, B14, L1 | 1 | covered by the above |

## 4. The derivation of fine surface selection

**Defaults-table entry:** see §2.3, row 1.

**Non-normative note (proposed for XSPEC after the `during` section):**

> Fine selection within a cell was declared inexpressible in v0.2 and implemented as a hand-written selector in every product. It turned out to be derivable. Read the phase's slices in order; each state-change slice ends in the view the actor sees next. A viewer's position is the terminal view of the last slice whose event fired for her: her own command's event moves her (PlaceBid → Bid progress: the waiting surface), a System processor's event moves everyone (LotRevealed → Round scores: the results surface), another persona's event moves her (StartAuction → Lot card), and a peer's event does not (another player's BidPlaced leaves her on the input surface). The surface is the one in the cell composing that view for her persona. This reproduces the selectors of all three testbed games and all three clean-room probes; where it does not decide (two surfaces composing the same view) the enablement default breaks the tie, and where a surface is entered by no event at all the transition is navigation, not dialog.

**Verification against the code.** BB: `SelectStarted` = `resolved → Rundresultat*; viewer ∈ bids → Väntan; else Budgivning` (AS:31-37). Cursor: `LotRevealed` (System, BB L533) is the latest fired-for-viewer event iff `resolved` (Decider.Impl.cs:66 sets `Resolved` on reveal); else the viewer's own `BidPlaced` (BB L354, carries `playerId` L356-360) iff `viewer ∈ bids`; else `AuctionStarted`/`NextLotStarted` (BB L265, L786) → `Lot card`. Identical. TTT: same with `PuzzleRevealed`/`SolutionSubmitted` (TTT L591, L363; TS:33-36). MEM: `Scored → Rundresultat*` = `QuestionDifferenceRevealed`/`DifferenceScored` (MEM L871-876) → `Round scores` (L886); `!revealed ∧ own DirectionSubmitted → Väntan` = L436→L443 `Guess progress`; `revealed ∧ ¬own DifferenceSubmitted → Riktningsavslöjande` = `QuestionDirectionRevealed` (L782) → `Direction reveal` (L795); `own DifferenceSubmitted → Väntan` = L537→L544. Identical to GS:33-42. The C1 probes match on every branch except the null viewer (run1/run3 → join form; run2 → "Du hann inte med"; live → input surface, R8): the note's "stands at the start of the chain" would select the input surface, which is arguably a defect the products share; a transformer MAY route a persona-less viewer to the join bare form instead, and the spec should say which. I recommend the bare form, since a viewer with no identity cannot issue the input command anyway (AE:160-166 ignores the POST).

**Where it needs help.** (a) `QuestionAsked`-style events whose terminal view is composed on two surfaces in the cell (MEM `Question card` on `Riktningsfråga` and `Skillnadsfråga`): the enablement default withholds `SubmitDifference` (`x: DirectionNotRevealed`, MEM L534, a state guard) and so prefers `Riktningsfråga`. (b) The identity prop on events is a convention (`playerId`); `self:` states it for views only. (c) In LOB with a single surface per cell the derivation is trivially true and costs nothing.

**Counter-example that limits, not breaks, it:** MEM `Skillnadsfråga` (XM-MEM L122-132) is reachable only by a GET that writes nothing (GE:428-446; XM-MEM finding 8). No event, no cursor. Same shape in LOB: view → edit mode under optimistic locking. Both are transitions without events; the dialog model cannot see them because the behaviour model does not, and the two honest fixes are (i) model the intent as a command (`BeginEditing` → `EditingStarted {userId}`; MEM `AcknowledgeDirection`), after which the cursor covers it, or (ii) accept it as navigation residue for sense A. Neither is a `when:`.

## 5. What belongs in emlang

**5.1 Lifecycle coverage lint (`em-phase-transition-uncovered`)** — not a construct. The phase table xmlang consumes is only as complete as the author's scenarios: BB has no State projection asserting `phase: ended` (p1-deriv row 6). A lint that requires every declared phase value to be entered by at least one projection scenario and every command to have at least one success scenario with a `phase` in its `given` makes `during`, B2 and B11 trustworthy. Rationale: a closed state × event table that is *checked* is exactly the statechart that survived MBUI (p1-mbui §3); emlang already has the table, it lacks the check. xmlang consumes it as today, plus `xm-command-phase-mismatch`.

**5.2 Guard classification by scenario shape** — a convention to document, not a key. A rejection scenario whose `given` equals the success scenario's and whose `when` differs is an input guard (`BidNegative`, BB L388-399); one whose `given` differs and whose `when` differs only in actor identity is an actor guard (`AlreadyBid`, BB L401-412: `lots: [lot0Nils50]` with `playerId: nilsId`; LOB `CannotApproveOwnInvoice`); the remainder are state guards (`NotEnoughPlayers`, `LockedByAnotherUser`). B4 needs the first class separated from the other two; emlang can state that decider-true scenarios make it computable, and lint scenarios that are ambiguous (`em-rejection-shape-ambiguous`, info). xmlang consumes the classification in its enablement default. This is explicitly *instead of* a resolvable predicate (B9): the predicate lives in the decider and its examples in GWT; that is the one-way rule applied inside emlang.

**5.3 Actor identity on events** — the smallest real gap. The cursor needs to know which event prop names the actor (`playerId`; LOB `approvedBy`, `editedBy`). Today it is a naming convention; `self:` covers views only. If emlang ever marks it (a note on the trigger, e.g. `t: Player /Lot  # actor: playerId`), xmlang's `self:` and the cursor default resolve against it; until then the Defaults note says "the event prop carrying the viewer's identity" and leaves the match to the transformer. Dead-weight check: identical in 3/3 games, but LOB models routinely carry several identity props per event (`createdBy`, `approvedBy`, `assignedTo`), so the ambiguity is real there.

**5.4 Trigger fix for host-paced gears (R17)** — already identified by p1-deriv §4a; belongs to sense A/C. Noted because after the fix `AskNextLot`/`EndAuction` become host-triggered commands whose `hasNextLot` Todo gate (BB L742) makes B4 select exactly one of them, replacing AS:177-180.

## 6. Verdict on H-B

H-B is **confirmed and sharpened**. The dialog model lives in emlang: phases, guards and decider-true scenarios are a closed state × event table, the only MBUI dialog artefact that survived, and xmlang consumes it through `during` without restating it. Everything xmlang could add is derivable or a `when:` in disguise — but the derivable part is larger than the spec admits. The fine selector the `during` section calls inexpressible is computable from the slice chain per viewer, and the enablement the interpreter hand-copies from the decider is statable as a default over guard names without the predicate. So the recommendation is not to relax the `when:` rejection but to replace its justification: `when:` is rejected because it is *redundant*, and the two things it would have expressed become Defaults rows plus one lint. The remaining residue — a surface entered by no event — is a navigation fact for sense A or a missing command for emlang, not a dialog fact for xmlang. On the LOB shapes the amendment names (own-invoice approval, edit only in draft, lock held by another user, parallel approvals), `for:`, `during` and the two defaults cover every case I could construct; where they did not, the missing piece was an event, never a predicate.
