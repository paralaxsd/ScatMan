using System.Diagnostics;

namespace ScatMan.Core;

/// <summary>
/// Describes a constructor parameter.
/// </summary>
public record ParameterDescriptor(string Name, string TypeName);

/// <summary>
/// Represents a public constructor signature.
/// </summary>
public record ConstructorSignature(
    IReadOnlyList<ParameterDescriptor> Parameters, string? Summary = null);

/// <summary>
/// Represents a public member and its formatted signature.
/// </summary>
public record MemberDescriptor(
    string Name,
    string Kind,
    string Signature,
    string? Summary = null,
    bool IsObsolete = false);

/// <summary>
/// Describes a public type found in inspected assemblies.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)}}}")]
public record TypeDescriptor(
    Type Type, string Kind, string? Summary = null)
{
    public string FullName => DebuggerDisplay.Replace('+', '.');
    public string Name => Type.Name;
    public string Namespace => Type.Namespace ?? "";

    string DebuggerDisplay => Type.FullName ?? Type.Name;
}

/// <summary>
/// Contains package version metadata from NuGet registration.
/// </summary>
public record PackageVersionInfo(
    string Version, DateTimeOffset Published, bool IsPrerelease);

/// <summary>
/// Represents a matching member hit, including owning type information.
/// </summary>
public record MemberSearchHit(
    string TypeFullName, string TypeName, MemberDescriptor Member);

/// <summary>
/// Bundles type and member search results.
/// </summary>
public record SearchHits(
    IReadOnlyList<TypeDescriptor> Types, IReadOnlyList<MemberSearchHit> Members);

/// <summary>
/// Describes a member whose signature changed between two package versions.
/// </summary>
public record ChangedMember(string Name, string Kind, string OldSignature, string NewSignature);

/// <summary>
/// Describes member-level API changes for a single type between two package versions.
/// </summary>
public record TypeDiff(
    string TypeFullName,
    IReadOnlyList<MemberDescriptor> Added,
    IReadOnlyList<MemberDescriptor> Removed,
    IReadOnlyList<ChangedMember> Changed,
    IReadOnlyList<MemberDescriptor> Deprecated);

/// <summary>
/// Represents the full API difference between two versions of a package.
/// </summary>
public record ApiDiff(
    string Package,
    string Version1,
    string Version2,
    IReadOnlyList<string> AddedTypes,
    IReadOnlyList<string> RemovedTypes,
    IReadOnlyList<TypeDiff> ChangedTypes);

/// <summary>
/// Thrown when a requested type cannot be found in downloaded assemblies.
/// </summary>
public sealed class TypeNotFoundException(string typeName)
    : Exception($"Type '{typeName}' not found in the downloaded assemblies.");

/// <summary>
/// Thrown when a requested package does not exist on NuGet.
/// </summary>
public sealed class PackageNotFoundException(string packageId)
    : Exception($"Package '{packageId}' not found on NuGet.");

/// <summary>
/// Build and runtime metadata for CLI and MCP diagnostics.
/// </summary>
public record MetaInfo(
    string Version, string Configuration, DateTime CommitDate,
    bool IsPublic, string OS, string DotNetVersion);