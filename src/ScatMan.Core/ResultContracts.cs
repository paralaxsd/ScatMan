namespace ScatMan.Core;

public abstract record PackageVersionResult(string Package, string Version);

public abstract record PackageRequestedVersionResult(
    string Package,
    string RequestedVersion,
    string Version);
