using NuGet.Configuration;

namespace ScatMan.Core;

/// <summary>
/// Resolves NuGet package sources from the nuget.config hierarchy.
/// </summary>
public static class PackageSourceResolver
{
    const string DefaultSourceUrl = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// Returns all enabled package sources from the nuget.config hierarchy.
    /// </summary>
    public static IReadOnlyList<(string Name, string Url)> GetSources()
    {
        var settings = Settings.LoadDefaultSettings(null);
        var packageSourceProvider = new PackageSourceProvider(settings);
        var sources = packageSourceProvider.LoadPackageSources();

        return sources
            .Where(s => s.IsEnabled)
            .Select(s => (s.Name, s.SourceUri.AbsoluteUri))
            .ToList();
    }

    /// <summary>
    /// Resolves a source name or URL to a concrete URL.
    /// If nameOrUrl matches a configured source name, returns its URL.
    /// If nameOrUrl is a URL, returns it as-is.
    /// If nameOrUrl is null/empty, returns the default source (nuget.org).
    /// </summary>
    public static string ResolveSourceUrl(string? nameOrUrl)
    {
        if (string.IsNullOrWhiteSpace(nameOrUrl))
            return DefaultSourceUrl;

        var sources = GetSources();
        var match = sources.FirstOrDefault(s => s.Name.Equals(nameOrUrl, StringComparison.OrdinalIgnoreCase));

        if (match != default)
            return match.Url;

        // Treat as direct URL
        if (Uri.TryCreate(nameOrUrl, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            return nameOrUrl;

        throw new ArgumentException($"Unknown package source '{nameOrUrl}'. Use 'scatman sources' to list available sources.");
    }
}
