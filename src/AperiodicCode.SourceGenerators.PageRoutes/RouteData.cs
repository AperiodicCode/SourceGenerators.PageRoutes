using System.Text.RegularExpressions;

namespace AperiodicCode.SourceGenerators.PageRoutes;

public class RouteData
{
    public RouteData(string pageName, string route)
    {
        // If pageName ends with "Page" remove it
        if (pageName.EndsWith("Page"))
        {
            pageName = pageName.Substring(0, pageName.Length - 4);
        }

        PageName = pageName;
        Route = route;
        Parameters = GetRouteParameters(route);
    }

    private string PageName { get; set; }
    private string Route { get; set; }

    /// <summary>
    /// List of params, like "long accountId", "int childPersonId", etc.
    /// </summary>
    private List<string> Parameters { get; set; }

    private List<string> ParametersNoType { get; set; } = new();

    private Dictionary<string, string> ParameterReplacements { get; set; } = new();

    public string GetUrlDeclaration()
    {
        if (!Parameters.Any())
        {
            return $"\n    public const string {PageName} = \"{Route}\";\n";
        }

        var paramString = string.Join(", ", Parameters);

        var paramStringNoType = string.Join(", ", ParametersNoType);

        var route = Route;

        foreach (var parameter in ParameterReplacements)
        {
            route = route.Replace(parameter.Key, parameter.Value);
        }

        var function = $$"""

        /// <summary>
        /// Route: <c>{{Route}}</c>
        /// </summary>
        public static string {{PageName}}({{paramString}})
        {
            return $"{{route}}";
        }

    """;

        return function;
    }

    private List<string> GetRouteParameters(string route)
    {
        var result = new List<string>();
        var paramsNoType = new List<string>();
        var parameterReplacements = new Dictionary<string, string>();

        var matches = Regex.Matches(route, @"(?<=\{)[^}]*(?=\})");

        foreach (var match in matches)
        {
            var param = match.ToString();

            if (param is null)
                continue;

            // No type constraint, it's just a string
            if (!param.Contains(":"))
            {
                result.Add($"string {param}");
                paramsNoType.Add(param);
                parameterReplacements.Add(param, param);

                continue;
            }

            var split = param.Split(':');

            if (split.Length == 2)
            {
                result.Add($"{split[1]} {split[0]}");
                paramsNoType.Add(split[0]);
                parameterReplacements.Add(param, split[0]);
            }
        }

        ParameterReplacements = parameterReplacements;
        ParametersNoType = paramsNoType;

        return result;
    }
}
