using Microsoft.CodeAnalysis;

namespace AperiodicCode.SourceGenerators.PageRoutes.Tests;

public static class TestHelper
{
    public static Task Verify(Compilation? compilation, ref GeneratorDriver driver)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver);
    }
}
