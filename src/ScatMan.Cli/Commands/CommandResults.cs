using ScatMan.Core;

namespace ScatMan.Cli.Commands;

sealed record TypesResult(
    string Package,
    string Version,
    string? Namespace,
    string? Filter,
    IReadOnlyList<TypeResult> Types)
    : PackageVersionResult(Package, Version);

sealed record TypeResult(string FullName, string Kind, string? Summary);

sealed record SearchResult(
    string Package,
    string Version,
    string Query,
    string? Namespace,
    IReadOnlyList<SearchTypeResult> MatchingTypes,
    IReadOnlyList<SearchMemberResult> MatchingMembers)
    : PackageVersionResult(Package, Version);

sealed record SearchTypeResult(string FullName, string Kind, string? Summary);

sealed record SearchMemberResult(
    string TypeName,
    string Kind,
    string Signature,
    string? Summary);

sealed record MembersResult(
    string Package,
    string Version,
    string TypeName,
    IReadOnlyList<MemberResult> Members)
    : PackageVersionResult(Package, Version);

sealed record MemberResult(string Name, string Kind, string Signature, string? Summary);

sealed record CtorsResult(
    string Package,
    string Version,
    string TypeName,
    IReadOnlyList<ConstructorResult> Constructors)
    : PackageVersionResult(Package, Version);

sealed record ConstructorResult(IReadOnlyList<ParameterResult> Parameters);

sealed record ParameterResult(string Name, string TypeName);
