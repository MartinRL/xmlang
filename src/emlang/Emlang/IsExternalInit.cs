// ponytail: netstandard2.0 lacks IsExternalInit; records need it — 3-line shim beats a PolySharp dep.
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit;
