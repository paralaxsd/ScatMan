namespace ScatMan.Core;

public record ParameterDescriptor(string Name, string TypeName);
public record ConstructorSignature(IReadOnlyList<ParameterDescriptor> Parameters);
public record MemberDescriptor(string Name, string Kind, string Signature);
public record TypeDescriptor(string FullName, string Name, string Namespace, string Kind);

public sealed class TypeNotFoundException(string typeName)
    : Exception($"Type '{typeName}' not found in the downloaded assemblies.");
