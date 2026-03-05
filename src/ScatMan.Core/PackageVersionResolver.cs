namespace ScatMan.Core;

public static class PackageVersionResolver
{
    public static async Task<string> ResolveAsync(
        string packageId,
        string requestedVersion,
        CancellationToken ct = default)
    {
        if (!requestedVersion.Equals("latest", StringComparison.OrdinalIgnoreCase) &&
            !requestedVersion.Equals("latest-pre", StringComparison.OrdinalIgnoreCase))
            return requestedVersion;

        var versions = await new NuGetRegistrationClient().GetVersionsAsync(packageId, ct);
        if (versions.Count == 0)
            throw new PackageNotFoundException(packageId);

        if (requestedVersion.Equals("latest-pre", StringComparison.OrdinalIgnoreCase))
            return versions[0].Version;

        var latestStable = versions.FirstOrDefault(v => !v.IsPrerelease);
        return latestStable?.Version ?? versions[0].Version;
    }
}
