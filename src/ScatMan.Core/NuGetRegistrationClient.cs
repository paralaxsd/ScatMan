using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetRepositories = NuGet.Repositories;

namespace ScatMan.Core;

/// <summary>
/// Reads package version metadata from NuGet registration endpoints.
/// </summary>
public sealed class NuGetRegistrationClient
{
    static readonly HttpClient Http = new(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip
    });
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    const string SemVer1Url = "https://api.nuget.org/v3/registration5-semver1";
    const string SemVer2Url = "https://api.nuget.org/v3/registration5-gz-semver2";

    /// <summary>
    /// Returns listed package versions from SemVer1 and SemVer2 registration feeds.
    /// </summary>
    /// <param name="packageId">NuGet package ID.</param>
    /// <param name="sourceUrl">Optional custom NuGet source URL. If null, uses nuget.org.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Versions ordered by published date descending.</returns>
    /// <exception cref="PackageNotFoundException">
    /// Thrown when the package does not exist in NuGet registration.
    /// </exception>
    public async Task<IReadOnlyList<PackageVersionInfo>> GetVersionsAsync(
        string packageId, string? sourceUrl = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(sourceUrl))
            return await GetVersionsFromRepositoryAsync(packageId, sourceUrl, ct);

        var (v1, notFound) = await FetchVersionsAsync(SemVer1Url, packageId, ct);
        if (notFound)
            throw new PackageNotFoundException(packageId);

        var (v2, _) = await FetchVersionsAsync(SemVer2Url, packageId, ct);

        var merged = v1.Concat(v2)
            .DistinctBy(v => v.Version)
            .OrderByDescending(v => v.Published)
            .ToList();

        return merged;
    }

    async Task<(List<PackageVersionInfo> Versions, bool NotFound)> FetchVersionsAsync(
        string baseUrl, string packageId, CancellationToken ct)
    {
        var url = $"{baseUrl}/{packageId.ToLowerInvariant()}/index.json";

        using var response = await Http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return ([], true);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, JsonOpts);
        if (index is null) return ([], false);

        var versions = new List<PackageVersionInfo>();

        foreach (var page in index.Items)
        {
            var items = page.Items;
            if (items is null)
            {
                var pageJson = await Http.GetStringAsync(page.Id, ct);
                items = JsonSerializer.Deserialize<RegistrationPage>(pageJson, JsonOpts)?.Items;
            }

            if (items is null) continue;

            foreach (var item in items)
            {
                var entry = item.CatalogEntry;
                if (!entry.Listed) continue;

                versions.Add(new PackageVersionInfo(
                    entry.Version,
                    entry.Published,
                    entry.Version.Contains('-')));
            }
        }

        return (versions, false);
    }

    async Task<IReadOnlyList<PackageVersionInfo>> GetVersionsFromRepositoryAsync(
        string packageId, string sourceUrl, CancellationToken ct)
    {
        try
        {
            var repo = Repository.Factory.GetCoreV3(sourceUrl);
            var resource = await repo.GetResourceAsync<PackageMetadataResource>(ct);

            var packages = (await resource.GetMetadataAsync(
                packageId,
                includePrerelease: true,
                includeUnlisted: false,
                sourceCacheContext: new SourceCacheContext(),
                log: NullLogger.Instance,
                token: ct)).ToList();

            if (packages.Count == 0)
                throw new PackageNotFoundException(packageId);

            return packages
                .OrderByDescending(p => p.Published ?? DateTimeOffset.MinValue)
                .Select(p => new PackageVersionInfo(
                    p.Identity.Version.ToString(),
                    p.Published ?? DateTimeOffset.Now,
                    p.Identity.Version.IsPrerelease))
                .ToList();
        }
        catch (FatalProtocolException ex) when (ex.Message.Contains("404"))
        {
            throw new PackageNotFoundException(packageId);
        }
    }

    record RegistrationIndex(
        [property: JsonPropertyName("items")] RegistrationPage[] Items);

    record RegistrationPage(
        [property: JsonPropertyName("@id")] string Id,
        [property: JsonPropertyName("items")] RegistrationItem[]? Items);

    record RegistrationItem(
        [property: JsonPropertyName("catalogEntry")] CatalogEntry CatalogEntry);

    record CatalogEntry(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("published")] DateTimeOffset Published,
        [property: JsonPropertyName("listed")] bool Listed);
}
