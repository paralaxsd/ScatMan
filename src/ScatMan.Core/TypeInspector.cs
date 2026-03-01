using System.Reflection;
using System.Runtime.InteropServices;

namespace ScatMan.Core;

public sealed class TypeInspector
{
    public IReadOnlyList<ConstructorSignature> GetConstructors(
        IReadOnlyList<string> assemblyPaths, string typeName)
    {
        using var mlc = CreateContext(assemblyPaths);

        var type = FindType(mlc, assemblyPaths, typeName)
            ?? throw new TypeNotFoundException(typeName);

        return [.. type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Select(c => new ConstructorSignature(
                [.. c.GetParameters().Select(p => new ParameterDescriptor(
                    p.Name ?? $"arg{p.Position}",
                    FormatTypeName(p.ParameterType)))]))];
    }

    public IReadOnlyList<MemberDescriptor> GetMembers(
        IReadOnlyList<string> assemblyPaths, string typeName)
    {
        using var mlc = CreateContext(assemblyPaths);

        var type = FindType(mlc, assemblyPaths, typeName)
            ?? throw new TypeNotFoundException(typeName);

        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var props   = type.GetProperties(flags).Select(FormatProperty);
        var methods = type.GetMethods(flags).Where(m => !m.IsSpecialName).Select(FormatMethod);
        var fields  = type.GetFields(flags).Where(f => !f.Name.StartsWith('<')).Select(FormatField);
        var events  = type.GetEvents(flags).Select(FormatEvent);

        return [.. props.Concat(methods).Concat(fields).Concat(events)
                        .OrderBy(d => d.Kind).ThenBy(d => d.Name)];
    }

    public IReadOnlyList<TypeDescriptor> GetTypes(
        IReadOnlyList<string> assemblyPaths, string? ns = null)
    {
        using var mlc = CreateContext(assemblyPaths);

        var types = new List<TypeDescriptor>();

        foreach (var path in assemblyPaths)
        {
            try
            {
                var asm = mlc.LoadFromAssemblyPath(path);

                Type[] allTypes;
                try   { allTypes = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { allTypes = [.. ex.Types.OfType<Type>()]; }

                types.AddRange(allTypes
                    .Where(t => t.IsPublic && (ns is null || t.Namespace == ns))
                    .Select(t => new TypeDescriptor(
                        (t.FullName ?? t.Name).Replace('+', '.'),
                        t.Name,
                        t.Namespace ?? "",
                        GetTypeKind(t))));
            }
            catch { }
        }

        return [.. types.OrderBy(t => t.Namespace).ThenBy(t => t.Name)];
    }

    static MetadataLoadContext CreateContext(IReadOnlyList<string> assemblyPaths)
    {
        var runtimeDlls = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        var resolver    = new PathAssemblyResolver([.. assemblyPaths, .. runtimeDlls]);
        return new MetadataLoadContext(resolver);
    }

    static Type? FindType(MetadataLoadContext mlc, IReadOnlyList<string> assemblyPaths, string typeName)
    {
        foreach (var path in assemblyPaths)
        {
            try
            {
                var asm  = mlc.LoadFromAssemblyPath(path);
                var type = asm.GetType(typeName)
                    ?? asm.GetTypes().FirstOrDefault(t =>
                        t.Name == typeName || t.FullName == typeName);

                if (type is not null) return type;
            }
            catch { }
        }

        return null;
    }

    static string GetTypeKind(Type t) => t switch
    {
        { IsEnum: true }      => "enum",
        { IsValueType: true } => "struct",
        { IsInterface: true } => "interface",
        _ when t.BaseType?.FullName == "System.MulticastDelegate" => "delegate",
        _ => "class"
    };

    static MemberDescriptor FormatProperty(PropertyInfo p)
    {
        var isStatic  = p.GetMethod?.IsStatic == true || p.SetMethod?.IsStatic == true;
        var accessors = (p.CanRead ? "get; " : "") + (p.CanWrite ? "set; " : "");
        var sig = $"{(isStatic ? "static " : "")}{FormatTypeName(p.PropertyType)} {p.Name} {{ {accessors}}}";
        return new MemberDescriptor(p.Name, "property", sig);
    }

    static MemberDescriptor FormatMethod(MethodInfo m)
    {
        var prefix     = m.IsStatic ? "static " : "";
        var returnType = FormatTypeName(m.ReturnType);
        var generics   = m.IsGenericMethod
            ? $"<{string.Join(", ", m.GetGenericArguments().Select(t => t.Name))}>"
            : "";
        var @params = string.Join(", ", m.GetParameters()
            .Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name ?? $"arg{p.Position}"}"));
        return new MemberDescriptor(m.Name, "method", $"{prefix}{returnType} {m.Name}{generics}({@params})");
    }

    static MemberDescriptor FormatField(FieldInfo f)
    {
        var mods = (f.IsStatic ? "static " : "") + (f.IsInitOnly ? "readonly " : "");
        return new MemberDescriptor(f.Name, "field", $"{mods}{FormatTypeName(f.FieldType)} {f.Name}");
    }

    static MemberDescriptor FormatEvent(EventInfo e)
    {
        var isStatic = e.AddMethod?.IsStatic == true;
        var typeName = e.EventHandlerType is { } t ? FormatTypeName(t) : "?";
        return new MemberDescriptor(e.Name, "event", $"{(isStatic ? "static " : "")}event {typeName} {e.Name}");
    }

    static string FormatTypeName(Type t)
    {
        if (t.IsGenericParameter) return t.Name;
        if (t.IsArray)            return $"{FormatTypeName(t.GetElementType()!)}[]";

        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def.FullName == "System.Nullable`1")
                return $"{FormatTypeName(t.GetGenericArguments()[0])}?";

            var name = t.Name[..t.Name.IndexOf('`')];
            var args = string.Join(", ", t.GetGenericArguments().Select(FormatTypeName));
            return $"{name}<{args}>";
        }

        return t.FullName switch
        {
            "System.Boolean" => "bool",
            "System.Byte"    => "byte",
            "System.SByte"   => "sbyte",
            "System.Int16"   => "short",
            "System.UInt16"  => "ushort",
            "System.Int32"   => "int",
            "System.UInt32"  => "uint",
            "System.Int64"   => "long",
            "System.UInt64"  => "ulong",
            "System.Single"  => "float",
            "System.Double"  => "double",
            "System.Decimal" => "decimal",
            "System.Char"    => "char",
            "System.String"  => "string",
            "System.Object"  => "object",
            "System.Void"    => "void",
            _ => t.Name
        };
    }
}
