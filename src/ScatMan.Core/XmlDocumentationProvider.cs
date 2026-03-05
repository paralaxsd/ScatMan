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

                var memberBody = name[2..];
                var parenIndex = memberBody.IndexOf('(');
                if (parenIndex >= 0)
                    memberBody = memberBody[..parenIndex];

                memberSummaries.TryAdd(memberBody, summary);
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

        var key = member switch
        {
            ConstructorInfo c => $"{baseTypeName}.{(c.IsStatic ? "#cctor" : "#ctor")}",
            MethodInfo m when m.IsGenericMethod =>
                $"{baseTypeName}.{m.Name}``{m.GetGenericArguments().Length}",
            MethodInfo m => $"{baseTypeName}.{m.Name}",
            PropertyInfo p => $"{baseTypeName}.{p.Name}",
            FieldInfo f => $"{baseTypeName}.{f.Name}",
            EventInfo e => $"{baseTypeName}.{e.Name}",
            _ => null
        };

        if (key is null)
            return null;

        if (_memberSummaries.TryGetValue(key, out var summary))
            return summary;

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
