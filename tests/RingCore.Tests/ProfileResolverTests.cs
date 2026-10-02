using FlowRing.RingCore;
using FlowRing.RingCore.Profile;
using FlowRing.RingCore.Ring;
using FluentAssertions;
using Xunit;

namespace FlowRing.RingCore.Tests;

public sealed class ProfileResolverTests
{
    private static ProfileData BuildProfile(string id, params (string kind, string pattern, string profileId)[] rules)
    {
        var meta = new ProfileMetadata(id, id, "1.0.0", "1.0", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, new string('0', 64));
        var ruleList = rules.Select(r => new ProfileResolverRule(r.kind, r.pattern, r.profileId)).ToList();
        return new ProfileData
        {
            Metadata = meta,
            RootRingId = "root",
            RingGraph = new Dictionary<string, RingNode>(),
            ActionRefs = Array.Empty<string>(),
            ContextRules = ruleList,
        };
    }

    [Fact]
    public void FirstMatchWins()
    {
        var profiles = new Dictionary<string, ProfileData>
        {
            ["default"] = BuildProfile("default"),
            ["developer"] = BuildProfile("developer", ("processName", "code.exe", "developer")),
        };
        var resolver = new ProfileResolver(profiles, "default");
        var ctx = new ApplicationContext("code.exe", "Visual Studio", nint.Zero, DateTimeOffset.UtcNow);
        resolver.Resolve(ctx).Should().Be("developer");
    }

    [Fact]
    public void NoMatchFallsBackToDefault()
    {
        var profiles = new Dictionary<string, ProfileData>
        {
            ["default"] = BuildProfile("default"),
            ["developer"] = BuildProfile("developer", ("processName", "code.exe", "developer")),
        };
        var resolver = new ProfileResolver(profiles, "default");
        var ctx = new ApplicationContext("explorer.exe", "文件资源管理器", nint.Zero, DateTimeOffset.UtcNow);
        resolver.Resolve(ctx).Should().Be("default");
    }

    [Fact]
    public void WindowTitleMatchIgnoresCase()
    {
        var profiles = new Dictionary<string, ProfileData>
        {
            ["default"] = BuildProfile("default", ("windowTitle", "VISUAL STUDIO", "developer")),
        };
        var resolver = new ProfileResolver(profiles, "default");
        var ctx = new ApplicationContext("code.exe", "visual studio code", nint.Zero, DateTimeOffset.UtcNow);
        resolver.Resolve(ctx).Should().Be("default");
    }

    [Fact]
    public void RegexMatchScansProcessAndTitle()
    {
        var profiles = new Dictionary<string, ProfileData>
        {
            ["default"] = BuildProfile("default", ("regex", "code", "developer")),
        };
        var resolver = new ProfileResolver(profiles, "default");
        var ctx = new ApplicationContext("vscode.exe", "Code - main.cpp", nint.Zero, DateTimeOffset.UtcNow);
        resolver.Resolve(ctx).Should().Be("default");
    }

    [Fact]
    public void UnknownRuleKindDoesNotMatch()
    {
        var profiles = new Dictionary<string, ProfileData>
        {
            ["default"] = BuildProfile("default", ("unknown-kind", "pattern", "anything")),
        };
        var resolver = new ProfileResolver(profiles, "default");
        var ctx = new ApplicationContext("code.exe", "Code", nint.Zero, DateTimeOffset.UtcNow);
        resolver.Resolve(ctx).Should().Be("default");
    }

    [Fact]
    public void ConstructorRejectsMissingDefaultId()
    {
        var profiles = new Dictionary<string, ProfileData>
        {
            ["p1"] = BuildProfile("p1"),
        };
        var act = () => new ProfileResolver(profiles, "missing");
        act.Should().Throw<ArgumentException>();
    }
}