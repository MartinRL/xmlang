using System;
using System.IO;
using Emlang.CodeGen;
using Microsoft.CodeAnalysis;

namespace Emlang.Generators;

/// <summary>
/// Shared plumbing for both generators. A spec participates by carrying EmlangPrefix
/// metadata on its AdditionalFiles item (buildTransitive props makes it compiler-visible);
/// the namespace is the per-file EmlangNamespace override or the project's RootNamespace.
/// Mode is the project-level EmlangEmit gate (absent = "surface"; tests projects set
/// "tests" and get SpecTests instead of records).
/// </summary>
internal static class EmlangEmit
{
    internal static IncrementalValuesProvider<(string Yaml, string Mode, EmitTarget Target)> Specs(
        IncrementalGeneratorInitializationContext context) =>
        context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".em.yaml", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, ct) =>
            {
                var (text, provider) = pair;
                var options = provider.GetOptions(text);
                if (!options.TryGetValue("build_metadata.AdditionalFiles.EmlangPrefix", out var prefix)
                    || prefix.Length == 0)
                    return (Yaml: string.Empty, Mode: string.Empty, Target: (EmitTarget?)null);

                if (!options.TryGetValue("build_metadata.AdditionalFiles.EmlangNamespace", out var ns)
                    || ns.Length == 0)
                    provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out ns);

                var mode = provider.GlobalOptions.TryGetValue("build_property.EmlangEmit", out var emit)
                    && emit.Length > 0 ? emit : "surface";

                return (Yaml: text.GetText(ct)?.ToString() ?? string.Empty, Mode: mode,
                    Target: (EmitTarget?)new EmitTarget(ns ?? string.Empty, prefix, Path.GetFileName(text.Path)));
            })
            .Where(static spec => spec.Target is not null)
            .Select(static (spec, _) => (spec.Yaml, spec.Mode, spec.Target!));
}

/// <summary>
/// Thin analyzer wrapper over Emlang.CodeGen: each participating spec gets its stratum-1
/// Commands/Events/Errors plus the Decider Evolve/Decide switch skeletons emitted straight
/// into the compilation (obj/, never disk). Correctness lives in SurfaceEmitterTests'
/// comparer round-trip and DeciderEmitterTests — this class only routes text.
/// </summary>
[Generator]
public sealed class EmlangRecordsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(EmlangEmit.Specs(context), static (production, spec) =>
        {
            if (spec.Mode != "surface")
                return;
            var elements = SpecModel.Parse(spec.Yaml);
            production.AddSource("Commands.g.cs", SurfaceEmitter.Emit(spec.Target, elements, 'c'));
            production.AddSource("Events.g.cs", SurfaceEmitter.Emit(spec.Target, elements, 'e'));
            production.AddSource("Errors.g.cs", SurfaceEmitter.Emit(spec.Target, elements, 'x'));
            production.AddSource("Decider.g.cs", DeciderEmitter.Emit(spec.Target, elements));
        });
    }
}
