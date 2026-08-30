using AwesomeAssertions;
using Emlang.CodeGen;
using Xunit;

namespace Emlang.Tests;

/// <summary>TestModel parses the `tests:` GWT/GT cases — checked against a minimal inline
/// spec for structure and against the FROZEN tank fixture for the full census.</summary>
public class TestModelTests
{
    private static IReadOnlyList<SpecTest> TankTests() =>
        TestModel.Parse(Fixture.Spec(Fixture.TankTillTusen));

    [Fact]
    public void Steps_carry_kind_lane_bare_name_and_pascal_cased_props()
    {
        var tests = TestModel.Parse("""
            slices:
              Slice:
                steps:
                  - c: DoIt
                tests:
                  it works:
                    given:
                      - v: State / Game
                        props: { phase: lobby }
                    when:
                      - c: DoIt
                        props: { gameId: gameId, lots: [a, b] }
                    then:
                      - e: Game / ItDone
            """);

        var test = tests.Single();
        (test.Slice, test.Name).Should().Be(("Slice", "it works"));
        var given = test.Given.Single();
        (given.Kind, given.Lane, given.Name).Should().Be(('v', "State", "Game"));
        given.Props.Single().Name.Should().Be("Phase");
        given.Props.Single().Value.Should().Be(new ScalarValue("lobby"));
        test.When!.Name.Should().Be("DoIt");
        test.When.Props[1].Value.Should().BeEquivalentTo(
            new ListValue([new ScalarValue("a"), new ScalarValue("b")]));
        (test.Then.Single().Kind, test.Then.Single().Lane).Should().Be(('e', "Game"));
    }

    [Fact]
    public void Tank_fixture_parses_the_full_census_of_40_tests()
    {
        var tests = TankTests();

        tests.Should().HaveCount(40);
        tests.Count(t => t.When is not null).Should().Be(28, "GWT decide cases");
        tests.Count(t => t.When is null && t.Then is [{ Kind: 'v', Lane: "State" }])
            .Should().Be(2, "GT fold cases");
        tests.Count(t => t.When is null && t.Then is [{ Kind: 'v', Lane: "" or "Todo" }])
            .Should().Be(10, "GT projection cases");
    }

    [Fact]
    public void Minted_and_null_pins_survive_as_raw_scalar_text()
    {
        var tests = TankTests();

        tests.Single(t => t.Name == "game can be created")
            .Then.Single().Props.Single(p => p.Name == "HostPlayerId")
            .Value.Should().Be(new ScalarValue("minted"));
        tests.Single(t => t.Name == "a non-submitter scores the worst once the deadline passes")
            .Then.Last().Props.Single(p => p.Name == "ReachedValue")
            .Value.Should().Be(new ScalarValue("null"));
    }

    [Fact]
    public void No_tank_test_value_is_an_inline_map()
    {
        static IEnumerable<TestValue> Flatten(TestValue value) =>
            value is ListValue list ? list.Items.SelectMany(Flatten).Prepend(value) : [value];

        TankTests()
            .SelectMany(t => t.Given.Concat(t.Then).Concat(t.When is { } w ? [w] : Array.Empty<TestStep>()))
            .SelectMany(s => s.Props)
            .SelectMany(p => Flatten(p.Value))
            .Should().NotContain(v => v is MapValue, "tests reference fixtures, never build maps inline");
    }
}
