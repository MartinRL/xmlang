using AwesomeAssertions;
using Xunit;

namespace Xmlang.Tests;

public class XmLinterTests
{
    private static IReadOnlyList<XmFinding> Lint(string xmYaml) =>
        XmLinter.Lint(XmParser.Parse(xmYaml), Fixtures.ParsedEm);

    private static IReadOnlyList<string> Rules(string xmYaml) =>
        [.. Lint(xmYaml).Select(f => f.Rule)];

    [Fact]
    public void CleanDocumentYieldsNoFindings()
    {
        Lint("""
            xmlang: "0.2"
            model: shop-event-model.yaml
            personas:
              Owner:
                role: owner
              Customer:
                role: customer
            surfaces:
              Dashboard:
                during: [open]
                compose:
                  - v: Storefront
                    self: items.itemId
                    fields:
                      primary: [total]
                      secondary: [note]
                      on-demand: [items]
              Setup:
                during: [closed]
                for: [Owner]
                compose:
                  - v: State / Shop
                  - c: OpenShop
            journeys:
              FirstSale:
                for: [Customer]
                slices:
                  - "🛒 BrowseShop"
            labels:
              sv:
                register: lekfull
                OpenShop: Öppna butiken!
                Dashboard: Butiken
                Storefront:
                  total: Summa
            """).Should().BeEmpty();
    }

    [Fact]
    public void MissingVersionKeyWarns()
    {
        Lint("surfaces:").Should().ContainSingle(f =>
            f.Rule == "xm-version-missing" && f.Severity == XmSeverity.Warning);
        Lint("""xmlang: "0.2" """).Should().BeEmpty();
    }

    [Fact]
    public void PersonaRoleMatchingNoSwimlaneWarns()
    {
        Lint("""
            xmlang: "0.2"
            personas:
              Ghost:
                role: ghost
            """).Should().ContainSingle(f =>
            f.Rule == "xm-unknown-role" && f.Severity == XmSeverity.Warning);
    }

    [Fact]
    public void SurfaceForUndeclaredPersonaIsAnError()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                for: [Nobody]
                compose:
                  - v: Storefront
            """).Should().Contain("xm-unknown-persona");
    }

    [Fact]
    public void JourneyForUndeclaredPersonaIsAnError()
    {
        Rules("""
            xmlang: "0.2"
            journeys:
              FirstSale:
                for: [Nobody]
                slices:
                  - "🛒 BrowseShop"
            """).Should().Contain("xm-unknown-persona");
    }

    [Fact]
    public void UndeclaredPhaseValueIsAnError()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                during: [banana, open, closed]
                compose:
                  - v: Storefront
            """).Should().Contain("xm-unknown-phase").And.NotContain("xm-phase-uncovered");
    }

    [Fact]
    public void SurfaceComposingNoViewIsAnError()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Actions:
                compose:
                  - c: AddItem
            """).Should().Contain("xm-surface-without-view");
    }

    [Fact]
    public void UnknownViewReferenceIsDangling()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                compose:
                  - v: Ghost
            """).Should().Contain("xm-dangling-ref");
    }

    [Fact]
    public void UnknownCommandReferenceIsDangling()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                  - c: Ghost
            """).Should().Contain("xm-dangling-ref");
    }

    [Fact]
    public void UnknownFieldInASalienceTierIsDangling()
    {
        Lint("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                    fields:
                      secondary: [ghost]
            """).Should().ContainSingle(f =>
            f.Rule == "xm-dangling-ref" && f.Message.Contains("ghost"));
    }

    [Fact]
    public void UnknownSliceInAJourneyIsDangling()
    {
        Rules("""
            xmlang: "0.2"
            journeys:
              FirstSale:
                slices: [GhostSlice]
            """).Should().Contain("xm-dangling-ref");
    }

    [Fact]
    public void SelfWithUnresolvableFirstSegmentIsAnError()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                    self: ghost.itemId
            """).Should().Contain("xm-self-field-missing");
    }

    [Fact]
    public void DottedSelfResolvingOnItsFirstSegmentIsFine()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
                    self: items.itemId
            """).Should().NotContain("xm-self-field-missing");
    }

    [Fact]
    public void SurfaceSharingABareViewNameWarns()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Storefront:
                compose:
                  - v: Storefront
            """).Should().Contain("xm-surface-shadows-view");
    }

    [Fact]
    public void SurfaceSharingALaneQualifiedViewNameWarns()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              State / Shop:
                compose:
                  - v: Storefront
            """).Should().Contain("xm-surface-shadows-view");
    }

    [Fact]
    public void ScreenLaneViewIsAnInfoFinding()
    {
        Lint("""
            xmlang: "0.2"
            surfaces:
              Results:
                compose:
                  - v: Screen / Results
            """).Should().ContainSingle(f =>
            f.Rule == "xm-screen-lane-view" && f.Severity == XmSeverity.Info);
    }

    [Fact]
    public void UnclaimedPhaseWarnsOnlyWhenEverySurfaceIsPhased()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                during: [open]
                compose:
                  - v: Storefront
            """).Should().Contain("xm-phase-uncovered");
    }

    [Fact]
    public void ADuringLessSurfaceCoversEveryPhase()
    {
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                during: [open]
                compose:
                  - v: Storefront
              Everywhere:
                compose:
                  - v: State / Shop
            """).Should().NotContain("xm-phase-uncovered");
    }

    [Fact]
    public void ZeroSurfacesSkipsPhaseCoverage()
    {
        Rules("""xmlang: "0.2" """).Should().NotContain("xm-phase-uncovered");
    }

    [Fact]
    public void LabelResolvingToNoElementIsOrphan()
    {
        Rules("""
            xmlang: "0.2"
            labels:
              sv:
                Ghost: Spöke
            """).Should().Contain("xm-orphan-label");
    }

    [Fact]
    public void FieldLabelResolvingToNoFieldIsOrphan()
    {
        Lint("""
            xmlang: "0.2"
            labels:
              sv:
                Storefront:
                  ghost: Spöke
            """).Should().ContainSingle(f =>
            f.Rule == "xm-orphan-label" && f.Message.Contains("ghost"));
    }

    [Fact]
    public void FieldLabelUnderAFieldlessElementIsOrphan()
    {
        // A surface is a known but fieldless element: its own label is fine,
        // a field key under it is not.
        Rules("""
            xmlang: "0.2"
            surfaces:
              Dashboard:
                compose:
                  - v: Storefront
            labels:
              sv:
                Dashboard:
                  ghost: Spöke
            """).Should().Contain("xm-orphan-label");
    }

    [Fact]
    public void DuplicateFindingsCollapse()
    {
        // xm-screen-lane-view carries no surface name, so composing the same
        // Screen-lane view on two surfaces yields one collapsed finding.
        Lint("""
            xmlang: "0.2"
            surfaces:
              ResultsA:
                compose:
                  - v: Screen / Results
              ResultsB:
                compose:
                  - v: Screen / Results
            """).Where(f => f.Rule == "xm-screen-lane-view").Should().ContainSingle();
    }
}
