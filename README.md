# xmlang

xmlang is a YAML-based DSL for **Experience Models**: the sibling dialect to emlang (Event Models as YAML), recording the UX judgments an Event Model deliberately omits — personas, surface composition, field salience, journeys, labels, design tokens — **as data, never as geometry**. The Experience Model depends on the Event Model strictly one-way, and every reference is lintable.

The canonical specification lives here: [xmlang-spec.md](xmlang-spec.md). This repository is also the reference implementation: a parser, an Event Model resolution surface, and a linter implementing the spec's full rule set.

The repository also hosts the .NET implementation of [emlang](https://github.com/emlang-project/emlang) itself — xmlang cannot live without it:

- **Emlang** — the em parser (`EmParser`), the codegen model and emitters (`Emlang.CodeGen`), and the line-aware lint surface (`Emlang.Linting`).
- **Emlang.Cli** — `em`, the .NET clone of the reference Go CLI (`dotnet tool install -g Emlang.Cli`). Phase 1 commands: `em parse` and `em lint` with the reference toolchain's rule set and output format, plus `version`/`help`.
- **Emlang.Generators** — Roslyn source generators: point an `AdditionalFiles` item at a `*.em.yaml` spec with `EmlangPrefix` metadata and the Commands/Events/Errors records, closed unions and Decider switch skeletons are emitted into the compilation; projects with `EmlangEmit=tests` get xUnit spec tests instead. The emitted unions require `LangVersion` preview (C# `union` types) in the consuming project.

The emlang packages version and release independently (tags `emlang-v*`) from the xmlang packages (tags `xmlang-v*`).

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

The Xmlang package minor version tracks the specification version (`Xmlang 0.5.x` implements spec v0.5); the patch component is free for implementation fixes. The Emlang packages carry their own SemVer and implement the emlang spec v1.0.0.

## Provenance

Extracted from the proprietary kvissig.se codebase and relicensed under MIT by its copyright holder.

## License

[MIT](LICENSE)
