## Recommendation: carving the emlang RFCs

### Ground fact that shapes every cut

emlang v1.0.0 is closed at every level: root (`slices` only), element (one type key + `props`, `additionalProperties: false`), extended slice (`steps`/`tests` only), and the three test-item schemas (`givenElement`, `commandElement`, `thenElement`, all `false`). The spec has no versioning, extension or profile section. So **every new key is breaking for a v1.0.0 validator**, no matter how "additive" it feels, and the local `EmAst` hard-fails on unknown keys (`EmAst.cs:340`). The only genuinely non-breaking moves are (a) rules on existing free text (`props` is the sole open object; names and lanes are prose), and (b) lints. That yields the ordering principle: **lints and prose conventions first, `props`-carried facts second, schema-touching constructs last and each behind an explicit opt-in.**

### A. The cut, in order

**RFC E1 — Profiles and the `emlang:` document header.** Scope: adds one optional root key `emlang: { version, profile }` (or `profile: decider`) so a document can declare which additional rules it submits to; defines "profile" as a conformance level whose rules are a strict superset of v1.0.0 and whose documents MUST remain parseable by v1.0.0 tools when the header is stripped. Touches: SPEC §5 Document Structure; schema root `properties`/`additionalProperties`. Breaking: yes for validators (root is closed), which is exactly why it goes first and alone: it is the *single* schema change every later RFC depends on, and a v1.0.0 tool's only failure mode is "unknown root key". Must NOT include: any profile's actual rules.

**RFC E2 — Decider profile (`profile: decider`).** Scope: the maintainer's 1:1 map: normative `State /` lane, given-shape rule, fold-precedence rule, phase-per-decider. Touches: SPEC §4 Tests (prose constraints on `given`), a new §7 Profiles; schema: none (all rules are on existing constructs and are lint-enforced). Breaking: no for parsers; documents remain v1.0.0-valid. Must NOT include `s:`, params, compensation, triggers.

**RFC E3 — Trigger origin and trigger sets.** Scope: text after the trigger's `/` MUST resolve to a `v:` name in the same document or be declared a bare form (`t: Role /Join form` with a companion rule: a bare form is a trigger origin appearing in no view); a slice MAY carry several `t:` and each denotes a legal initiator of the same command. Touches: SPEC §2 Trigger (swimlane semantics, L70-79 region), §3 Slices ("sequence" gets one rule: triggers precede the command). Breaking: no (spec already allows multiple `t:`; resolution is a lint). Must NOT include: any navigation semantics (that is xmlang's D-entry/D-destination).

**RFC E4 — View parameters.** Scope: a `params` key on view elements (`v: Due payments, params: {asOf: DateTimeOffset}, props: {...}`) separating projection inputs from projected data. Touches: SPEC §2 View, element schema (adds `params` beside `props`). Breaking: yes, element is closed. Alternative that is non-breaking and I recommend: a `props` note convention `asOf: DateTimeOffset @param` (props is free-form) plus a lint; promote to a key only if a second consumer beyond xmlang needs it. Must NOT include: query languages, filter semantics.

**RFC E5 — Compensation.** Scope: `compensates: &lt;CommandName&gt;` on a command element. Touches: SPEC §2 Command, element schema. Breaking: yes. Dead weight on every testbed (p1 §4a: zero undo pairs; lob-ap has Void/Reverse but xmlang derives nothing). **Recommend deferring**; write it only as a Non-goal note in E2 with a re-open condition.

**RFC E6 — Lint set: scenario coverage, actor identity, fixture completeness.** Scope: `em-phase-transition-uncovered` (every command has a success scenario in each phase it may succeed in), `em-actor-identity` (a stated prop-naming convention, e.g. `&lt;role&gt;Id`, so events carry who acted), `em-view-information-incomplete`. Touches: a new SPEC "Lint rules (non-normative)" appendix. Breaking: no. Must NOT include: any construct. Depends on E2 (phase per decider) for the coverage lint.

Order: E1 → E2 → E3 → E6, E4 as a props-note; E5 deferred. E2 and E3 are independent of each other; both depend only on E1 for the opt-in header (E3 could even ship without E1 as pure lints).

### B. The decider RFC (E2)

**`s:` vs promoted `v: State /`.** Ruling: **keep `v: State /`, promote it to normative text inside the decider profile; do not add `s:`.** Strongest objection to `s:`: Event Modeling's own picture has four lanes (trigger, command, event, view) and state *is* a view (the decider's read model of its own stream); five kinds is a virtue because every kind maps to a box on the canvas, and `s:` would be the first element with no canvas counterpart. Practical corollaries: `s:` breaks `givenElement`, `thenElement`, `element`, every Go reference tool, and `em fmt` round-trip; `v: State /` breaks nothing and the maintainer's own phrase "perhaps s: rather than v:" is hedged. The cost of the ruling is that the lane name `State` becomes reserved prose within the profile; the RFC should state that explicitly and offer `st:` only as a Non-goal with a re-open condition ("if a second tool needs to distinguish state from view without the profile header").

