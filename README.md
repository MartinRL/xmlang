# xmlang

xmlang is a YAML-based DSL for **Experience Models**: the sibling dialect to emlang (Event Modeling as YAML), recording the UX judgments an Event Model deliberately omits — personas, surface composition, field salience, journeys, labels, design tokens — **as data, never as geometry**. The Experience Model depends on the Event Model strictly one-way, and every reference is lintable.

The canonical specification lives here: [xmlang-spec.md](xmlang-spec.md). This repository is also the reference implementation: a parser, an Event Model resolution surface, and a linter implementing the spec's full rule set.

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
using Xmlang;

var xm = XmParser.Parse(File.ReadAllText("shop.xm.yaml"));
var em = EmParser.Parse(File.ReadAllText("shop.em.yaml"));
var findings = XmLinter.Lint(xm, em);
```

## Versioning

The package minor version tracks the specification version (`Xmlang 0.4.x` implements spec v0.4); the patch component is free for implementation fixes.

## Provenance

Extracted from the proprietary kvissig.se codebase and relicensed under MIT by its copyright holder.

## License

[MIT](LICENSE)
