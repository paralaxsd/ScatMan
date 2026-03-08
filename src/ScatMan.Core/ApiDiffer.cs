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

            var (added, removed, changed, deprecated) = DiffMembers(members1, members2);
            if (added.Count > 0 || removed.Count > 0 || changed.Count > 0 || deprecated.Count > 0)
                typeDiffs.Add(new TypeDiff(name, added, removed, changed, deprecated));
        }

        return new ApiDiff(package, version1, version2, addedTypes, removedTypes, typeDiffs);
    }

    static (
        IReadOnlyList<MemberDescriptor> Added,
        IReadOnlyList<MemberDescriptor> Removed,
        IReadOnlyList<ChangedMember> Changed,
        IReadOnlyList<MemberDescriptor> Deprecated)
        DiffMembers(IReadOnlyList<MemberDescriptor> v1, IReadOnlyList<MemberDescriptor> v2)
    {
        var sigSet1 = v1.Select(m => m.Signature).ToHashSet();
        var sigSet2 = v2.Select(m => m.Signature).ToHashSet();

        // Purely added/removed by exact signature
        var pureAdded   = v2.Where(m => !sigSet1.Contains(m.Signature)).ToArray();
        var pureRemoved = v1.Where(m => !sigSet2.Contains(m.Signature)).ToArray();

        // Changed = same name, single overload each, different signature
        var addedByName   = pureAdded.GroupBy(m => m.Name)
            .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
        var removedByName = pureRemoved.GroupBy(m => m.Name)
            .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());

        var changedNames = addedByName.Keys.Intersect(removedByName.Keys).ToHashSet();
        var changed = changedNames
            .Select(n => new ChangedMember(
                n, removedByName[n].Kind, removedByName[n].Signature, addedByName[n].Signature))
            .OrderBy(c => c.Name)
            .ToArray();

        // Deprecated = existed in v1 without [Obsolete], still exists in v2 with [Obsolete]
        var obsSet1 = v1.Where(m => m.IsObsolete).Select(m => m.Signature).ToHashSet();
        var deprecated = v2
            .Where(m => m.IsObsolete && sigSet1.Contains(m.Signature) && !obsSet1.Contains(m.Signature))
            .ToArray();

        // Exclude "changed" members from added/removed
        var added   = pureAdded.Where(m => !changedNames.Contains(m.Name)).ToArray();
        var removed = pureRemoved.Where(m => !changedNames.Contains(m.Name)).ToArray();

        return (added, removed, changed, deprecated);
    }

    static bool MatchesType(TypeDescriptor t, string name) =>
        t.Name == name || t.FullName == name;
}
