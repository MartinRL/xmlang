using AwesomeAssertions;
using Xunit;

namespace Xmlang.Tests;

public class EmModelTests
{
    [Fact]
    public void ShortAndLongElementKeysCollectAlike()
    {
        var em = EmModel.Parse("""
            slices:
              Short:
                - c: DoThing
                - e: Log / ThingDone
                - x: ThingBroken
                - v: Things
              Long:
                - command: DoOther
                - event: Log / OtherDone
                - exception: OtherBroken
                - view: Others
            """);
        em.Elements.Select(e => (e.Kind, e.Name)).Should().BeEquivalentTo(new[]
        {
            ('c', "DoThing"), ('e', "ThingDone"), ('x', "ThingBroken"), ('v', "Things"),
            ('c', "DoOther"), ('e', "OtherDone"), ('x', "OtherBroken"), ('v', "Others"),
        });
    }

    [Fact]
    public void LaneIsThePrefixBeforeTheLastSlash()
    {
        var em = EmModel.Parse("""
            slices:
              S:
                - e: Shop / Sub / ItemAdded
            """);
        var element = em.Elements.Single();
        element.Lane.Should().Be("Shop / Sub");
        element.Name.Should().Be("ItemAdded");
    }

    [Fact]
    public void PropsRichestOccurrenceDefinesTheElement()
    {
        var em = EmModel.Parse("""
            slices:
              A:
                - e: Shop / ItemAdded
                  props:
                    itemId: Guid
                    quantity: int
              B:
                - e: Shop / ItemAdded
            """);
        em.Elements.Single().Fields.Should().Equal(
            new EmField("itemId", "Guid"), new EmField("quantity", "int"));
    }

    [Fact]
    public void TriggerRolesAreDistinctInitiatorLanePrefixes()
    {
        Fixtures.ParsedEm.TriggerRoles.Should().BeEquivalentTo("owner", "customer");
    }

    [Fact]
    public void SliceKeysAreKeptVerbatim()
    {
        Fixtures.ParsedEm.Slices.Should().Equal("🧑 OpenShop", "🛒 BrowseShop");
    }

    [Fact]
    public void ExtendedSliceFormWithStepsKeyIsCollected()
    {
        var em = EmModel.Parse("""
            slices:
              Extended:
                notes: some prose
                steps:
                  - c: DoThing
                  - e: Log / ThingDone
            """);
        em.Elements.Select(e => e.Name).Should().BeEquivalentTo("DoThing", "ThingDone");
    }

    [Fact]
    public void PhaseValuesComeFromTheStateLanePhaseEnumAnnotation()
    {
        Fixtures.ParsedEm.PhaseValues.Should().Equal("closed", "open");
    }

    [Fact]
    public void MergeConcatenatesElementsAndDistinctsRolesAndPhases()
    {
        var merged = EmModel.Merge([Fixtures.ParsedEm, Fixtures.ParsedEm]);
        merged.Elements.Should().HaveCount(Fixtures.ParsedEm.Elements.Count * 2);
        merged.Slices.Should().HaveCount(4);
        merged.TriggerRoles.Should().BeEquivalentTo("owner", "customer");
        merged.PhaseValues.Should().Equal("closed", "open");
    }

    [Fact]
    public void FindViewAndFindCommandNormalizeReferences()
    {
        var em = Fixtures.ParsedEm;
        em.FindView("Storefront")!.Name.Should().Be("Storefront");
        em.FindView("State / Shop")!.Lane.Should().Be("State");
        em.FindView("State /  Shop ")!.Name.Should().Be("Shop");
        em.FindView("Todo / Outstanding bids")!.Fields.Should().Equal(new EmField("bids", "Bid[]"));
        em.FindView("Ghost").Should().BeNull();
        em.FindCommand("OpenShop")!.Kind.Should().Be('c');
        em.FindCommand("Ghost").Should().BeNull();
    }

    [Fact]
    public void ParseWithoutSlicesRootThrows()
    {
        // Pinned behavior: an emlang doc without a `slices:` root is not parseable.
        var act = () => EmModel.Parse("foo: 1");
        act.Should().Throw<KeyNotFoundException>();
    }
}
