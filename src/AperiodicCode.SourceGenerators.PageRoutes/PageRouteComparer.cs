namespace AperiodicCode.SourceGenerators.PageRoutes;

public sealed class PageRouteComparer : IEqualityComparer<(string name, List<string>? routes)>
{
    public static PageRouteComparer Instance { get; } = new();

    public bool Equals((string name, List<string>? routes) x, (string name, List<string>? routes) y)
    {
        if (ReferenceEquals(x.Item2, y.Item2))
            return x.Item1 == y.Item1;
        if (x.Item2 == null || y.Item2 == null)
            return false;

        return x.Item1 == y.Item1 && x.Item2.SequenceEqual(y.Item2);
    }

    public int GetHashCode((string name, List<string>? routes) obj)
    {
        unchecked
        {
            int hash = obj.Item1?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ (obj.Item2 == null ? 0 : GetListHashCode(obj.Item2));
            return hash;
        }
    }

    private static int GetListHashCode(List<string> list)
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in list)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
