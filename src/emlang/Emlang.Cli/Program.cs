using System.Reflection;
using System.Text;
using Emlang.Linting;

// em — the .NET clone of the Go emlang CLI (github.com/emlang-project/emlang).
// Phase 1: parse, lint, version, help + the .emlang.yaml lint.ignore config
// (kvissig's Go lint depends on it). Output formats mirror the reference CLI
// byte-for-byte on valid specs (props/tests print in document order, which the
// Go CLI leaves to map iteration order).
// Phase 2: fmt (-w, --keys short|long, fmt.keys config) + stdin '-' everywhere.
// Like the reference, fmt renders from the AST — comments are dropped.

Console.OutputEncoding = Encoding.UTF8;
// Byte parity with the Go CLI, which emits \n on every platform.
Console.Out.NewLine = "\n";
Console.Error.NewLine = "\n";

const string SpecVersion = "1.1.0 dialect, forked from upstream 1.0.0";

var (remaining, configPath) = ExtractConfigFlag(args);

if (remaining.Count < 1)
{
    PrintUsage();
    return 1;
}

switch (remaining[0])
{
    case "version":
        Console.WriteLine($"em version {ToolVersion()} (emlang spec {SpecVersion})");
        return 0;
    case "help" or "-h" or "--help":
        PrintUsage();
        return 0;
    case "parse":
        return CmdParse(remaining.Skip(1).ToArray());
    case "lint":
        return CmdLint(remaining.Skip(1).ToArray(), configPath);
    case "fmt":
        return CmdFmt(remaining.Skip(1).ToArray(), configPath);
    default:
        Console.Error.WriteLine($"Unknown command: {remaining[0]}");
        PrintUsage();
        return 1;
}

static (List<string> Remaining, string ConfigPath) ExtractConfigFlag(string[] args)
{
    var remaining = new List<string>();
    var configPath = "";
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] is "-c" or "--config" && i + 1 < args.Length)
            configPath = args[++i];
        else
            remaining.Add(args[i]);
    }

    return (remaining, configPath);
}

// The Go CLI's config resolution: -c flag > EMLANG_CONFIG env > .emlang.yaml in cwd.
// A missing default file is fine; a missing explicit path is an error. Phase 2 reads
// lint.ignore + fmt.keys.
static (IReadOnlyList<string> LintIgnore, string FmtKeys)? LoadConfig(string configPath)
{
    var explicitPath = configPath.Length > 0
        ? configPath
        : Environment.GetEnvironmentVariable("EMLANG_CONFIG") ?? "";
    var path = explicitPath.Length > 0 ? explicitPath : ".emlang.yaml";

    if (!File.Exists(path))
    {
        if (explicitPath.Length == 0)
            return ([], "");
        Console.Error.WriteLine($"Error loading config: reading config: {path}: file not found");
        return null;
    }

    try
    {
        var stream = new YamlDotNet.RepresentationModel.YamlStream();
        stream.Load(new StringReader(File.ReadAllText(path)));
        var rules = new List<string>();
        var fmtKeys = "";
        if (stream.Documents.Count > 0
            && stream.Documents[0].RootNode is YamlDotNet.RepresentationModel.YamlMappingNode root)
        {
            if (root.Children.TryGetValue("lint", out var lintNode)
                && lintNode is YamlDotNet.RepresentationModel.YamlMappingNode lint
                && lint.Children.TryGetValue("ignore", out var ignoreNode)
                && ignoreNode is YamlDotNet.RepresentationModel.YamlSequenceNode ignore)
            {
                foreach (var rule in ignore.Children)
                    rules.Add(rule.ToString());
            }

            if (root.Children.TryGetValue("fmt", out var fmtNode)
                && fmtNode is YamlDotNet.RepresentationModel.YamlMappingNode fmt
                && fmt.Children.TryGetValue("keys", out var keysNode))
            {
                fmtKeys = keysNode.ToString();
            }
        }

        return (rules, fmtKeys);
    }
    catch (YamlDotNet.Core.YamlException ex)
    {
        Console.Error.WriteLine($"Error loading config: parsing config {path}: {ex.Message}");
        return null;
    }
}

static string ToolVersion()
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    var plus = version.IndexOf('+');
    return plus < 0 ? version : version[..plus];
}

static void PrintUsage()
{
    Console.WriteLine("em - the emlang toolchain for .NET (https://github.com/MartinRL/xmlang)");
    Console.WriteLine();
    Console.WriteLine("Usage: em [-c <config>] <command> [arguments]");
    Console.WriteLine();
    Console.WriteLine("Flags:");
    Console.WriteLine("  -c, --config <file>  Path to config file (default: .emlang.yaml, or EMLANG_CONFIG env)");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  parse <file>         Parse a YAML source file and show structure (use - for stdin)");
    Console.WriteLine("  lint <file>          Lint a YAML source file for issues (use - for stdin)");
    Console.WriteLine("  fmt <file>           Format a YAML source file (use - for stdin, -w for in-place)");
    Console.WriteLine("                       --keys short|long: override key style");
    Console.WriteLine("  version              Print version information");
    Console.WriteLine("  help                 Show this help message");
}

static EmDocument? ParseFile(string path, out string name)
{
    name = path == "-" ? "<stdin>" : path;
    string text;
    try
    {
        text = path == "-" ? Console.In.ReadToEnd() : File.ReadAllText(path);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Error reading input: {ex.Message}");
        return null;
    }

    try
    {
        return EmAst.Parse(text);
    }
    catch (FormatException ex)
    {
        Console.Error.WriteLine($"Parse error in {name}: {ex.Message}");
        return null;
    }
}

