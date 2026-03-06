using System.Reflection;
using System.Runtime.InteropServices;

namespace ScatMan.Core;

/// <summary>
/// Encapsulates the context for inspecting types from a set of assemblies, including the
/// MetadataLoadContext for loading assemblies and an XmlDocumentationProvider for accessing type
/// and member summaries from XML documentation files.<br/>
/// Implements IDisposable to ensure proper disposal of the MetadataLoadContext.
/// </summary>
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