namespace Emlang.CodeGen;

/// <summary>
/// The per-consumer facts an emlang spec deliberately does NOT carry: the C# namespace and
/// the union-name prefix. Consumers declare them as MSBuild metadata on the spec's
/// AdditionalFiles item (EmlangPrefix, optional EmlangNamespace; namespace defaults to
/// RootNamespace). SpecFile is the spec's file name, used in generated-file headers.
/// </summary>
public record EmitTarget(string Namespace, string Prefix, string SpecFile)
{
    public string CommandUnion => Prefix + "Command";
    public string EventUnion => Prefix + "Event";
    public string ErrorUnion => Prefix + "Error";
    public string StateType => Prefix + "State";
    public string ContextType => Prefix + "Context";
}
