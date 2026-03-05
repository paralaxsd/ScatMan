using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using ScatMan.Core;
using CoreMemberDescriptor = ScatMan.Core.MemberDescriptor;

namespace ScatMan.Mcp.Tools;

[McpServerToolType]
static class ScatManTools
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented    = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpServerTool(Name = "get_versions")]
    [Description("List available versions of a NuGet package, newest first.")]
    static async Task<string> GetVersions(
        [Description("NuGet package ID, e.g. \"Newtonsoft.Json\"")] string packageId,
        [Description("Include prerelease versions (default: false)")] bool includePrerelease = false,
        CancellationToken ct = default)
    {
        var client = new NuGetRegistrationClient();

        IReadOnlyList<PackageVersionInfo> all;
        try   { all = await client.GetVersionsAsync(packageId, ct); }
        catch (PackageNotFoundException ex) { return Error(ex.Message); }

        var versions = includePrerelease ? all : all.Where(v => !v.IsPrerelease).ToList();

        return Serialize(new
        {
            package  = packageId,
            total    = all.Count,
            versions = versions.Select(v => new
            {
                version      = v.Version,
                published    = v.Published.ToString("yyyy-MM-dd"),
                isPrerelease = v.IsPrerelease
            })
        });
    }

    [McpServerTool(Name = "get_types")]
    [Description("List all public types in a NuGet package.")]
    static async Task<string> GetTypes(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version, or alias: latest / latest-pre")] string version,
        [Description(
            "Namespace filter (exact or glob, optional). " +
            "Glob syntax follows Microsoft.Extensions.FileSystemGlobbing.")]
        string? ns = null,
        [Description(
            "Type-name filter (glob optional). " +
            "Plain text is case-insensitive substring.")]
        string? filter = null,
        CancellationToken ct = default)
    {
        string resolvedVersion;
        try { resolvedVersion = await ResolveVersionAsync(packageId, version, ct); }
        catch (PackageNotFoundException ex) { return Error(ex.Message); }

        var assemblies = await FetchAssembliesAsync(packageId, resolvedVersion, ct);
        if (assemblies is null) return Error($"Package {packageId} {resolvedVersion} not found.");

        var types = new TypeInspector().GetTypes(assemblies, ns);

        if (filter is not null)
            types = [.. types.Where(t => PatternFilters.MatchesSubstringOrGlob(t.Name, filter))];

        var result = new GetTypesResult(
            packageId,
            version,
            resolvedVersion,
            ns,
            filter,
            types.Count,
            [.. types.Select(t => new MappedType(
                t.FullName,
                t.Name,
                t.Namespace,
                t.Kind,
                t.Summary))]);

        return Serialize(result);
    }

    [McpServerTool(Name = "search")]
    [Description(
        "Search for types and members by name across an entire NuGet package. " +
        "Useful when you know a method or type name exists but not which type it belongs to.")]
    static async Task<string> Search(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version, or alias: latest / latest-pre")] string version,
        [Description("Name query: glob pattern or plain case-insensitive substring")] string query,
        [Description("Namespace filter (exact or glob, optional)")] string? ns = null,
        CancellationToken ct = default)
    {
        string resolvedVersion;
        try { resolvedVersion = await ResolveVersionAsync(packageId, version, ct); }
        catch (PackageNotFoundException ex) { return Error(ex.Message); }

        var assemblies = await FetchAssembliesAsync(packageId, resolvedVersion, ct);
        if (assemblies is null) return Error($"Package {packageId} {resolvedVersion} not found.");

        var hits = new TypeInspector().Search(assemblies, query, ns);

        var result = new SearchResult(
            packageId,
            version,
            resolvedVersion,
            query,
            ns,
            [.. hits.Types.Select(t => new MappedType(
                t.FullName,
                t.Name,
                t.Namespace,
                t.Kind,
                t.Summary))],
            [.. hits.Members.Select(h => new MappedMember(
                h.TypeName,
                h.TypeFullName,
                h.Member.Kind,
                h.Member.Name,
                h.Member.Signature,
                h.Member.Summary))]);

        return Serialize(result);
    }

    [McpServerTool(Name = "get_members")]
    [Description(
        "List all public members of a type (constructors, methods, properties, events, fields). " +
        "Constructors are always included — no need to call a separate ctors tool.")]
    static async Task<string> GetMembers(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version, or alias: latest / latest-pre")] string version,
        [Description("Full or simple type name, e.g. \"WasapiCapture\" or \"NAudio.CoreAudioApi.WasapiCapture\"")] string typeName,
        [Description("Include optional parameter default values in signatures (default: true)")]
        bool includeDefaultValues = true,
        [Description("Include member and parameter attributes in signatures (default: false)")]
        bool includeAttributes = false,
        CancellationToken ct = default)
    {
        string resolvedVersion;
        try { resolvedVersion = await ResolveVersionAsync(packageId, version, ct); }
        catch (PackageNotFoundException ex) { return Error(ex.Message); }

        var assemblies = await FetchAssembliesAsync(packageId, resolvedVersion, ct);
        if (assemblies is null) return Error($"Package {packageId} {resolvedVersion} not found.");

        IReadOnlyList<CoreMemberDescriptor> members;
        try
        {
            members = new TypeInspector().GetMembers(
                assemblies,
                typeName,
                includeDefaultValues,
                includeAttributes);
        }
        catch (TypeNotFoundException ex) { return Error(ex.Message); }

        var result = new GetMembersResult(
            packageId,
            version,
            resolvedVersion,
            typeName,
            includeDefaultValues,
            includeAttributes,
            members.Count,
            [.. members.Select(m => new MemberDetail(m.Kind, m.Name, m.Signature, m.Summary))]);

        return Serialize(result);
    }

    [McpServerTool(Name = "meta")]
    [Description(
        "Show metadata about the ScatMan MCP tool, including version and build information.")]
    static string Meta() => Serialize(MetaInfoFactory.Create());

    static async Task<IReadOnlyList<string>?> FetchAssembliesAsync(
        string packageId, string version, CancellationToken ct)
    {
        try   { return await new PackageDownloader().DownloadAsync(packageId, version, ct); }
        catch { return null; }
    }

    static async Task<string> ResolveVersionAsync(
        string packageId,
        string version,
        CancellationToken ct)
    {
        return await PackageVersionResolver.ResolveAsync(packageId, version, ct);
    }

    static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);

}
