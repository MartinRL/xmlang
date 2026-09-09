I have everything. Findings below.

---

## 1. Fixtures

**(a) `State /` lane declaration.** State is a *view* (`v:`), never its own element. Every spec puts it in a trailing appendix slice explicitly marked as "not a moment in the timeline":

- `tests/Emlang.Tests/fixtures/blindbudet.em.yaml:920` `👀 Decision Model:` → `:922 - v: State / Game`, phase enum at `:926` `phase: AuctionPhase (lobby|started|ended)`; `DERIVED (computed, NOT stored…)` block `:939-944`.
- `mer-eller-mindre.em.yaml:1291` `👀 Decision Model`; `tank-till-tusen.em.yaml:1009-1015` (`phase: TankPhase (lobby|started|ended)`, DERIVED at `:1028`).
- `rfcs/0001-evidence/lob-ap.em.yaml:1261` `👀 Supplier Decision Model` (`- v: State / Supplier`, `:1263`; deliberately named `status:` not `phase:`, `:1267`) and `:1281` `👀 Invoice Decision Model` (`- v: State / Invoice`, phase enum `:1286`, `heldFromPhase: InvoicePhase?` `:1287`).

Header comments that state the convention verbatim: "emlang has no `state` element, so every command/processor slice expresses its Given as the View State / Game" — `blindbudet.em.yaml:23-26` and again `:894-897`; `mer-eller-mindre.em.yaml:21-27` ("Verification is decider-true… GWT given:s never replay events"); `tank-till-tusen.em.yaml:21-24`. Per-slice comments read `# Decider-true GWT: given the folded State / Game props decide reads…` (e.g. `blindbudet.em.yaml:362`, `:548`, `:787`, `:826`; `mer-eller-mindre.em.yaml:271`, `:444`, `:796`, `:887`). lob-ap's own header restates the lane table at `:11-12` and flags the multi-decider phase-namespace problem at `:21-25`.

**(b) `given:` shapes** — counted mechanically across the four files:

| shape | blindbudet | mer-eller-mindre | tank-till-tusen | lob-ap | total |
|---|---|---|---|---|---|
| `given:` all `e:` (events) | 12 | 15 | 12 | 15 | **54** |
| `given:` all `v: State / X` | 20 | 24 | 22 | 40 | **106** |
| `given:` all `v: Todo / X` | 0 | 0 | 0 | 3 | **3** |
| `given: []` (empty) | 5 | 7 | 6 | 4 | **22** |
| mixed kinds in one `given:` | 0 | 0 | 0 | 0 | **0** |
| total tests | 37 | 46 | 40 | 62 | **185** |

Events shape, verbatim (`blindbudet.em.yaml:171-175`):
```
        given:
          - e: Game / AuctionOpened
            props: { hostPlayerId: martinId, hostName: Martin, joinCode: joinCode, lots: [lot0, lot1] }
          - e: Game / PlayerJoined
            props: { playerId: nilsId, playerName: Nils }
```
second at `lob-ap.em.yaml:1299-1309` (five `- e: Invoice / …`).

State shape, verbatim (`blindbudet.em.yaml:207-212`):
```
        given:
          - v: State / Game
            props:
              phase: lobby
              joinCode: joinCode
              players: [hostMartin]
```
second, inline-map form (`lob-ap.em.yaml:375-376`):
```
          - v: State / Invoice
            props: { invoiceId: inv1, phase: draft, version: 1 }
```
Empty, verbatim: `blindbudet.em.yaml:139` and `:153` — `given: []`.
Todo-lane given (the only non-State/non-event given), `lob-ap.em.yaml:932-934`:
```
        given:
          - v: Todo / Due payments
            props: { asOf: 2026-09-30, due: [dueInv1], hasDue: true }
```

