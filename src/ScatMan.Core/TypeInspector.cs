using System.Reflection;
using System.Runtime.InteropServices;

namespace ScatMan.Core;

public sealed class TypeInspector
{
    public IReadOnlyList<ConstructorSignature> GetConstructors(
        IReadOnlyList<string> assemblyPaths, string typeName)
    {
        var runtimeDlls = Directory.GetFiles(
            RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

        var resolver = new PathAssemblyResolver([.. assemblyPaths, .. runtimeDlls]);
        using var mlc = new MetadataLoadContext(resolver);

        var type = FindType(mlc, assemblyPaths, typeName)
            ?? throw new TypeNotFoundException(typeName);

        // Extract all data while context is still alive
        return [.. type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Select(c => new ConstructorSignature(
                [.. c.GetParameters().Select(p => new ParameterDescriptor(
                    p.Name ?? $"arg{p.Position}",
                    FormatTypeName(p.ParameterType)))]))];
    }

    static Type? FindType(MetadataLoadContext mlc, IReadOnlyList<string> assemblyPaths, string typeName)
    {
        foreach (var path in assemblyPaths)
        {
            try
            {
                var asm = mlc.LoadFromAssemblyPath(path);
                var type = asm.GetType(typeName)
                    ?? asm.GetTypes().FirstOrDefault(t =>
                        t.Name == typeName || t.FullName == typeName);

                if (type is not null) return type;
            }
            catch { /* assembly may not load cleanly — skip */ }
        }

        return null;
    }

    static string FormatTypeName(Type t)
    {
        if (!t.IsGenericType) return t.Name;

        var name = t.Name[..t.Name.IndexOf('`')];
        var args = string.Join(", ", t.GetGenericArguments().Select(FormatTypeName));
        return $"{name}<{args}>";
    }
}
