namespace ScatMan.Core;

/// <summary>
/// Compares the public API of two versions of a NuGet package and returns the differences.
/// </summary>
public sealed class ApiDiffer
{
    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/

    /// <summary>
    /// Diffs the public API between two sets of assemblies for the same package.
    /// </summary>
    /// <param name="package">Package ID (used for result metadata only).</param>
    /// <param name="version1">Version string for the first assembly set.</param>
    /// <param name="assembliesV1">Assembly paths for version 1.</param>
    /// <param name="version2">Version string for the second assembly set.</param>
    /// <param name="assembliesV2">Assembly paths for version 2.</param>
    /// <param name="typeName">Optional type filter (full or simple name).</param>
    /// <returns>The API differences between the two versions.</returns>
    public ApiDiff Diff(
        string package,
        string version1, IReadOnlyList<string> assembliesV1,
        string version2, IReadOnlyList<string> assembliesV2,
        string? typeName = null)
    {
        var inspector = new TypeInspector();

        using var ctx1 = new TypeInspectionContext(assembliesV1);
        using var ctx2 = new TypeInspectionContext(assembliesV2);

        var types1 = inspector.GetTypes(assembliesV1, inspectionCtxt: ctx1);
        var types2 = inspector.GetTypes(assembliesV2, inspectionCtxt: ctx2);

        if (typeName is not null)
        {
            types1 = [.. types1.Where(t => MatchesType(t, typeName))];
            types2 = [.. types2.Where(t => MatchesType(t, typeName))];
        }

        var map1 = types1.ToDictionary(t => t.FullName);
        var map2 = types2.ToDictionary(t => t.FullName);

        var addedTypes   = map2.Keys.Except(map1.Keys).Order().ToArray();
        var removedTypes = map1.Keys.Except(map2.Keys).Order().ToArray();
        var commonNames  = map1.Keys.Intersect(map2.Keys).Order();

        var typeDiffs = new List<TypeDiff>();

        foreach (var name in commonNames)
        {
            IReadOnlyList<MemberDescriptor> members1, members2;
            try
            {
                members1 = inspector.GetMembers(ctx1, map1[name].Type);
                members2 = inspector.GetMembers(ctx2, map2[name].Type);
            }
            catch
            {
                continue;
            }

            var sigSet1 = members1.Select(m => m.Signature).ToHashSet();
            var sigSet2 = members2.Select(m => m.Signature).ToHashSet();

            var added   = members2.Where(m => !sigSet1.Contains(m.Signature)).ToArray();
            var removed = members1.Where(m => !sigSet2.Contains(m.Signature)).ToArray();

            if (added.Length > 0 || removed.Length > 0)
                typeDiffs.Add(new TypeDiff(name, added, removed));
        }

        return new ApiDiff(package, version1, version2, addedTypes, removedTypes, typeDiffs);
    }

    static bool MatchesType(TypeDescriptor t, string name) =>
        t.Name == name || t.FullName == name;
}
