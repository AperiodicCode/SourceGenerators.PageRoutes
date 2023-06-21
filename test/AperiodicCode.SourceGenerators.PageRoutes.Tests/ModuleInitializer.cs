using System.Runtime.CompilerServices;

namespace AperiodicCode.SourceGenerators.PageRoutes.Tests;

public static class ModuleInitializer
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Init()
    {
        DerivePathInfo((file, _, type, method) => new(Path.Join(Path.GetDirectoryName(file), "_snapshots"), type.Name, method.Name));

        VerifySourceGenerators.Initialize();
    }
}