**(c) Coverage.** Yes, both directions hold, with the marker convention perfectly regular: every `👀`/`📋` slice's tests are event-given projections, and every `✍️` slice's tests are `v: State / X` (or `given: []` for the empty-stream rejection). Per-slice audit shows only two exceptions, both in lob-ap: `✍️ Execute Payment Run` (`:907`) and `✍️ Remind Approvers` (`:1093`) give a **`Todo /`** view rather than `State /`. Every declared State view has its own fold-GT: blindbudet `:946`+`:962`, mer-eller-mindre `:1291`ff (2 cases), tank-till-tusen `:1009`ff (2), lob-ap `:1271` (Supplier, 1) and `:1298`/`:1313`/`:1324` (Invoice, 3).

**(d) `Screen /` remnants:** zero in all four `.em.yaml` files. Surviving mentions are xmlang-side only: `xmlang-spec.md:222`, `:294` (`xm-screen-lane-view`, info), `tests/Xmlang.Tests/Fixtures.cs:43`, `ApprovalTests.cs:82`, `XmLinterTests.cs:236,328,331`, and the history note in `rfcs/0001-evidence/p1-derivability.md:92`.

**(e) Triggers:** `- t: &lt;emoji&gt; Role /Origin`, e.g. `blindbudet.em.yaml:121` `- t: 🧑‍🏫 host /Auction catalog`, `:190` `- t: 🧑‍🎓 Player /Join form`, `:526` `- t: ⚙️ System / Reveal lot`; lob-ap `:84` `- t: 🛠️ Admin /Supplier list`. Trigger totals: 7 / 9 / 7 / 20. **No slice has more than one `t:`** anywhere (scanned all 85 slices).

## 2. Parser

Three parallel parsers, all with the same closed kind table — **there is no `s:`**:
- `EmAst.cs:66-82` `Prefixes`: `t/trg/trigger`, `c/cmd/command`, `e/evt/event`, `x/err/exception`, `v/view` → `EmElementType` (`:13`, five values, no State).
- `EmParser.cs:86-92` and `SpecModel.cs:60-66`: `c/e/x/v` (+ long forms); `EmParser` handles `t` separately at `:104-108` and **discards the origin** — `CollectRole` (`:116-121`) keeps only the part before the last slash.
- `TestModel.cs:81-84`: `c/e/x/v` only; a step without one throws (`:101`).

Lane split is uniformly "prefix before the last slash" (`EmParser.cs:35-41`, `SpecModel.cs:81-89`); `EmAst.cs:327` uses *first* slash instead. `State` is special-cased in exactly one place: `EmParser.PhaseValues` (`:143-151`) filters `Kind=='v' &amp;&amp; Lane=="State"`, takes the field named `phase`, and parses the parenthesized `(a|b|c)` note (`EnumValues`, `:153-159`) into `EmSpec.PhaseValues` (`:22`) for xmlang's `during:` resolution. Given/when/then types are validated in `EmAst.cs:224-227`: `AllowedGiven = [Event, View]`, `AllowedWhen = [Command]`, `AllowedThen = [Event, View, Exception]` — so a state-only given is legal *only* because State is a View; a `t:` or `c:` in `given:` errors at `:271-273`. `TestModel` takes only the first `when` step (`:71`).

## 3. Linter

`Linting/Linter.cs` — no rules folder, three rules, **all `LintSeverity.Warning`** (hardcoded at `:92`), all suppressible via `ignoreRules`:

| rule id | severity | check |
|---|---|---|
| `command-without-event` | warning | `Linter.cs:53-56` — a `c:` not followed by an `e:` or `x:` before the next `c:` (`IsFollowedByEventOrException`, `:70-85`) |
| `orphan-exception` | warning | `:59-62` — an `x:` with no preceding `c:` in the slice |
| `slice-missing-event` | warning | `:65-67` — slice has no `e:` at all (reported at line 0) |

Empty slices are skipped (`:37`). **Nothing about given shape, State, phase, projection coverage, or trigger origin.** The doc comment (`:14-18`) pins this as a faithful port of the Go reference set, so any new rule is a divergence to negotiate.

## 4. Generators

