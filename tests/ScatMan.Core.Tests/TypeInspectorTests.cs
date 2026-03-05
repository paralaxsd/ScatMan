using Shouldly;
#pragma warning disable IDE0051

namespace ScatMan.Core.Tests;

public sealed class TypeInspectorTests
{
    // Test: GetConstructors returns correct signatures for a simple type
    [Fact]
    public void GetConstructors_ReturnsExpectedSignatures()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyCtorType).Assembly.Location;
        var result = inspector.GetConstructors([assembly], nameof(DummyCtorType));
        result.Count.ShouldBe(2);
        result[0].Parameters.Count.ShouldBe(0);
        result[1].Parameters.Count.ShouldBe(1);
        result[1].Parameters[0].TypeName.ShouldBe("string");
    }

    // Test: GetMembers returns public members only
    [Fact]
    public void GetMembers_ReturnsPublicMembers()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyMemberType).Assembly.Location;
        var result = inspector.GetMembers([assembly], nameof(DummyMemberType));
        result.ShouldContain(m => m.Name == "PublicProp");
        result.ShouldNotContain(m => m.Name == "PrivateProp");
    }

    // Test: Search finds types and members by substring and glob
    [Fact]
    public void Search_FindsBySubstringAndGlob()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyCtorType).Assembly.Location;
        var hits = inspector.Search([assembly], "Dummy*");
        hits.Types.ShouldContain(t => t.Name == "DummyCtorType");
        hits.Types.ShouldContain(t => t.Name == "DummyMemberType");
        // Member-Treffer sind bei Query "Dummy*" nicht zu erwarten
    }

    [Fact]
    public void Search_FindsPublicPropMember()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyMemberType).Assembly.Location;
        var hits = inspector.Search([assembly], "PublicProp");
        hits.Members.ShouldContain(m => m.Member.Name == "PublicProp");
    }

    // Test: GetTypes returns all public types in assembly
    [Fact]
    public void GetTypes_ReturnsAllPublicTypes()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyCtorType).Assembly.Location;
        var types = inspector.GetTypes([assembly]);
        types.ShouldContain(t => t.Name == "DummyCtorType");
        types.ShouldContain(t => t.Name == "DummyMemberType");
    }

    // Dummy types for testing
    public sealed class DummyCtorType
    {
        public DummyCtorType() {}
        // ReSharper disable once UnusedParameter.Local
        public DummyCtorType(string s) {}
    }

    public sealed class DummyMemberType
    {
        public int PublicProp { get; set; }
        private int PrivateProp { get; set; }
    }
}
