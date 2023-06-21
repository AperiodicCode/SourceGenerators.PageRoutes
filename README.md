# AperiodicCode.SourceGenerators.PageRoutes

Automatically generate page routes for Blazor page components.

## Example

Input `TestPage.razor`:

```cshtml
@page "/test"
```

Output:

```cs
public static partial class Pages
{
    public const string Test = "/test";
}
```
