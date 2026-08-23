// xm — the xmlang linter CLI (xmlang-spec.md).
//   xm lint <spec.xm.yaml>
// Resolves the xm's `model:` Event Model(s) relative to the xm file; exits 1 on any error.

using Xmlang;

if (args is not ["lint", var xmPath])
{
    Console.Error.WriteLine("usage: xm lint <spec.xm.yaml>");
    return 2;
}

var xm = XmParser.Parse(File.ReadAllText(xmPath));
if (xm.Models.Count == 0)
{
    Console.Error.WriteLine($"{xmPath}: no 'model:' key — cannot resolve references");
    return 2;
}

var xmDir = Path.GetDirectoryName(Path.GetFullPath(xmPath))!;
var em = EmParser.Merge([.. xm.Models.Select(m => EmParser.Parse(File.ReadAllText(Path.Combine(xmDir, m))))]);

var findings = XmLinter.Lint(xm, em);
foreach (var finding in findings)
    Console.WriteLine($"{finding.Severity.ToString().ToLowerInvariant(),-7} {finding.Rule}: {finding.Message}");

var errors = findings.Count(f => f.Severity == XmSeverity.Error);
Console.WriteLine(findings.Count == 0
    ? "OK (no issues found)"
    : $"{errors} error(s), {findings.Count(f => f.Severity == XmSeverity.Warning)} warning(s), "
      + $"{findings.Count(f => f.Severity == XmSeverity.Info)} info");
return errors > 0 ? 1 : 0;
