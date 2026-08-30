using AwesomeAssertions;
using Emlang.CodeGen;
using Xunit;

namespace Emlang.Tests;

/// <summary>The general annotation-to-C# mapping rules, checked one by one.</summary>
public class SpecModelTests
{
    [Theory]
    [InlineData("string", "string")]
    [InlineData("Guid", "Guid")]
    [InlineData("DateTimeOffset", "DateTimeOffset")]
    [InlineData("int?", "int?")]
    [InlineData("Direction (mer|mindre)", "Direction")]
    [InlineData("byte (0-100)", "byte")]
    [InlineData("Lot[]", "IReadOnlyList<Lot>")]
    [InlineData("Guid[]", "IReadOnlyList<Guid>")]
    public void Annotation_maps_to_the_committed_type(string annotation, string expected) =>
        SpecModel.MapType(annotation).Should().Be(expected);

    [Fact]
    public void Prop_names_are_pascal_cased() =>
        SpecModel.PascalCase("guessedDifference").Should().Be("GuessedDifference");

    [Fact]
    public void Events_strip_the_stream_prefix_and_track_lines()
    {
        var elements = SpecModel.Parse("""
            slices:
              Slice:
                - c: DoIt
                - e: Game / ItDone
                  props:
                    gameId: Guid
            """);

        elements.Should().BeEquivalentTo(new[]
        {
            new SpecElement('c', "DoIt", [], 3),
            new SpecElement('e', "ItDone", [new SpecProp("GameId", "Guid", 6)], 4, "Game"),
        });
    }

    [Fact]
    public void The_props_richest_occurrence_defines_the_surface()
    {
        var elements = SpecModel.Parse("""
            slices:
              Producer:
                - c: DoIt
                  props:
                    gameId: Guid
              Consumer:
                - e: Game / Later
                - c: DoIt
            """);

        elements.Single(e => e.Kind == 'c').Props.Should().ContainSingle()
            .Which.Name.Should().Be("GameId");
    }
}
