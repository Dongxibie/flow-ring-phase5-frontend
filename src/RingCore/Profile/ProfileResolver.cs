using FlowRing.RingCore.Profile;

namespace FlowRing.RingCore;

/// <summary>
/// Profile 解析器：根据 ApplicationContext + Profile 的 ContextRules 决定激活哪个 Profile。
/// 匹配规则按声明顺序 first-match。
/// MVP 匹配模式：processName / windowTitle / windowClass / regex（regex 暂当 plain 处理）。
/// </summary>
public sealed class ProfileResolver
{
    private readonly IReadOnlyDictionary<string, ProfileData> _profiles;
    private readonly string _defaultProfileId;

    public ProfileResolver(IReadOnlyDictionary<string, ProfileData> profiles, string defaultProfileId)
    {
        if (!profiles.ContainsKey(defaultProfileId))
        {
            throw new ArgumentException($"defaultProfileId '{defaultProfileId}' 不在 profiles 中", nameof(defaultProfileId));
        }
        _profiles = profiles;
        _defaultProfileId = defaultProfileId;
    }

    public string Resolve(ApplicationContext context)
    {
        foreach (var (_, profile) in _profiles)
        {
            foreach (var rule in profile.ContextRules)
            {
                if (Matches(rule, context))
                {
                    return profile.Metadata.Id;
                }
            }
        }
        return _defaultProfileId;
    }

    private static bool Matches(ProfileResolverRule rule, ApplicationContext context)
    {
        return rule.Kind switch
        {
            "processName" => string.Equals(context.ProcessName, rule.Pattern, StringComparison.OrdinalIgnoreCase),
            "windowTitle" => context.WindowTitle.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            "windowClass" => string.Equals(context.WindowTitle, rule.Pattern, StringComparison.OrdinalIgnoreCase),
            "regex" => context.WindowTitle.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase)
                     || context.ProcessName.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}