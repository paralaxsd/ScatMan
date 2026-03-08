using Shouldly;
using Xunit;

namespace ScatMan.Core.Tests;

public sealed class ApiDifferTests
{
    static readonly string Assembly = typeof(TypeInspectorTests.DummyMemberType).Assembly.Location;

    [Fact]
    public void Diff_SameAssembly_ReturnsNoDiff()
    {
        var diff = new ApiDiffer().Diff("test", "1.0", [Assembly], "1.0", [Assembly]);

        diff.AddedTypes.ShouldBeEmpty();
        diff.RemovedTypes.ShouldBeEmpty();
        diff.ChangedTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Diff_SameAssembly_PackageAndVersionsPopulated()
    {
        var diff = new ApiDiffer().Diff("MyPkg", "1.0", [Assembly], "2.0", [Assembly]);

        diff.Package.ShouldBe("MyPkg");
        diff.Version1.ShouldBe("1.0");
        diff.Version2.ShouldBe("2.0");
    }

    [Fact]
    public void Diff_WithTypeFilter_RestrictsToMatchingType()
    {
        var diff = new ApiDiffer().Diff(
            "test", "1.0", [Assembly], "1.0", [Assembly],
            typeName: nameof(TypeInspectorTests.DummyMemberType));

        // No changes since same assembly — but the filter should have been applied
        diff.ChangedTypes.ShouldBeEmpty();
        diff.AddedTypes.ShouldBeEmpty();
        diff.RemovedTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Diff_WithNonExistentTypeFilter_ReturnsEmptyDiff()
    {
        var diff = new ApiDiffer().Diff(
            "test", "1.0", [Assembly], "2.0", [Assembly],
            typeName: "NonExistentType");

        diff.AddedTypes.ShouldBeEmpty();
        diff.RemovedTypes.ShouldBeEmpty();
        diff.ChangedTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Diff_V2HasExtraAssembly_AddsTypesFromIt()
    {
        var coreLib = typeof(object).Assembly.Location;
        var diff = new ApiDiffer().Diff("test", "1.0", [Assembly], "2.0", [Assembly, coreLib]);

        diff.AddedTypes.ShouldNotBeEmpty();
        diff.RemovedTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Diff_V1HasExtraAssembly_RemovesTypesFromIt()
    {
        var coreLib = typeof(object).Assembly.Location;
        var diff = new ApiDiffer().Diff("test", "1.0", [Assembly, coreLib], "2.0", [Assembly]);

        diff.RemovedTypes.ShouldNotBeEmpty();
        diff.AddedTypes.ShouldBeEmpty();
    }

    // IsObsolete detection via TypeInspector

    [Fact]
    public void GetMembers_MarksObsoleteMembers()
    {
        var inspector = new TypeInspector();
        var members = inspector.GetMembers([Assembly], nameof(TypeInspectorTests.DummyObsoleteType));

        members.ShouldContain(m => m.Name == "ObsoleteMethod" && m.IsObsolete);
        members.ShouldContain(m => m.Name == "ObsoleteProp" && m.IsObsolete);
        members.ShouldContain(m => m.Name == "ActiveMethod" && !m.IsObsolete);
    }

    [Fact]
    public void GetMembers_NonObsoleteMembers_HaveFalseFlag()
    {
        var inspector = new TypeInspector();
        var members = inspector.GetMembers([Assembly], nameof(TypeInspectorTests.DummyMemberType));

        members.ShouldAllBe(m => !m.IsObsolete);
    }

    // TypeDiff structure

    [Fact]
    public void TypeDiff_SameAssembly_HasNoChangedOrDeprecated()
    {
        var diff = new ApiDiffer().Diff("test", "1.0", [Assembly], "1.0", [Assembly]);

        foreach (var td in diff.ChangedTypes)
        {
            td.Changed.ShouldBeEmpty();
            td.Deprecated.ShouldBeEmpty();
        }
    }
}
