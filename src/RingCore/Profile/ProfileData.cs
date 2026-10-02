using FlowRing.RingCore.Profile;
using FlowRing.RingCore.Ring;

namespace FlowRing.RingCore;

/// <summary>
/// Profile 运行时内存形态。重命名为 ProfileData 以避免与 FlowRing.RingCore.Profile 命名空间冲突。
/// 持久化形态由 Phase 6 ProfileStore 落盘。
/// </summary>
public sealed class ProfileData
{
    public required ProfileMetadata Metadata { get; init; }
    public required string RootRingId { get; init; }
    public required IReadOnlyDictionary<string, RingNode> RingGraph { get; init; }
    public required IReadOnlyList<string> ActionRefs { get; init; }
    public required IReadOnlyList<ProfileResolverRule> ContextRules { get; init; }
}