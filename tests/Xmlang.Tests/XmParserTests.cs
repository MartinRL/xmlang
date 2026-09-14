using AwesomeAssertions;
using Xunit;

namespace Xmlang.Tests;

public class XmParserTests
{
    [Fact]
    public void VersionKeyIsParsedAndAbsentIsNull()
    {
        XmParser.Parse("""xmlang: "0.2" """).Version.Should().Be("0.2");
        XmParser.Parse("personas:").Version.Should().BeNull();
    }

    [Fact]
    public void ModelScalarListAndAbsentForms()
    {
        XmParser.Parse("model: shop.yaml").Models.Should().Equal("shop.yaml");
        XmParser.Parse("""
            model:
              - shop.yaml
              - billing.yaml
            """).Models.Should().Equal("shop.yaml", "billing.yaml");
        XmParser.Parse("xmlang: \"0.2\"").Models.Should().BeEmpty();
    }

    [Fact]
    public void PersonasParseWithAndWithoutRole()
    {
        var xm = XmParser.Parse("""
            personas:
              Owner:
                role: owner
              Guest:
            """);
        xm.Personas.Should().Equal(new XmPersona("Owner", "owner"), new XmPersona("Guest", null));
    }

    [Fact]
    public void SurfaceForAndDuringDefaultToEmpty()
    {
        var surface = XmParser.Parse("""
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
            """).Surfaces.Single();
        surface.For.Should().BeEmpty();
        surface.During.IsAll().Should().BeTrue();
    }

    [Fact]
    public void ViewItemParsesSalienceTiersAndMissingFieldsMeansAllEmpty()
    {
        var items = XmParser.Parse("""
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                    fields:
                      primary: [total]
                      secondary: [note]
                      on-demand: [items]
                  - v: CashPosition
            """).Surfaces.Single().Compose;
        items[0].View!.Primary.Should().Equal("total");
        items[0].View!.Secondary.Should().Equal("note");
        items[0].View!.OnDemand.Should().Equal("items");
        items[1].View!.Primary.Should().BeEmpty();
        items[1].View!.Secondary.Should().BeEmpty();
        items[1].View!.OnDemand.Should().BeEmpty();
    }

    [Fact]
    public void CommandProminenceDefaultsToPrimary()
    {
        var items = XmParser.Parse("""
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                  - c: AddItem
                  - c: OpenShop
                    prominence: overflow
            """).Surfaces.Single().Compose;
        items[1].Command.Should().Be(new XmCommandItem("AddItem", "primary"));
        items[2].Command.Should().Be(new XmCommandItem("OpenShop", "overflow"));
    }

    [Fact]
    public void SelfIsKeptVerbatim()
    {
        XmParser.Parse("""
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                    self: items.itemId
            """).Surfaces.Single().Compose[0].View!.Self.Should().Be("items.itemId");
    }

    [Fact]
    public void QuotedElementNamesSurviveVerbatim()
    {
        XmParser.Parse("""
            surfaces:
              Dashboard:
                compose:
                  - v: "Todo / Outstanding bids"
            """).Surfaces.Single().Compose[0].View!.Name.Should().Be("Todo / Outstanding bids");
    }

    [Fact]
    public void JourneysParseNameForAndSlices()
    {
        var journey = XmParser.Parse("""
            journeys:
              FirstSale:
                for: [Owner]
                slices:
                  - "🧑 OpenShop"
                  - "🛒 BrowseShop"
            """).Journeys.Single();
        journey.Name.Should().Be("FirstSale");
        journey.For.Should().Equal("Owner");
        journey.Slices.Should().Equal("🧑 OpenShop", "🛒 BrowseShop");
    }

    [Fact]
    public void LabelsParseRegisterStringFormSelfEmptyAndNestedFields()
    {
        var map = XmParser.Parse("""
            labels:
              sv:
                register: lekfull
                AddItem: Lägg i korgen!
                Storefront:
                  total: Summa
                  items:
                    $self: Varor
                    $empty: Korgen är tom
            """).Labels["sv"];
        map.Register.Should().Be("lekfull");
        map.Elements["AddItem"].Self.Should().Be("Lägg i korgen!");
        map.Elements.Should().NotContainKey("register");
        var storefront = map.Elements["Storefront"];
        storefront.Fields["total"].Self.Should().Be("Summa");
        storefront.Fields["items"].Self.Should().Be("Varor");
        storefront.Fields["items"].Empty.Should().Be("Korgen är tom");
    }

    [Fact]
    public void TokensFlattenToDottedPathsAndListsJoin()
    {
        var tokens = XmParser.Parse("""
            tokens:
              color:
                brand:
                  $type: color
                  $value: "#0B5FFF"
              fontStack:
                $value: [Inter, system-ui]
            """).Tokens;
        tokens.Should().Equal(
            new XmToken("color.brand", "#0B5FFF"),
            new XmToken("fontStack", "Inter, system-ui"));
    }

    [Fact]
    public void ComposeOrderIsPreserved()
    {
        var items = XmParser.Parse("""
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                  - c: AddItem
                  - v: "State / Shop"
            """).Surfaces.Single().Compose;
        items.Select(i => i.View?.Name ?? i.Command!.Name)
            .Should().Equal("Storefront", "AddItem", "State / Shop");
    }
}
