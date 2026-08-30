using AwesomeAssertions;
using Emlang.CodeGen;
using Xunit;

namespace Emlang.Tests;

/// <summary>
/// Negative proof for the comparer: inline YAML + inline C# only — the fixture specs are
/// NEVER touched here. Each case shows the comparer actually fails on a fabricated
/// divergence, in both directions.
/// </summary>
public class SurfaceComparerTests
{
    private static readonly EmitTarget Fake = new("Fake.Domain", "Fake", "unused.yaml");

    private const string Spec = """
        slices:
          Open Thing:
            steps:
              - c: OpenThing
                props:
                  hostName: string
                  packId: string
              - x: ThingNotFound
              - e: Game / ThingOpened
                props:
                  gameId: Guid
                  lots: Lot[]
                  openedAt: DateTimeOffset
            tests:
              a test case whose props are fixture values, not types:
                given: []
                when:
                  - c: OpenThing
                    props: { hostName: Martin, packId: fake }
                then:
                  - e: Game / ThingOpened
                    props: { gameId: gameId }
        """;

    private const string Commands = """
        namespace Fake.Domain;
        public record OpenThing(string HostName, string PackId);
        public union FakeCommand(OpenThing);
        """;

    private const string Events = """
        namespace Fake.Domain;
        public record ThingOpened(Guid GameId, IReadOnlyList<Lot> Lots, DateTimeOffset OpenedAt);
        public union FakeEvent(ThingOpened);
        """;

    private const string Errors = """
        namespace Fake.Domain;
        public record ThingNotFound;
        public union FakeError(ThingNotFound);
        """;

    private static IReadOnlyList<Divergence> Compare(
        string spec = Spec, string commands = Commands, string events = Events, string errors = Errors) =>
        SurfaceComparer.Compare(Fake, spec, commands, events, errors);

    [Fact]
    public void Matching_surfaces_produce_no_divergences() =>
        Compare().Should().BeEmpty();

    [Fact]
    public void Test_props_are_fixture_values_and_never_shape_the_surface() =>
        // The tests: block above uses value-props (hostName: Martin) — a naive parser
        // would report prop-type mismatches; the surface must come from steps only.
        Compare().Should().BeEmpty();

    [Fact]
    public void A_fake_spec_prop_missing_in_code_is_reported()
    {
        var spec = Spec.Replace("hostName: string", "hostName: string\n          fakeProp: int");
        Compare(spec: spec).Should().ContainSingle(d => d.Code == "missing-prop:FakeProp");
    }

    [Fact]
    public void An_extra_code_param_missing_in_spec_is_reported()
    {
        var commands = Commands.Replace("string PackId)", "string PackId, int Sneaky)");
        Compare(commands: commands).Should().ContainSingle(d => d.Code == "extra-param:Sneaky");
    }

    [Fact]
    public void A_wrong_param_type_is_reported()
    {
        var commands = Commands.Replace("string PackId", "Guid PackId");
        var divergence = Compare(commands: commands).Should().ContainSingle().Subject;
        divergence.Code.Should().Be("prop-type:PackId");
        divergence.Detail.Should().Contain("spec says string").And.Contain("code says Guid");
    }

    [Fact]
    public void A_spec_element_without_a_record_is_reported()
    {
        var errors = Errors.Replace("public record ThingNotFound;", "");
        Compare(errors: errors).Should().Contain(d => d.Code == "missing-record" && d.Element == "ThingNotFound");
    }

    [Fact]
    public void A_record_without_a_spec_element_is_reported()
    {
        var errors = Errors.Replace("public record ThingNotFound;",
            "public record ThingNotFound;\npublic record Orphan;");
        Compare(errors: errors).Should().ContainSingle(d => d.Code == "unspecced-record" && d.Element == "Orphan");
    }

    [Fact]
    public void Swapped_param_order_is_reported()
    {
        var commands = Commands.Replace("string HostName, string PackId", "string PackId, string HostName");
        Compare(commands: commands).Should().ContainSingle(d => d.Code == "prop-order");
    }

    [Fact]
    public void A_union_case_missing_from_the_spec_is_reported()
    {
        var errors = Errors.Replace("public record ThingNotFound;",
            "public record ThingNotFound;\npublic record Rogue;")
            .Replace("FakeError(ThingNotFound)", "FakeError(ThingNotFound, Rogue)");
        Compare(errors: errors).Should().Contain(d => d.Code == "union-extra" && d.Element == "Rogue");
    }

    [Fact]
    public void A_wrong_namespace_is_reported()
    {
        var errors = Errors.Replace("namespace Fake.Domain;", "namespace Wrong.Domain;");
        Compare(errors: errors).Should().ContainSingle(d => d.Code == "wrong-namespace" && d.Element == "Wrong.Domain");
    }

    [Fact]
    public void A_spec_element_missing_from_the_union_is_reported()
    {
        var errors = Errors.Replace("FakeError(ThingNotFound)", "FakeError()");
        Compare(errors: errors).Should().Contain(d => d.Code == "union-missing" && d.Element == "ThingNotFound");
    }
}
