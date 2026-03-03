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
        [Description("Package version")] string version,
        [Description("Filter by namespace (optional)")] string? ns = null,
        [Description("Case-insensitive substring filter on type name (optional)")] string? filter = null,
        CancellationToken ct = default)
    {
        var assemblies = await FetchAssembliesAsync(packageId, version, ct);
        if (assemblies is null) return Error($"Package {packageId} {version} not found.");

        var types = new TypeInspector().GetTypes(assemblies, ns);

        if (filter is not null)
            types = [.. types.Where(t => t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))];

        return Serialize(new
        {
            package    = packageId,
            version,
            @namespace = ns,
            filter,
            count      = types.Count,
            types      = types.Select(t => new { t.FullName, t.Name, t.Namespace, t.Kind })
        });
    }

    [McpServerTool(Name = "search")]
    [Description(
        "Search for types and members by name across an entire NuGet package. " +
        "Useful when you know a method or type name exists but not which type it belongs to.")]
    static async Task<string> Search(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version")] string version,
        [Description("Case-insensitive substring to search for in type and member names")] string query,
        [Description("Restrict search to this namespace (optional)")] string? ns = null,
        CancellationToken ct = default)
    {
        var assemblies = await FetchAssembliesAsync(packageId, version, ct);
        if (assemblies is null) return Error($"Package {packageId} {version} not found.");

        var hits = new TypeInspector().Search(assemblies, query, ns);

        return Serialize(new
        {
            package        = packageId,
            version,
            query,
            @namespace     = ns,
            matchingTypes  = hits.Types.Select(t => new { t.FullName, t.Name, t.Namespace, t.Kind }),
            matchingMembers = hits.Members.Select(h => new
            {
                h.TypeName,
                h.TypeFullName,
                h.Member.Kind,
                h.Member.Name,
                h.Member.Signature
            })
        });
    }

    [McpServerTool(Name = "get_members")]
    [Description(
        "List all public members of a type (constructors, methods, properties, events, fields). " +
        "Constructors are always included — no need to call a separate ctors tool.")]
    static async Task<string> GetMembers(
        [Description("NuGet package ID")] string packageId,
        [Description("Package version")] string version,
        [Description("Full or simple type name, e.g. \"WasapiCapture\" or \"NAudio.CoreAudioApi.WasapiCapture\"")] string typeName,
        CancellationToken ct = default)
    {
        var assemblies = await FetchAssembliesAsync(packageId, version, ct);
        if (assemblies is null) return Error($"Package {packageId} {version} not found.");

        IReadOnlyList<CoreMemberDescriptor> members;
        try   { members = new TypeInspector().GetMembers(assemblies, typeName); }
        catch (TypeNotFoundException ex) { return Error(ex.Message); }

        return Serialize(new
        {
            package  = packageId,
            version,
            typeName,
            count   = members.Count,
            members = members.Select(m => new { m.Kind, m.Name, m.Signature })
        });
    }

    static async Task<IReadOnlyList<string>?> FetchAssembliesAsync(
        string packageId, string version, CancellationToken ct)
    {
        try   { return await new PackageDownloader().DownloadAsync(packageId, version, ct); }
        catch { return null; }
    }

    static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);
}
