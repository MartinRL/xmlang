using System.Text;
using Emlang;
using Xunit;

namespace Xmlang.Tests;

/// <summary>Whole-AST and CLI-output snapshots: the shapes and the rendered lint
/// report are pinned, so any parser/linter default change fails with a line diff.</summary>
public class ApprovalTests
{
    /// <summary>A rich xm document over Fixtures.Em exercising every section; lints clean.</summary>
    private const string Xm = """
        xmlang: "0.2"
        model: shop.emlang.yaml

        personas:
          Owner:
            role: owner
          Customer:
            role: customer

        surfaces:
          Shopfront:
            for: [Customer]
            during: [open]
            compose:
              - v: Storefront
                self: items.itemId
                fields:
                  primary: [items, total]
                  secondary: [note]
              - c: AddItem
                prominence: primary
          BackOffice:
            for: [Owner]
            compose:
              - v: State / Shop
              - c: OpenShop
                prominence: overflow

        journeys:
          FirstPurchase:
            for: [Customer]
            slices:
              - "🧑 OpenShop"
              - "🛒 BrowseShop"

        labels:
          sv:
            register: informell
            AddItem: Lägg till
            Storefront:
              total: Summa
              items:
                $self: Varor
                $empty: Tomt!
          en:
            AddItem: Add item

        tokens:
          color:
            brand:
              $type: color
              $value: "#0B5FFF"
        """;

    /// <summary>Deliberately violates many lint rules at once.</summary>
    private const string DirtyXm = """
        personas:
          Clerk:
            role: cashier

        surfaces:
          Storefront:
            for: [Ghost]
            during: [open]
            compose:
              - v: Storefront
                self: missing
                fields:
                  primary: [nope]
              - v: Screen / Results
          Checkout:
            during: [paused]
            compose:
              - c: Checkout

        journeys:
          Buy:
            slices:
              - "🛒 BrowseShop"
              - MissingSlice

        labels:
          en:
            Ghosty: Boo
        """;

    [Fact]
    public Task EmSpecShape() => Verifier.Verify(Fixtures.ParsedEm);

    [Fact]
    public Task XmSpecShape() => Verifier.Verify(XmParser.Parse(Xm));

    [Fact]
    public Task LintReportClean() => Verifier.Verify(LintReport(Xm), extension: "txt");

    [Fact]
    public Task LintReportDirty() => Verifier.Verify(LintReport(DirtyXm), extension: "txt");

    /// <summary>The same pipeline and formatting as src/Xmlang.Cli/Program.cs, in-process.</summary>
    private static string LintReport(string xmText)
    {
        var findings = XmLinter.Lint(XmParser.Parse(xmText), Fixtures.ParsedEm);
        var report = new StringBuilder();
        foreach (var finding in findings)
            report.AppendLine($"{finding.Severity.ToString().ToLowerInvariant(),-7} {finding.Rule}: {finding.Message}");
        var errors = findings.Count(f => f.Severity == XmSeverity.Error);
        report.AppendLine(findings.Count == 0
            ? "OK (no issues found)"
            : $"{errors} error(s), {findings.Count(f => f.Severity == XmSeverity.Warning)} warning(s), "
              + $"{findings.Count(f => f.Severity == XmSeverity.Info)} info");
        return report.ToString();
    }
}
