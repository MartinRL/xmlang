using Emlang.CodeGen;
using Microsoft.CodeAnalysis;

namespace Emlang.Generators;

/// <summary>
/// The tests stratum: in projects with EmlangEmit=tests, the spec's `tests:` GWT/GT cases
/// become SpecTests.g.cs in the project's own RootNamespace (enclosing-namespace lookup
/// resolves the domain types). TestsEmitter never throws — unmappable cases become failing
/// generated tests, so the generator only routes text.
/// </summary>
[Generator]
public sealed class EmlangTestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(EmlangEmit.Specs(context), static (production, spec) =>
        {
            if (spec.Mode != "tests")
                return;
            production.AddSource(
                "SpecTests.g.cs",
                TestsEmitter.Emit(spec.Target, SpecModel.Parse(spec.Yaml), TestModel.Parse(spec.Yaml)));
        });
    }
}
