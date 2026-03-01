using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScatMan.Core;

public sealed class NuGetRegistrationClient
{
    static readonly HttpClient Http = new();
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    const string BaseUrl = "https://api.nuget.org/v3/registration5-semver1";

    public async Task<IReadOnlyList<PackageVersionInfo>> GetVersionsAsync(
        string packageId, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/{packageId.ToLowerInvariant()}/index.json";

        using var response = await Http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new PackageNotFoundException(packageId);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var index = JsonSerializer.Deserialize<RegistrationIndex>(json, JsonOpts)
            ?? throw new InvalidOperationException("Empty response from NuGet registration API.");

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

        versions.Sort((a, b) => b.Published.CompareTo(a.Published));
        return versions;
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
