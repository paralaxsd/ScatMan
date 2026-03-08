using Shouldly;
#pragma warning disable IDE0051
using Xunit;

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

    // Test: GetConstructors returns XML summary for a type
    [Fact]
    public void GetConstructors_ReturnsXmlSummary()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyCtorType).Assembly.Location;
        var ctors = inspector.GetConstructors([assembly], nameof(DummyCtorType));

        // Finde den Konstruktor mit Parameter
        var ctorWithParam = ctors.FirstOrDefault(c => c.Parameters.Count == 1);
        ctorWithParam.ShouldNotBeNull();
        ctorWithParam.Summary.ShouldNotBeNull();
        ctorWithParam.Summary.ShouldContain("Konstruktor mit Parameter");
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

    // Test: GetMembers filters by kind
    [Fact]
    public void GetMembers_FiltersByKind_Methods()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyMemberType).Assembly.Location;
        var result = inspector.GetMembers([assembly], nameof(DummyMemberType), kind: "method");
        result.ShouldAllBe(m => m.Kind == "method");
        result.ShouldContain(m => m.Name == "PublicMethod");
    }

    [Fact]
    public void GetMembers_FiltersByKind_Properties()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyMemberType).Assembly.Location;
        var result = inspector.GetMembers([assembly], nameof(DummyMemberType), kind: "property");
        result.ShouldAllBe(m => m.Kind == "property");
        result.ShouldContain(m => m.Name == "PublicProp");
    }

    [Fact]
    public void GetMembers_FiltersByKind_CaseInsensitive()
    {
        var inspector = new TypeInspector();
        var assembly = typeof(DummyMemberType).Assembly.Location;
        var resultLower = inspector.GetMembers([assembly], nameof(DummyMemberType), kind: "method");
        var resultUpper = inspector.GetMembers([assembly], nameof(DummyMemberType), kind: "METHOD");
        resultLower.ShouldBe(resultUpper);
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
    /// <summary>
    /// Dummy-Typ für Konstruktor-Test
    /// </summary>
    public sealed class DummyCtorType
    {
        /// <summary>
        /// Standard-Konstruktor für DummyCtorType
        /// </summary>
        public DummyCtorType() {}

        /// <summary>
        /// Konstruktor mit Parameter
        /// </summary>
        /// <param name="s">Test-Parameter</param>
        public DummyCtorType(string s) {}
    }

    public sealed class DummyMemberType
    {
        public int PublicProp { get; set; }
        private int PrivateProp { get; set; }

        public void PublicMethod() { }
    }
}

