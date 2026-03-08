using System.Reflection;
using System.Text;

namespace ScatMan.Core;

/// <summary>
/// Inspects public API metadata from downloaded assemblies using MetadataLoadContext.
/// </summary>
public sealed class TypeInspector
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/

    /// <summary>
    /// Returns public instance constructors of a type.
    /// </summary>
    /// <param name="assemblyPaths">Assembly paths to inspect.</param>
    /// <param name="typeName">Full or simple type name.</param>
    /// <returns>Public constructor signatures.</returns>
    /// <exception cref="TypeNotFoundException">
    /// Thrown when the type cannot be resolved from the provided assemblies.
    /// </exception>
    public IReadOnlyList<ConstructorSignature> GetConstructors(
        IReadOnlyList<string> assemblyPaths, string typeName)
    {
        using var inspectionCtxt = new TypeInspectionContext(assemblyPaths);

        var type = FindType(inspectionCtxt.LoadCtxt, assemblyPaths, typeName)
            ?? throw new TypeNotFoundException(typeName);

        return [.. type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Select(c => new ConstructorSignature(
                [.. c.GetParameters().Select(p => new ParameterDescriptor(
                    p.Name ?? $"arg{p.Position}",
                    FormatTypeName(p.ParameterType)))],
                inspectionCtxt.DocProvider.GetMemberSummary(c)
            ))];
    }

    /// <summary>
    /// Returns public members of a type, optionally including defaults and attributes.
    /// </summary>
    /// <param name="assemblyPaths">Assembly paths to inspect.</param>
    /// <param name="typeName">Full or simple type name.</param>
    /// <param name="includeDefaultValues">Include default values for optional parameters.</param>
    /// <param name="includeAttributes">Include member and parameter attributes in signatures.</param>
    /// <param name="kind">Optional kind filter: constructor, method, property, field, event.</param>
    /// <returns>Public members declared on the resolved type.</returns>
    /// <exception cref="TypeNotFoundException">
    /// Thrown when the type cannot be resolved from the provided assemblies.
    /// </exception>
    public IReadOnlyList<MemberDescriptor> GetMembers(
        IReadOnlyList<string> assemblyPaths,
        string typeName,
        bool includeDefaultValues = true,
        bool includeAttributes = false,
        string? kind = null)
    {
        using var inspectionCtxt = new TypeInspectionContext(assemblyPaths);

        var type = FindType(inspectionCtxt.LoadCtxt, assemblyPaths, typeName)
            ?? throw new TypeNotFoundException(typeName);

        var members = GetMembersFromType(type, includeDefaultValues, includeAttributes, inspectionCtxt.DocProvider);

        return kind is null
            ? members
            : [.. members.Where(m => m.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Searches public type and member names by glob pattern or case-insensitive substring.
    /// </summary>
    /// <param name="assemblyPaths">Assembly paths to inspect.</param>
    /// <param name="query">Case-insensitive search text.</param>
    /// <param name="ns">Optional namespace filter.</param>
    /// <returns>Type and member matches.</returns>
    public SearchHits Search(IReadOnlyList<string> assemblyPaths, string query, string? ns = null)
    {
        using var inspectionCtxt = new TypeInspectionContext(assemblyPaths);

        var matchingTypes = new List<TypeDescriptor>();
        var matchingMembers = new List<MemberSearchHit>();

        try
        {
            var descriptors = GetTypes(assemblyPaths, ns, inspectionCtxt);

            foreach (var descriptor in descriptors)
            {
                if (PatternFilters.MatchesSubstringOrGlob(descriptor.Name, query))
                    matchingTypes.Add(descriptor);

                try
                {
                    var memberHits = GetMembersFromType(
                            descriptor.Type,
                            includeDefaultValues: true,
                            includeAttributes: false,
                            inspectionCtxt.DocProvider)
                        .Where(m => PatternFilters.MatchesSubstringOrGlob(m.Name, query))
                        .Select(m => new MemberSearchHit(descriptor.FullName, descriptor.Name, m))
                        .ToArray();

                    matchingMembers.AddRange(memberHits);
                }
                catch
                {
                    // ignored — type references unavailable assemblies
                }
            }
        }
        catch
        {
            // ignored
        }

        return new SearchHits(
            [.. matchingTypes.OrderBy(t => t.Namespace).ThenBy(t => t.Name)],
            [.. matchingMembers.OrderBy(h => h.TypeName).ThenBy(h => h.Member.Name)]);
    }

    /// <summary>
    /// Returns all public types, optionally restricted to one namespace.
    /// </summary>
    /// <param name="assemblyPaths">Assembly paths to inspect.</param>
    /// <param name="ns">Optional namespace filter.</param>
    /// <param name="inspectionCtxt">
    /// An optional shared inspection context. If none is given,
    /// a new context will be created and again disposed within this method.
    /// </param>
    /// <returns>Public type descriptors.</returns>
    public IReadOnlyList<TypeDescriptor> GetTypes(
        IReadOnlyList<string> assemblyPaths, string? ns = null,
        TypeInspectionContext? inspectionCtxt = null)
    {
        var localCtxt = inspectionCtxt ?? new TypeInspectionContext(assemblyPaths);
        var mlc = localCtxt.LoadCtxt;
        var docs = localCtxt.DocProvider;

        try
        {
            var types = new List<TypeDescriptor>();

            foreach (var asm in localCtxt.Assemblies)
            {
                try
                {
                    var allTypes = GetPublicTypesFrom(asm);

                    types.AddRange(allTypes
                        .Where(t =>
                            (t.IsPublic || t.IsNestedPublic) && NamespaceMatches(t.Namespace, ns))
                        .Select(t => new TypeDescriptor(
                            t, GetTypeKind(t), docs.GetTypeSummary(t))));
                }
                catch
                {
                    // ignored
                }
            }

            return [.. types.OrderBy(t => t.Namespace).ThenBy(t => t.Name)];
        }
        finally
        {
            if (inspectionCtxt is null)
            {
                localCtxt.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns public members of a type using a pre-existing inspection context.
    /// Intended for callers that manage context lifetime (e.g. ApiDiffer).
    /// </summary>
    internal IReadOnlyList<MemberDescriptor> GetMembers(TypeInspectionContext ctx, Type type) =>
        GetMembersFromType(type, includeDefaultValues: true, includeAttributes: false, ctx.DocProvider);

    static bool NamespaceMatches(string? typeNamespace, string? filterNamespace) =>
        PatternFilters.MatchesExactOrGlob(typeNamespace, filterNamespace);

    static Type[] GetPublicTypesFrom(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return [.. ex.Types.OfType<Type>()];
        }
    }

    static IReadOnlyList<MemberDescriptor> GetMembersFromType(
        Type type,
        bool includeDefaultValues,
        bool includeAttributes,
        XmlDocumentationProvider docs)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var results = new List<MemberDescriptor>();

        void TryAdd<T>(Func<IEnumerable<T>> getMembers, Func<T, MemberDescriptor> format)
        {
            IEnumerable<T> members;
            try { members = getMembers(); }
            catch { return; }

            foreach (var m in members)
            {
                try { results.Add(format(m)); }
                catch { /* member references unavailable assembly */ }
            }
        }

        TryAdd(() => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance),
             c => FormatConstructor(c, includeDefaultValues, includeAttributes, docs));
        TryAdd(() => type.GetProperties(flags),
             p => FormatProperty(p, includeAttributes, docs));
        TryAdd(() => type.GetMethods(flags).Where(m => !m.IsSpecialName),
             m => FormatMethod(m, includeDefaultValues, includeAttributes, docs));
        TryAdd(() => type.GetFields(flags).Where(f => !f.Name.StartsWith('<')),
             f => FormatField(f, includeAttributes, docs));
        TryAdd(() => type.GetEvents(flags),
             e => FormatEvent(e, includeAttributes, docs));

        return [.. results.OrderBy(d => d.Kind).ThenBy(d => d.Name)];
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
            catch
            {
                // ignored
            }
        }

        return null;
    }

    static string GetTypeKind(Type t) => t switch
    {
        { IsEnum: true } => "enum",
        { IsValueType: true } => "struct",
        { IsInterface: true } => "interface",
        _ when t.BaseType?.FullName == "System.MulticastDelegate" => "delegate",
        _ => "class"
    };

    static MemberDescriptor FormatConstructor(
        ConstructorInfo c,
        bool includeDefaultValues,
        bool includeAttributes,
        XmlDocumentationProvider docs)
    {
        ParameterInfo[] parms;
        try { parms = c.GetParameters(); }
        catch { parms = []; }

        var @params = string.Join(", ", parms
            .Select(p => FormatParameter(p, includeDefaultValues, includeAttributes)));

        var sig = $".ctor({@params})";
        if (includeAttributes)
            sig = $"{FormatAttributes(c)}{sig}";

        return new MemberDescriptor(".ctor", "constructor", sig, docs.GetMemberSummary(c),
            IsObsoleteMember(c));
    }

    static MemberDescriptor FormatProperty(
        PropertyInfo p,
        bool includeAttributes,
        XmlDocumentationProvider docs)
    {
        var isStatic = p.GetMethod?.IsStatic == true || p.SetMethod?.IsStatic == true;
        var accessors = (p.CanRead ? "get; " : "") + (p.CanWrite ? "set; " : "");
        var sig =
            $"{(isStatic ? "static " : "")}{SafeTypeName(() => p.PropertyType)} {p.Name} {{ {accessors}}}";

        if (includeAttributes)
            sig = $"{FormatAttributes(p)}{sig}";

        return new MemberDescriptor(p.Name, "property", sig, docs.GetMemberSummary(p),
            IsObsoleteMember(p));
    }

    static MemberDescriptor FormatMethod(
        MethodInfo m,
        bool includeDefaultValues,
        bool includeAttributes,
        XmlDocumentationProvider docs)
    {
        var prefix = m.IsStatic ? "static " : "";
        var returnType = SafeTypeName(() => m.ReturnType);
        var generics = m.IsGenericMethod
            ? $"<{string.Join(", ", m.GetGenericArguments().Select(t => t.Name))}>"
            : "";

        ParameterInfo[] parms;
        try { parms = m.GetParameters(); }
        catch { parms = []; }

        var @params = string.Join(", ", parms
            .Select(p => FormatParameter(p, includeDefaultValues, includeAttributes)));

        var sig = $"{prefix}{returnType} {m.Name}{generics}({@params})";
        if (includeAttributes)
            sig = $"{FormatAttributes(m)}{sig}";

        return new MemberDescriptor(m.Name, "method", sig, docs.GetMemberSummary(m),
            IsObsoleteMember(m));
    }

    static MemberDescriptor FormatField(
        FieldInfo f,
        bool includeAttributes,
        XmlDocumentationProvider docs)
    {
        var mods = (f.IsStatic ? "static " : "") + (f.IsInitOnly ? "readonly " : "");
        var sig = $"{mods}{SafeTypeName(() => f.FieldType)} {f.Name}";

        if (includeAttributes)
            sig = $"{FormatAttributes(f)}{sig}";

        return new MemberDescriptor(f.Name, "field", sig, docs.GetMemberSummary(f),
            IsObsoleteMember(f));
    }

    static MemberDescriptor FormatEvent(
        EventInfo e,
        bool includeAttributes,
        XmlDocumentationProvider docs)
    {
        var isStatic = e.AddMethod?.IsStatic == true;
        var typeName = SafeTypeName(() => e.EventHandlerType!);
        var sig = $"{(isStatic ? "static " : "")}event {typeName} {e.Name}";

        if (includeAttributes)
            sig = $"{FormatAttributes(e)}{sig}";

        return new MemberDescriptor(e.Name, "event", sig, docs.GetMemberSummary(e),
            IsObsoleteMember(e));
    }

    static bool IsObsoleteMember(MemberInfo member)
    {
        try
        {
            return CustomAttributeData.GetCustomAttributes(member)
                .Any(a => a.AttributeType.FullName == "System.ObsoleteAttribute");
        }
        catch { return false; }
    }

    static string FormatParameter(
        ParameterInfo parameter,
        bool includeDefaultValues,
        bool includeAttributes)
    {
        var name = parameter.Name ?? $"arg{parameter.Position}";
        var type = parameter.ParameterType;

        var isByRef = type.IsByRef;
        var valueType = isByRef ? type.GetElementType() ?? type : type;

        var prefix = parameter.IsOut ? "out " :
            isByRef && parameter.IsIn ? "in " :
            isByRef ? "ref " :
            HasAttribute(parameter, "System.ParamArrayAttribute") ? "params " :
            "";

        var formatted = $"{prefix}{SafeTypeName(() => valueType)} {name}";

        if (includeDefaultValues && parameter.IsOptional)
            formatted = $"{formatted} = {FormatDefaultValue(parameter, valueType)}";

        if (includeAttributes)
            formatted = $"{FormatAttributes(parameter)}{formatted}";

        return formatted;
    }

    static string FormatDefaultValue(ParameterInfo parameter, Type parameterType)
    {
        object? value;
        try { value = parameter.DefaultValue; }
        catch { return "default"; }

        if (value is null) return "null";

        if (value is DBNull)
            return "default";

        if (value.GetType().FullName == "System.Reflection.Missing")
            return "default";

        if (parameterType.IsEnum)
        {
            try
            {
                var enumName = Enum.GetName(parameterType, value);
                if (enumName is not null)
                    return $"{SafeTypeName(() => parameterType)}.{enumName}";
            }
            catch
            {
                // ignored
            }
        }

        return value switch
        {
            bool b => b ? "true" : "false",
            char c => $"'{EscapeChar(c)}'",
            string s => $"\"{EscapeString(s)}\"",
            float f => $"{f}F",
            double d => $"{d}D",
            decimal m => $"{m}M",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "default"
        };
    }

    static bool HasAttribute(ParameterInfo parameter, string fullName)
    {
        try
        {
            return CustomAttributeData.GetCustomAttributes(parameter)
                .Any(a => a.AttributeType.FullName == fullName);
        }
        catch
        {
            return false;
        }
    }

    static string FormatAttributes(MemberInfo member)
    {
        var attributes = GetAttributes(() => CustomAttributeData.GetCustomAttributes(member));
        return attributes.Count == 0 ? "" : $"{string.Join(" ", attributes)} ";
    }

    static string FormatAttributes(ParameterInfo parameter)
    {
        var attributes = GetAttributes(() => CustomAttributeData.GetCustomAttributes(parameter));
        return attributes.Count == 0 ? "" : $"{string.Join(" ", attributes)} ";
    }

    static IReadOnlyList<string> GetAttributes(
        Func<IList<CustomAttributeData>> getAttributes)
    {
        IList<CustomAttributeData> attributes;
        try { attributes = getAttributes(); }
        catch { return []; }

        return [.. attributes.Select(FormatAttribute)];
    }

    static string FormatAttribute(CustomAttributeData attribute)
    {
        var name = attribute.AttributeType.Name;
        if (name.EndsWith("Attribute", StringComparison.Ordinal))
            name = name[..^"Attribute".Length];

        var ctorArgs = attribute.ConstructorArguments.Select(FormatAttributeArgument);
        var namedArgs = attribute.NamedArguments
            .Select(a => $"{a.MemberName} = {FormatAttributeValue(a.TypedValue)}");

        var allArgs = ctorArgs.Concat(namedArgs).ToArray();
        if (allArgs.Length == 0)
            return $"[{name}]";

        return $"[{name}({string.Join(", ", allArgs)})]";
    }

    static string FormatAttributeArgument(CustomAttributeTypedArgument arg) =>
        FormatAttributeValue(arg);

    static string FormatAttributeValue(CustomAttributeTypedArgument arg)
    {
        var value = arg.Value;
        if (value is null) return "null";

        if (value is IReadOnlyCollection<CustomAttributeTypedArgument> items)
            return $"new[] {{ {string.Join(", ", items.Select(FormatAttributeValue))} }}";

        return value switch
        {
            bool b => b ? "true" : "false",
            char c => $"'{EscapeChar(c)}'",
            string s => $"\"{EscapeString(s)}\"",
            Type t => $"typeof({SafeTypeName(() => t)})",
            _ => FormatFormattable(value)
        };
    }

    static string FormatFormattable(object value)
    {
        if (value.GetType().IsEnum)
            return value.ToString() ?? "0";

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
            ?? value.ToString()
            ?? "?";
    }

    static string EscapeString(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var c in value)
            builder.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c.ToString()
            });

        return builder.ToString();
    }

    static string EscapeChar(char value) => value switch
    {
        '\\' => "\\\\",
        '\'' => "\\'",
        '\n' => "\\n",
        '\r' => "\\r",
        '\t' => "\\t",
        _ => value.ToString()
    };

    static string SafeTypeName(Func<Type> getType)
    {
        try { return FormatTypeName(getType()); }
        catch { return "?"; }
    }

    static string FormatTypeName(Type t)
    {
        if (t.IsGenericParameter) return t.Name;
        if (t.IsArray) return $"{FormatTypeName(t.GetElementType()!)}[]";

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
            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Int16" => "short",
            "System.UInt16" => "ushort",
            "System.Int32" => "int",
            "System.UInt32" => "uint",
            "System.Int64" => "long",
            "System.UInt64" => "ulong",
            "System.Single" => "float",
            "System.Double" => "double",
            "System.Decimal" => "decimal",
            "System.Char" => "char",
            "System.String" => "string",
            "System.Object" => "object",
            "System.Void" => "void",
            _ => t.Name
        };
    }
}