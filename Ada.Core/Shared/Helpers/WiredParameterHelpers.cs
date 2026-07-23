namespace Ada.Core.Shared.Helpers;

public static class WiredParameterHelpers
{
    public static string Serialize(IEnumerable<int> parameters)
    {
        return string.Join(",", parameters);
    }

    public static List<int> Deserialize(string? parameters)
    {
        if (string.IsNullOrEmpty(parameters))
        {
            return [];
        }

        return parameters
            .Split(',')
            .Select(x => int.TryParse(x, out var value) ? value : 0)
            .ToList();
    }
}
