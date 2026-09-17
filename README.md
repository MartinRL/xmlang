# xmlang

Two YAML-based DSLs for specification-driven development:

- **emlang** — Event Model DSL. Records what the system *does*: commands, events, views, test scenarios, state. An event-sourced domain expressed as pure data.
- **xmlang** — Experience Model DSL. Records what users *see and do*: personas, surfaces, field salience, interaction judgments (`confirm:`, `then:`), journeys, labels, design tokens. **Judgment as data, never as geometry.** The Experience Model depends on the Event Model strictly one-way; every reference is lintable.

Together, they separate domain logic from UX judgment, and make both machine-readable. The specs drive a deterministic source generator (at build time, equality contract) for domain vocabulary, and an interpreter (at request time, conformance contract) for UI rendering. See [The Screens Left the Repo](https://martinrl.github.io/chronograph/the-screens-left-the-repo) for the design rationale.

The canonical specifications live here: [xmlang-spec.md](xmlang-spec.md) and [emlang-dialect.md](emlang-dialect.md). This repository is the reference implementation: parsers, linters, and CLIs for both dialects.

## Where xmlang sits

Event Modeling already has an experience row: step 3 of the workshop is the storyboard, the wireframes across the top of the blueprint. xmlang is that row as data instead of pictures. It keeps the judgment a wireframe carries (who sees which view and command, when, in what order of importance, with what friction, landing where) and refuses the geometry. The Event Model alone already determines a default experience (see [Defaults](xmlang-spec.md#defaults)); an xm file records only the judgments that depart from it, and each entry replaces one informal artifact: a persona doc, a sitemap, a journey map, a copy deck.

That is also why xmlang has no workshop format of its own. Its input is the storyboard row of an Event Modeling session, or a design. emlang's picture is the blueprint; an xm's picture is a rendered surface, and only one possible one.

| Technique | Unit | Form | Where it lands here |
|---|---|---|---|
| User story | *As a persona, I want capability, so that benefit* | Prose card, a promise of a conversation | Persona and capability are `for:` and `c:`. The Confirmation half (acceptance criteria) is emlang's Given/When/Then, not xm |
| Use case (Jacobson, Cockburn) | Actor, goal, main success scenario, extensions, preconditions | Structured prose, numbered steps, UI deliberately kept out | Actor is a persona, goal a command, each extension a named rejection, preconditions the `during:` phase. The success scenario is derived from the Event Model; xm records only the judgments on it (`confirm:`, `then:`, salience). xm is the place for what use cases were told to leave out |
| Domain Storytelling (Hofer, Schwentner) | One concrete story: actors, work objects, numbered activities | Pictographic diagram; one story, no branches | A discovery input, upstream of both models. Actors, work objects and activities become swimlanes, views and commands in emlang. xm is the record after discovery, and covers every declined path the story left out |
| Event Modeling storyboard | A wireframe per slice on the timeline | Sketch | xm, minus the geometry |
| Wireframe, Figma, design system | A screen | Geometry, components, visual language | Neither model. Layout belongs to the transformer, components and tokens to the design system ([RFC xmlang-0002](rfcs/xmlang-0002-frontend-architecture.md)) |
| BDD / Gherkin | A scenario | Given/When/Then | emlang, not xm |

Rule of thumb: if the sentence says what the system does, it is emlang. If it says what a person sees or decides, and the Event Model cannot derive it, it is xmlang. If it says where something is on the screen, it is neither.

### Packages

**Event Model (emlang)**
- **Emlang** — Parser (`EmParser`), line-aware AST, linter, and formatter.
- **Emlang.Cli** — `em` command-line tool. `em parse`, `em lint`, `em fmt` with support for stdin and formatting options.

**Experience Model (xmlang)**
- **Xmlang** — Parser (`XmParser`), linter, and interpreter (resolves xm specs against em specs).
- **Xmlang.Cli** — `xm` command-line tool. `xm lint` resolves and validates xm specs against their event models.

**Important:** This repository is the two DSLs, parsers, and linters only. Code generation and UI rendering are application concerns. Each app carries its own source generators and interpreters against its own conventions:
- kvissig.se has a Roslyn source generator for decider vocabulary (extracted 2026-09-14).
- RequestSample demonstrates both patterns: generated domain vocabulary + an interpreter for UI rendering.

The packages version independently: `emlang-v*` tags for Event Model releases, `xmlang-v*` for Experience Model.

## How They Work Together

```
Event Model (emlang)          Experience Model (xmlang)
├─ Commands                   ├─ Surfaces
├─ Events                     ├─ Personas
├─ Views (data, not layout)   ├─ Journeys
├─ State (decider folds)      ├─ Commands + confirm:/then: judgment
└─ Test scenarios             └─ Labels, design tokens

        ↓ Lintable references (one-way dependency)

Source Generator (app-owned)  Interpreter (request-time)
├─ Generates domain vocab     ├─ Reads xm spec
├─ Vocab lock-in: approval    ├─ Renders surfaces
│  tests at CI                ├─ Approval tests on output
└─ Deterministic: equality    └─ Conformance contract
   contract
```

The event model is the source of truth. The experience model records surface decisions, personas, and interaction judgments as data. A source generator turns event models into deterministic code; an interpreter turns experience models into rendered UX at request time. Both are validated by approval tests.

## Dialect Positions

The emlang dialect (defined in [emlang-dialect.md](emlang-dialect.md)) forks upstream emlang spec v1.0.0 with four positions on what a slice *is*. The xmlang dialect (defined in [xmlang-spec.md](xmlang-spec.md)) defines how experience decisions bind to event model slices:

| Position                         | Meaning in this dialect                                                                                                                                                                                                                                            | Reference                                                                                                                                                                                                               |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Vertical Slice Architecture**  | Every slice is a complete feature: initiator, command, events, view, tests. Nothing is layered across slices. Surfaces in xm map 1:1 to decision model slices.                                                                                                     | Jimmy Bogard, [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) (2018); Adam Dymitruk, [Event Modeling](https://eventmodeling.org/)                                               |
| **Decider**                      | A state-change slice is a pure `decide(command, state, context) → events ∪ error`. Tests give **state**, never event lists; every state has a fold test that produces it. The xm dialect adds interaction judgment (`confirm:`, `then:`) to commands.              | Jérémie Chassaing, [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) (2021)                                                                          |
| **Dynamic Consistency Boundary** | A state is a *decision model*, defined by its fold tests: the event types they fold are its query, its identity props are its tags. Surfaces compose views from this query. No aggregates, no stream-per-entity; consistency is an append condition on that query. | Sara Pellegrini, [Kill Aggregate!](https://sara.event-thinking.io/2023/04/kill-aggregate-chapter-1-I-am-here-to-kill-the-aggregate.html) (2023); Bastian Waidelich & Sara Pellegrini, [dcb.events](https://dcb.events/) |
| **Railway-Oriented Programming** | A state-change slice's outcome is a tagged union: `Event`(s) on the accepted (happy) path, or exactly one named `Rejection` on the declined path — never a thrown exception. The decider's `decide()` returns events ∪ rejection, not `void` with a `throw`. Surfaces in xm compose the user's journey through both paths: acceptance routes the user forward, each rejection shows its own label as a disable reason. UI and interpreter code MUST handle each named rejection explicitly; a generic catch-all handler is not a conforming consumer. | Scott Wlaschin, [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) (2013); `src/samples/RequestSample/Domain/Decider.cs` and its [README](src/samples/RequestSample/README.md) |

The intended domain is B2B SaaS and line-of-business software. Background on the method: Martin Dilger, [Understanding Eventsourcing](https://leanpub.com/eventmodeling-and-eventsourcing). The first three positions are supported by accepted RFCs in [rfcs/](rfcs/) (2026-09-14); Railway-Oriented Programming formalizes reasoning already implicit in RFC emlang-0002/0004's rejection-handling rules.

The RFCs accepting these positions live under [rfcs/](rfcs/) (accepted 2026-09-14).

## Install

The library (parser + linter, for build-time tools or runtime interpreters):

```
dotnet add package Xmlang
```

The CLI (a dotnet global tool named `xm`):

```
dotnet tool install -g Xmlang.Cli
xm lint specs/my-game.xm.yaml
```

`xm lint` resolves the document's `model:` Event Model(s) relative to the xm file, prints every finding, and exits non-zero on errors.

## Usage (library)

```csharp
using Emlang;
using Xmlang;

var xm = XmParser.Parse(File.ReadAllText("shop.xm.yaml"));
var em = EmParser.Parse(File.ReadAllText("shop.em.yaml"));
var findings = XmLinter.Lint(xm, em);
```

## Versioning

The Xmlang package minor version tracks the specification version (`Xmlang 0.6.x` implements spec v0.6); the patch component is free for implementation fixes. The Emlang packages carry their own SemVer (`emlang-v*` tags) and implement the decider dialect in [emlang-dialect.md](emlang-dialect.md), forked from upstream emlang spec v1.0.0.

## Provenance

Extracted from the proprietary kvissig.se codebase and relicensed under MIT by its copyright holder.

## License

[MIT](LICENSE)
