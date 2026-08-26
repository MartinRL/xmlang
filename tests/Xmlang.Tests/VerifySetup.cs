using System.Runtime.CompilerServices;

namespace Xmlang.Tests;

internal static class VerifySetup
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyDiffPlex.Initialize();
        Verifier.UseProjectRelativeDirectory("Snapshots");
    }
}