**Given-shape rule.** Under `profile: decider`: a test whose `when` is a command MUST have `given` of zero elements or exactly one `v: State /X`; a test with no `when` (fold/projection) MUST have `given` of events only. Enforced by `em-given-events-in-decision` (error under profile, warning otherwise), `em-given-mixed`, `em-given-multi-state`. Local practice already complies (128/128 state-change tests; zero mixed), so the rule restates the models rather than migrating them; the generator's uncompilable multi-State branch becomes unreachable.

**State preceded by its fold.** Every `v: State /X` appearing in any `given` MUST be the `then` of at least one events-only test somewhere in the document (**reference, not document order**). Document order is the wrong axis: local practice places the Decision Model slice trailing, and the spec attaches no meaning to slice order. Lint: `em-state-without-fold`.

**Todo givens.** They are *not* decider state: `Todo / Due payments` is a processor read-model over a stream, folded from events like any view. In a decider-true test an automation command's `given` should be the decider's `State` (the decider validates; the Todo only selected the instance). So a Todo given is a **smell** under the profile (`em-given-todo`, warning), with the correct form being `given: v: State / Invoice {phase: scheduled}` and the Todo left to its own projection tests. Not a second decider: a decider is named by its State lane, and there is no `State / Todo`.

**Phase namespace.** Each `v: State /&lt;Decider&gt;` may carry `phase: &lt;Enum&gt; (a|b|c)`; phases are namespaced by decider (`Invoice.scheduled`). Rule: a bare phase value used in two deciders is an error. xmlang's `during` gains the map form already admitted (SYNTHESIS R5); `EmSpec.PhaseValues` becomes `PhaseValues[decider]`. `self:` resolves through E6's actor-identity convention against the decider's stream events, not through state.

### C. Compatibility posture

Propose upstream, but ship as a profile. "My dialect" is honest naming for E2's rules; the mechanism to keep it from forking is E1: a document with `emlang: {profile: decider}` is v1.0.0 minus one root key, and the RFC should require that `em fmt` preserves the header verbatim and that stripping it yields a v1.0.0-valid document. Go reference tools: E2, E3, E6 need zero changes to pass; only E1's root key makes them reject the file until upstream accepts it. If upstream declines E1, the fallback is a YAML comment header `# emlang: profile=decider` parsed by local tools, which forks nothing.

### D. Lint vs construct

Lints/conventions (no schema change): trigger origin resolution (E3), multiple triggers (already legal; needs prose), given-shape, fold precedence, Todo smell, phase-per-decider (prose on `props`), scenario coverage, actor identity, information completeness. Constructs: **only the profile header** (E1) is required; `params` is optional and deferrable to a props note; `compensates` is deferred. Minimal construct set: one root key.

### E. Risks (one line each)

- E1: upstream rejects any root key on closed-schema grounds; fallback is a comment header and the profile becomes a local dialect.
- E2: reserving the `State` lane name collides with a user who uses `State /` for a human screen; the profile must say the name is reserved.
- E2: "reference, not order" for fold precedence lets a state fold live in another YAML document; the rule must scope to the file.
- E3: strict origin resolution flags ~5 mismatches per game fixture and forces bare-form declarations for join screens; that is real migration work.
- E3: multiple triggers make D-enablement depend on emlang shipping first; xmlang must gate the rule on it.
- E4: a props note is invisible to the schema and will be silently ignored by tools that do not know it.
- E6: coverage lint fires on every existing model today; it must ship as warning with a documented count per fixture.
- All: no second modeller has written under these rules; every "restates practice" claim rests on four models by one team.

### Critical Files for Implementation
- C:\code\GitHub\xmlang\src\emlang\Emlang\Linting\EmAst.cs (unknown-key hard error at line 340; root/element acceptance for the profile header)
- C:\code\GitHub\xmlang\src\emlang\Emlang\EmParser.cs (`PhaseValues` lines 145-151 to become per-decider; trigger origin kept at 104-121)
- C:\code\GitHub\xmlang\src\emlang\Emlang\Linting\Linter.cs (new profile lint rules)
- C:\code\GitHub\xmlang\src\emlang\Emlang\TestsEmitter.cs (`EmitGiven` line 112-127, the given-shape rule makes the Fold branch for decision tests unreachable)
- C:\code\GitHub\xmlang\rfcs\0001-evidence\lob-ap.em.yaml (Todo givens at 933/945/1110 and the `phase` namespace workaround at 1267 are the test cases for E2)