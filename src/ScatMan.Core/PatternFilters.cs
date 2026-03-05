using Microsoft.Extensions.FileSystemGlobbing;

namespace ScatMan.Core;

public static class PatternFilters
{
    public static bool MatchesExactOrGlob(string? value, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return true;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return IsGlobPattern(pattern)
            ? GlobMatches(value, pattern)
            : value.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesSubstringOrGlob(string? value, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return true;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return IsGlobPattern(pattern)
            ? GlobMatches(value, pattern)
            : value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    static bool IsGlobPattern(string pattern) =>
        pattern.Contains('*') ||
        pattern.Contains('?') ||
        pattern.Contains('[') ||
        pattern.Contains('{');

    static bool GlobMatches(string value, string pattern)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(pattern);
        return matcher.Match(value).HasMatches;
    }
}
