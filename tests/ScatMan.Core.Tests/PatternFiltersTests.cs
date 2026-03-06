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
    [InlineData("", "", true)]
    [InlineData("foo.txt", "", true)]
    [InlineData("", "foo.txt", false)]
    [InlineData("foo.txt", "*.*", true)]
    [InlineData("foo.txt", "foo.*", true)]
    [InlineData("foo.txt", "*.TXT", true)]
    [InlineData("foo.txt", "*foo*", true)]
    [InlineData("foo.txt", "*bar*", false)]
    [InlineData("foo.txt", "foo?txt", false)]
    [InlineData("foo.txt", "foo.txt?", false)]
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
    [InlineData("", "", true)]
    [InlineData("foo.txt", "", true)]
    [InlineData("", "foo", false)]
    [InlineData("foo.txt", "txt", true)]
    [InlineData("foo.txt", ".t", true)]
    [InlineData("foo.txt", "o.t", true)]
    [InlineData("foo.txt", "o.x", false)]
    [InlineData("foo.txt", "FOO.TXT", true)]
    [InlineData("foo.txt", "foo.txt", true)]
    [InlineData("foo.txt", "bar.txt", false)]
    [InlineData("foo.txt", "*foo*", true)]
    [InlineData("foo.txt", "*bar*", false)]
    [InlineData("foo.txt", "foo?txt", false)]
    public void MatchesSubstringOrGlob_Works(string? value, string? pattern, bool expected)
        => PatternFilters.MatchesSubstringOrGlob(value, pattern).ShouldBe(expected);

    // Path patterns (slash-separated) and namespace-style strings (dots are NOT separators
    // in FileSystemGlobbing — * matches across dots, ** matches across slashes only).
    [Theory]
    [InlineData("path/to/file.txt", "**/*.txt", true)]
    [InlineData("path/to/file.txt", "path/**", true)]
    [InlineData("path/to/file.txt", "path/**/*.txt", true)]
    [InlineData("path/to/file.txt", "other/**", false)]
    [InlineData("file.txt", "**/*.txt", true)]
    [InlineData("file.txt", "**", true)]
    [InlineData("path/to/file.txt", "path/to/file.txt", true)]
    [InlineData("path/to/file.txt", "path/to/other.txt", false)]
    // Dots are NOT path separators — * matches across them
    [InlineData("System.Collections.Generic", "System.*", true)]
    [InlineData("System.Collections.Generic", "System.Collections.*", true)]
    [InlineData("System.Collections.Generic", "Other.*", false)]
    // ** does NOT cross dots — equivalent to * in dotted strings
    [InlineData("System.Collections.Generic", "System.**", true)]
    [InlineData("System.Collections.Generic", "Other.**", false)]
    public void MatchesExactOrGlob_PathAndNamespacePatterns(string value, string pattern, bool expected)
        => PatternFilters.MatchesExactOrGlob(value, pattern).ShouldBe(expected);

    [Theory]
    [InlineData("path/to/file.txt", "**/*.txt", true)]
    [InlineData("path/to/file.txt", "path/**", true)]
    [InlineData("path/to/file.txt", "other/**", false)]
    [InlineData("file.txt", "**", true)]
    // Substring matching works across slashes and dots
    [InlineData("path/to/file.txt", "to/file", true)]
    [InlineData("path/to/file.txt", "other", false)]
    [InlineData("System.Collections.Generic", "Collections", true)]
    [InlineData("System.Collections.Generic", "System.*", true)]
    [InlineData("System.Collections.Generic", "Other.*", false)]
    public void MatchesSubstringOrGlob_PathAndNamespacePatterns(string value, string pattern, bool expected)
        => PatternFilters.MatchesSubstringOrGlob(value, pattern).ShouldBe(expected);

}
