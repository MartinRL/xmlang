using AwesomeAssertions;
using Emlang;
using Xunit;

namespace Xmlang.Tests;

public class EmParserTests
{
    [Fact]
    public void ShortAndLongElementKeysCollectAlike()
    {
        var em = EmParser.Parse("""
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
    public void LaneIsThePrefixBeforeTheFirstSlash()
    {
        // RFC emlang-0003: the swimlane is the text before the first '/', as in the Go reference.
        var em = EmParser.Parse("""
            slices:
              S:
                - e: Shop / Sub / ItemAdded
            """);
        var element = em.Elements.Single();
        element.Lane.Should().Be("Shop");
        element.Name.Should().Be("Sub / ItemAdded");
    }

    [Fact]
    public void PropsRichestOccurrenceDefinesTheElement()
    {
        var em = EmParser.Parse("""
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
        var em = EmParser.Parse("""
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
    public void PhaseValuesComeFromTheStateElementsPhaseEnumAnnotationPerDecisionModel()
    {
        Fixtures.ParsedEm.Phases.Should().ContainKey("Shop").WhoseValue.Should().Equal("closed", "open");
        Fixtures.ParsedEm.PhaseValues.Should().Equal("closed", "open");
    }

    [Fact]
    public void InitiatorsCarryRoleOriginAndKind()
    {
        var em = EmParser.Parse("""
            slices:
              Pay:
                - a: 🧾 Clerk /Invoice list
                - t: ⚙️ System / Payment run
                - c: Pay
                - e: Invoice / Paid
            """);
        em.Initiators.Should().Equal(
            new EmInitiator("Pay", "🧾 Clerk", "Invoice list", false),
            new EmInitiator("Pay", "⚙️ System", "Payment run", true));
        em.TriggerRoles.Should().Equal("clerk", "system");
        em.InitiatorsOf("Pay").Should().HaveCount(2);
        em.Chains.Single().TerminalView.Should().BeNull();
    }

    [Fact]
    public void ScenariosReadTheDecisionTests()
    {
        var em = EmParser.Parse("""
            slices:
              Start:
                steps:
                  - c: Start
                  - x: NotOpen
                  - e: Game / Started
                tests:
                  starts:
                    given:
                      - s: Game
                        props: { phase: lobby }
                    when:
                      - c: Start
                    then:
                      - e: Game / Started
                  rejects:
                    given:
                      - s: Game
                    when:
                      - c: Start
                    then:
                      - x: NotOpen
            """);
        em.Scenarios.Should().Equal(
            new EmScenario("Start", "Start", "Game", "lobby", true),
            new EmScenario("Start", "Start", "Game", null, false));
    }

    [Fact]
    public void MergeConcatenatesElementsAndDistinctsRolesAndPhases()
    {
        var merged = EmParser.Merge([Fixtures.ParsedEm, Fixtures.ParsedEm]);
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
        em.FindState("Shop")!.Kind.Should().Be('s');
        em.FindState("State /  Shop ")!.Name.Should().Be("Shop");
        em.FindView("Todo / Outstanding bids")!.Fields.Should().Equal(new EmField("bids", "Bid[]"));
        em.FindView("Ghost").Should().BeNull();
        em.FindCommand("OpenShop")!.Kind.Should().Be('c');
        em.FindCommand("Ghost").Should().BeNull();
    }

    [Fact]
    public void ParseWithoutSlicesRootThrows()
    {
        // Pinned behavior: an emlang doc without a `slices:` root is not parseable.
        var act = () => EmParser.Parse("foo: 1");
        act.Should().Throw<KeyNotFoundException>();
    }
}
