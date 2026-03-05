using Shouldly;

namespace ScatMan.Core.Tests;

public sealed class PatternFiltersTests
{
    [Theory]
    [InlineData("foo.txt", "foo.txt", true)]
    [InlineData("foo.txt", "FOO.TXT", true)]
    [InlineData("foo.txt", "bar.txt", false)]
    [InlineData("foo.txt", "*.txt", true)]
    [InlineData("foo.txt", "*.md", false)]
    [InlineData("foo.txt", null, true)]
    [InlineData(null, "foo.txt", false)]
    [InlineData(null, null, true)]
    public void MatchesExactOrGlob_Works(string? value, string? pattern, bool expected)
        => PatternFilters.MatchesExactOrGlob(value, pattern).ShouldBe(expected);

    [Theory]
    [InlineData("foo.txt", "foo", true)]
    [InlineData("foo.txt", "FOO", true)]
    [InlineData("foo.txt", "bar", false)]
    [InlineData("foo.txt", "*.txt", true)]
    [InlineData("foo.txt", "*.md", false)]
    [InlineData("foo.txt", null, true)]
    [InlineData(null, "foo", false)]
    [InlineData(null, null, true)]
    public void MatchesSubstringOrGlob_Works(string? value, string? pattern, bool expected)
        => PatternFilters.MatchesSubstringOrGlob(value, pattern).ShouldBe(expected);
}
