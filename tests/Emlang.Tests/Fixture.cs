using Emlang.CodeGen;

namespace Emlang.Tests;

/// <summary>
/// The three kvissig game specs, FROZEN as fixtures: they update deliberately, never
/// automatically, when the source repo's specs change. The EmitTargets mirror what the
/// consuming projects declare as AdditionalFiles metadata (EmlangPrefix + RootNamespace).
/// </summary>
internal static class Fixture
{
    internal static readonly EmitTarget MerEllerMindre =
        new("MerEllerMindre.Domain", "Game", "mer-eller-mindre.em.yaml");

    internal static readonly EmitTarget Blindbudet =
        new("Blindbudet.Domain", "Auction", "blindbudet.em.yaml");

    internal static readonly EmitTarget TankTillTusen =
        new("TankTillTusen.Domain", "Tank", "tank-till-tusen.em.yaml");

    internal static string Spec(EmitTarget target) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", target.SpecFile));
}
