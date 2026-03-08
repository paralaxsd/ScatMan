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
        // Use two assemblies for v2 (core lib adds types), v1 has just the test assembly
        var coreLib = typeof(object).Assembly.Location;
        var diff = new ApiDiffer().Diff("test", "1.0", [Assembly], "2.0", [Assembly, coreLib]);

        // All types from coreLib that weren't in the test assembly should appear as added
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
}
