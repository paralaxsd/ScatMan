using ScatMan.Core;
using Xunit;

namespace ScatMan.Core.Tests;

public class PackageSourceResolverTests
{
    [Fact]
    public void GetSources_ReturnsAtLeastNugetOrg()
    {
        var sources = PackageSourceResolver.GetSources();

        Assert.NotEmpty(sources);
        Assert.Contains(sources, s => s.Name.Equals("nuget.org", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSources_NugetOrgUrlIsCorrect()
    {
        var sources = PackageSourceResolver.GetSources();
        var nugetOrg = sources.First(s => s.Name.Equals("nuget.org", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("https://api.nuget.org/v3/index.json", nugetOrg.Url);
    }

    [Fact]
    public void ResolveSourceUrl_Null_ReturnsNugetOrgUrl()
    {
        var url = PackageSourceResolver.ResolveSourceUrl(null);

        Assert.Equal("https://api.nuget.org/v3/index.json", url);
    }

    [Fact]
    public void ResolveSourceUrl_EmptyString_ReturnsNugetOrgUrl()
    {
        var url = PackageSourceResolver.ResolveSourceUrl("");

        Assert.Equal("https://api.nuget.org/v3/index.json", url);
    }

    [Fact]
    public void ResolveSourceUrl_WhitespaceString_ReturnsNugetOrgUrl()
    {
        var url = PackageSourceResolver.ResolveSourceUrl("   ");

        Assert.Equal("https://api.nuget.org/v3/index.json", url);
    }

    [Fact]
    public void ResolveSourceUrl_KnownSourceName_ReturnsSourceUrl()
    {
        var url = PackageSourceResolver.ResolveSourceUrl("nuget.org");

        Assert.Equal("https://api.nuget.org/v3/index.json", url);
    }

    [Fact]
    public void ResolveSourceUrl_KnownSourceName_CaseInsensitive()
    {
        var url1 = PackageSourceResolver.ResolveSourceUrl("NUGET.ORG");
        var url2 = PackageSourceResolver.ResolveSourceUrl("NuGet.Org");

        Assert.Equal("https://api.nuget.org/v3/index.json", url1);
        Assert.Equal("https://api.nuget.org/v3/index.json", url2);
    }

    [Fact]
    public void ResolveSourceUrl_DirectHttpUrl_ReturnsUrl()
    {
        var url = PackageSourceResolver.ResolveSourceUrl("http://example.com/nuget/v3/index.json");

        Assert.Equal("http://example.com/nuget/v3/index.json", url);
    }

    [Fact]
    public void ResolveSourceUrl_DirectHttpsUrl_ReturnsUrl()
    {
        var url = PackageSourceResolver.ResolveSourceUrl("https://nexus.company.com/repository/nuget/v3/index.json");

        Assert.Equal("https://nexus.company.com/repository/nuget/v3/index.json", url);
    }

    [Fact]
    public void ResolveSourceUrl_InvalidSourceName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PackageSourceResolver.ResolveSourceUrl("invalid-source-name"));

        Assert.Contains("Unknown package source", ex.Message);
        Assert.Contains("Use 'scatman sources'", ex.Message);
    }

    [Fact]
    public void ResolveSourceUrl_MalformedUrl_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PackageSourceResolver.ResolveSourceUrl("not-a-url"));

        Assert.Contains("Unknown package source", ex.Message);
    }

    [Fact]
    public void ResolveSourceUrl_FtpUrl_ThrowsArgumentException()
    {
        // FTP is not supported, only HTTP/HTTPS
        var ex = Assert.Throws<ArgumentException>(() =>
            PackageSourceResolver.ResolveSourceUrl("ftp://example.com/nuget/v3/index.json"));

        Assert.Contains("Unknown package source", ex.Message);
    }

    [Fact]
    public void ResolveSourceUrl_RelativeUrl_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PackageSourceResolver.ResolveSourceUrl("/relative/path"));

        Assert.Contains("Unknown package source", ex.Message);
    }
}
