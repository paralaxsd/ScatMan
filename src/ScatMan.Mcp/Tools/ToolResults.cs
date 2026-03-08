using ScatMan.Core;

namespace ScatMan.Mcp.Tools;

sealed record GetTypesResult(
    string Package,
    string RequestedVersion,
    string Version,
    string? Namespace,
    string? Filter,
    int Count,
    IReadOnlyList<MappedType> Types)
    : PackageRequestedVersionResult(Package, RequestedVersion, Version);

sealed record SearchResult(
    string Package,
    string RequestedVersion,
    string Version,
    string Query,
    string? Namespace,
    IReadOnlyList<MappedType> MatchingTypes,
    IReadOnlyList<MappedMember> MatchingMembers)
    : PackageRequestedVersionResult(Package, RequestedVersion, Version);

sealed record GetMembersResult(
    string Package,
    string RequestedVersion,
    string Version,
    string TypeName,
    bool IncludeDefaultValues,
    bool IncludeAttributes,
    string? Kind,
    int Count,
    IReadOnlyList<MemberDetail> Members)
    : PackageRequestedVersionResult(Package, RequestedVersion, Version);

sealed record MappedType(
    string FullName,
    string Name,
    string Namespace,
    string Kind,
    string? Summary);

sealed record MappedMember(
    string TypeName,
    string TypeFullName,
    string Kind,
    string Name,
    string Signature,
    string? Summary);

sealed record MemberDetail(string Kind, string Name, string Signature, string? Summary);
