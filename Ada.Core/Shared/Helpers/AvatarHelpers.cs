using System.Text.RegularExpressions;

namespace Ada.Core.Shared.Helpers;

public static partial class AvatarHelpers
{
    private const int _maxFigureCodeLength = 256;

    private static readonly Regex _figureCodePattern =
        MyRegex();

    public static bool IsValidFigureCode(string? figureCode)
        => !string.IsNullOrEmpty(figureCode) &&
           figureCode.Length <= _maxFigureCodeLength &&
           _figureCodePattern.IsMatch(figureCode);
    [GeneratedRegex(@"\A[a-z]{2}(-\d{1,6}){1,4}(\.[a-z]{2}(-\d{1,6}){1,4})*\z", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
