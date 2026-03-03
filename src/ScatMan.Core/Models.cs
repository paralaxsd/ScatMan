namespace ScatMan.Core;

public record ParameterDescriptor(string Name, string TypeName);
public record ConstructorSignature(IReadOnlyList<ParameterDescriptor> Parameters);
public record MemberDescriptor(string Name, string Kind, string Signature);
public record TypeDescriptor(string FullName, string Name, string Namespace, string Kind);

public record PackageVersionInfo(string Version, DateTimeOffset Published, bool IsPrerelease);

public record MemberSearchHit(string TypeFullName, string TypeName, MemberDescriptor Member);

public record SearchHits(IReadOnlyList<TypeDescriptor> Types, IReadOnlyList<MemberSearchHit> Members);

public sealed class TypeNotFoundException(string typeName)
    : Exception($"Type '{typeName}' not found in the downloaded assemblies.");

public sealed class PackageNotFoundException(string packageId)
    : Exception($"Package '{packageId}' not found on NuGet.");
