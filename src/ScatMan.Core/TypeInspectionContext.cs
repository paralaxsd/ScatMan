using System.Reflection;
using System.Runtime.InteropServices;

namespace ScatMan.Core;

public sealed class TypeInspectionContext : IDisposable
{
    /******************************************************************************************
     * STRUCTORS
     * ***************************************************************************************/
    public TypeInspectionContext(IReadOnlyList<string> assemblyPaths)
    {
        LoadCtxt = CreateContext(assemblyPaths);
        DocProvider = XmlDocumentationProvider.Load(assemblyPaths);
        AssemblyPaths = assemblyPaths;
    }

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public IEnumerable<Assembly> Assemblies =>
        AssemblyPaths.Select(path => LoadCtxt.LoadFromAssemblyPath(path));

    public MetadataLoadContext LoadCtxt { get; init; }
    public XmlDocumentationProvider DocProvider { get; init; }
    public IReadOnlyList<string> AssemblyPaths { get; init; }

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public void Dispose() => LoadCtxt.Dispose();

    static MetadataLoadContext CreateContext(IReadOnlyList<string> assemblyPaths)
    {
        var runtimeDlls = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        var resolver = new PathAssemblyResolver([.. assemblyPaths, .. runtimeDlls]);
        return new(resolver);
    }
}