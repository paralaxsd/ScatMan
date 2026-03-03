namespace ScatMan.Core;

public static class MetaInfoFactory
{
    public static MetaInfo Create() =>
        new(ThisAssembly.AssemblyInformationalVersion,
            ThisAssembly.AssemblyConfiguration,
            ThisAssembly.GitCommitDate,
            ThisAssembly.IsPublicRelease,
            Environment.OSVersion.ToString(),
            Environment.Version.ToString());
}