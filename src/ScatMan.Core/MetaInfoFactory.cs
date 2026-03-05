namespace ScatMan.Core;

/// <summary>
/// Creates metadata snapshots describing the current build and runtime environment.
/// </summary>
public static class MetaInfoFactory
{
    /// <summary>
    /// Creates the current metadata snapshot.
    /// </summary>
    public static MetaInfo Create() =>
        new(ThisAssembly.AssemblyInformationalVersion,
            ThisAssembly.AssemblyConfiguration,
            ThisAssembly.GitCommitDate,
            ThisAssembly.IsPublicRelease,
            Environment.OSVersion.ToString(),
            Environment.Version.ToString());
}