static int CmdParse(string[] args)
{
    if (args.Length < 1)
    {
        Console.Error.WriteLine("Usage: em parse <file>");
        return 1;
    }

    if (ParseFile(args[0], out var name) is not { } document)
        return 1;

    Console.WriteLine($"Parsed {name} successfully");
    Console.WriteLine("----------------------------------------");
    Console.WriteLine($"Document with {document.SliceCount} slice(s)");

    foreach (var subDoc in document.SubDocs)
    {
        foreach (var slice in subDoc.Slices)
        {
            Console.WriteLine();
            PrintSlice(slice);
        }
    }

    return 0;
}

static void PrintSlice(EmSlice slice)
{
    Console.WriteLine($"Slice: {(slice.Name.Length == 0 ? "(anonymous)" : slice.Name)}");
    Console.WriteLine($"  {slice.Elements.Count} element(s)");
    foreach (var element in slice.Elements)
        PrintElement("    ", element);

    if (slice.Tests.Count > 0)
    {
        Console.WriteLine($"  {slice.Tests.Count} attached test(s)");
        foreach (var test in slice.Tests)
            PrintTest(test);
    }
}

static void PrintTest(EmTest test)
{
    Console.WriteLine($"Test:   {test.Name}");
    PrintTestSection("Given", test.Given);
    PrintTestSection("When", test.When);
    PrintTestSection("Then", test.Then);
}

static void PrintTestSection(string label, IReadOnlyList<EmElement> elements)
{
    if (elements.Count == 0)
        return;
    Console.WriteLine($"  {label}: {elements.Count} element(s)");
    foreach (var element in elements)
        PrintElement("    ", element);
}

static void PrintElement(string indent, EmElement element)
{
    var swimlane = element.Swimlane.Length == 0 ? "" : element.Swimlane + "/";
    Console.WriteLine($"{indent}{element.Type.Display()}: {swimlane}{element.Name}");

    if (element.Props.Count > 0)
    {
        Console.WriteLine($"{indent}  props:");
        foreach (var prop in element.Props)
            Console.WriteLine($"{indent}    {prop.Key}: {FormatValue(prop.Value)}");
    }
}

// Go fmt's %v: scalars verbatim, sequences as [a b c], mappings as map[k:v].
static string FormatValue(object? value) => value switch
{
    IReadOnlyList<KeyValuePair<string, object?>> mapping =>
        "map[" + string.Join(" ", mapping.Select(e => $"{e.Key}:{FormatValue(e.Value)}")) + "]",
    IReadOnlyList<object?> sequence =>
        "[" + string.Join(" ", sequence.Select(FormatValue)) + "]",
    null => "<nil>",
    _ => value.ToString() ?? "",
};

static int CmdLint(string[] args, string configPath)
{
    if (args.Length < 1)
    {
        Console.Error.WriteLine("Usage: em lint <file>");
        return 1;
    }

    if (LoadConfig(configPath) is not { } config)
        return 1;
    if (ParseFile(args[0], out var name) is not { } document)
        return 1;

    var issues = Linter.Lint(document, config.LintIgnore);

    if (issues.Count == 0)
    {
        Console.WriteLine($"{name}: OK (no issues found)");
        return 0;
    }

    var errorCount = issues.Count(i => i.Severity == LintSeverity.Error);
    var warningCount = issues.Count - errorCount;

    Console.WriteLine($"{name}: {issues.Count} issue(s) found");
    Console.WriteLine("----------------------------------------");
    foreach (var issue in issues)
        Console.WriteLine(
            $"{name}:{issue.Line}:{issue.Column}: {issue.Severity.Display()}: {issue.Message} [{issue.Rule}]");
    Console.WriteLine("----------------------------------------");
    Console.WriteLine($"Summary: {errorCount} error(s), {warningCount} warning(s)");

    return errorCount > 0 ? 1 : 0;
}

static int CmdFmt(string[] args, string configPath)
{
    var write = false;
    var keysFlag = "";
    var keysFlagSet = false;
    var files = new List<string>();
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-w" or "--write":
                write = true;
                break;
            case "--keys" when i + 1 < args.Length:
                keysFlag = args[++i];
                keysFlagSet = true;
                break;
            case var arg when arg.StartsWith("--keys=", StringComparison.Ordinal):
                keysFlag = arg.Substring("--keys=".Length);
                keysFlagSet = true;
                break;
            default:
                files.Add(args[i]);
                break;
        }
    }

    if (files.Count < 1)
    {
        Console.Error.WriteLine("Usage: em fmt [-w] [--keys short|long] <file>");
        return 1;
    }

    var input = files[0];
    if (write && input == "-")
    {
        Console.Error.WriteLine("Error: -w cannot be used with stdin");
        return 1;
    }

    if (LoadConfig(configPath) is not { } config)
        return 1;
    if (ParseFile(input, out _) is not { } document)
        return 1;

    // Priority mirrors the Go CLI: flag > config fmt.keys > "long".
    var keyStyle = config.FmtKeys.Length > 0 ? config.FmtKeys : "long";
    if (keysFlagSet)
        keyStyle = keysFlag;

    var output = EmFormatter.Format(document, keyStyle);

    if (write)
    {
        try
        {
            File.WriteAllText(input, output);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Error writing {input}: {ex.Message}");
            return 1;
        }
    }
    else
    {
        Console.Out.Write(output);
    }

    return 0;
}
