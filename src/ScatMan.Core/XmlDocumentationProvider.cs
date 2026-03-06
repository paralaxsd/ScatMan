using System.Reflection;
using System.Xml.Linq;

namespace ScatMan.Core;

public sealed class XmlDocumentationProvider
{
    readonly IReadOnlyDictionary<string, string> _typeSummaries;
    readonly IReadOnlyDictionary<string, string> _memberSummaries;

    XmlDocumentationProvider(
        IReadOnlyDictionary<string, string> typeSummaries,
        IReadOnlyDictionary<string, string> memberSummaries)
    {
        _typeSummaries = typeSummaries;
        _memberSummaries = memberSummaries;
    }

    public static XmlDocumentationProvider Load(IReadOnlyList<string> assemblyPaths)
    {
        var typeSummaries = new Dictionary<string, string>(StringComparer.Ordinal);
        var memberSummaries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var xmlPath in assemblyPaths
            .Select(p => Path.ChangeExtension(p, ".xml"))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            XDocument doc;
            try { doc = XDocument.Load(xmlPath); }
            catch { continue; }

            foreach (var memberElement in doc.Descendants("member"))
            {
                var name = memberElement.Attribute("name")?.Value;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var summary = Normalize(memberElement.Element("summary")?.Value);
                if (string.IsNullOrWhiteSpace(summary))
                    continue;

                if (name.StartsWith("T:", StringComparison.Ordinal))
                {
                    var key = name[2..];
                    typeSummaries.TryAdd(key, summary);
                    continue;
                }

                if (name.Length < 3 || name[1] != ':')
                    continue;

                // Use full member signature (including parameter list) as key
                var memberKey = name[2..];
                memberSummaries.TryAdd(memberKey, summary);
            }
        }

        return new XmlDocumentationProvider(typeSummaries, memberSummaries);
    }

    public string? GetTypeSummary(Type type)
    {
        var fullName = type.FullName;
        if (fullName is null)
            return null;

        var key = fullName.Replace('+', '.');
        return _typeSummaries.GetValueOrDefault(key);
    }

    public string? GetMemberSummary(MemberInfo member)
    {
        var typeName = member.DeclaringType?.FullName;
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var baseTypeName = typeName.Replace('+', '.');

        string? key = null;
        if (member is ConstructorInfo ctor)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0)
                key = $"{baseTypeName}.#ctor";
            else
            {
                var paramTypes = string.Join(",", parameters.Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));
                key = $"{baseTypeName}.#ctor({paramTypes})";
            }
        }
        else if (member is MethodInfo m)
        {
            if (m.IsGenericMethod)
                key = $"{baseTypeName}.{m.Name}``{m.GetGenericArguments().Length}";
            else
                key = $"{baseTypeName}.{m.Name}";
        }
        else if (member is PropertyInfo p)
            key = $"{baseTypeName}.{p.Name}";
        else if (member is FieldInfo f)
            key = $"{baseTypeName}.{f.Name}";
        else if (member is EventInfo e)
            key = $"{baseTypeName}.{e.Name}";

        if (key is null)
            return null;

        if (_memberSummaries.TryGetValue(key, out var summary))
            return summary;

        // Fallback: for constructors without FullName (e.g. generic types)
        if (member is ConstructorInfo ctor2 && ctor2.GetParameters().Length > 0)
        {
            var paramTypes = string.Join(",", ctor2.GetParameters().Select(p => p.ParameterType.Name));
            var fallbackKey = $"{baseTypeName}.#ctor({paramTypes})";
            return _memberSummaries.GetValueOrDefault(fallbackKey);
        }

        if (member is MethodInfo method && method.IsGenericMethod)
        {
            var fallback = $"{baseTypeName}.{method.Name}";
            return _memberSummaries.GetValueOrDefault(fallback);
        }

        return null;
    }

    static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        return lines.Length == 0 ? null : string.Join(" ", lines);
    }
}