`Emlang.Generators` only routes text (`EmlangRecordsGenerator.cs:60-63`, `EmlangTestsGenerator.cs:17-23`). Emission:
- From `State /` views: **nothing directly.** `SurfaceEmitter` (`:17`) filters by kind `c`/`e`/`x`, so `v:` elements never become records — `SpecModel.cs:19-20` says so explicitly ("'v' elements are inert for record emission"). The decider's state type is `EmitTarget.StateType` = `Prefix + "State"` (`EmitTarget.cs:14`), a **name convention, not derived from the `State / X` view at all**; the props of `State / Game` are only used to *type* test fixture values. `DeciderEmitter.cs:33-51` emits `Evolve(state, event)`, `Decide(state, command, context)` switches with no default arm plus `private static partial` declarations per `e:`/`c:` — bodies handwritten.
- From GWT tests: `TestsEmitter.EmitBody` (`:80-94`) classifies three shapes — `when:` present → `EmitDecideGwt`; no `when` + single `then: v:` with `Lane=="State"` → `EmitFoldGt` (`:173`); otherwise single `then: v:` → `EmitProjectionGt` (`:184`, calls `Projections.&lt;Name&gt;`).
- **The given branch is where State is load-bearing:** `EmitGiven` (`:112-133`) has exactly three arms — (1) `Given.Count == 1 &amp;&amp; Kind:'v', Lane:"State"` → `var given = XState.Initial with { … }`; (2) `Count == 0` → `XState.Initial`; (3) **everything else** → `Decider.Fold(new XEvent[] { … })`. So generated decide-tests use given *state* when the given is a single State view, and given *events* otherwise.

Two latent bugs the RFC can cite as motivation: arm (3) swallows both **multi-State givens** (`lob-ap.em.yaml:687-692`, three `v: State / Invoice`) and **`Todo /` givens** (`:932-934`, `:1109`) — `Construct` (`:227-237`) resolves the `v:` element fine and then emits `new Due payments(…)` / `new Invoice(…)` inside an `XEvent[]` initializer, i.e. uncompilable C# with no `SpecTestException` to catch it. A first-class state element / state-only `given` would make this a parse-time error instead.

## 5. Formatter

`EmFormatter.cs` renders **from the AST**, so it preserves nothing it cannot model: comments are dropped by design (`:8`), props and test names are re-sorted (`:47`, `:88`), and lanes are re-joined without spaces (`:82` → `State/Game`, not `State / Game`). Unknown keys don't survive because they never parse: `EmAst.ParseElement` throws `unknown key "…"` (`:340`), `ParseTest` throws `unknown test key` (`:256`), `ParseSlice` throws `unknown slice key` (`:187`), `ParseDocument` throws `unknown top-level key` (`:119`) — pinned by `tests/Emlang.Tests/LintingTests.cs:168,172`. So **`s:`, `params:` and `compensates:` are all hard parse errors today**, not silent drops; each needs `Prefixes`/key-switch, `EmElement`, `EmFormatter.TypeKey`/`WriteElement`, and the Go reference port. `p3-impact.md:36` flags exactly this ("`EmFormatter` must round-trip it (check it does not drop unknown step keys)") — the answer is: it cannot drop them, it rejects them.

## 6. Git

`git log --oneline -30` returns 17 commits; branches are `main` and `origin/main` only — no state/gwt/decider branch. Relevant subjects: `0ecdb11` bump to 0.3.0 (em fmt), `b46a68f` em fmt + stdin, `256f0c2` canonical formatter (Go reference port), `f09d782` em CLI phase 1, `cb4580b` line-aware AST and linter, `64224f4` host emlang as its own package family, `1d9ad84` pin parser and linter behavior with Verify approval snapshots, `2195ceb` xmlang v0.4.0. Nothing mentions state, gwt, decider, given, or projection — the decider-true GWT convention lives entirely in fixture comments, never in a commit or spec change. Note `1d9ad84`: adding fields to `EmSpec`/`EmElement` re-approves `tests/Xmlang.Tests/Snapshots/ApprovalTests.EmSpecShape.verified.txt`. Nothing was modified.