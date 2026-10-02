using FlowRing.RingCore.Geometry;

namespace FlowRing.RingCore.Ring;

/// <summary>
/// Ring 解析：当前 RingNode + 选定的 Direction → 返回 ActionSlot / ChildRingSlot / None。
/// 用于状态机从 Selecting → Executing 时调度 Action，或递归进入子 Ring。
/// </summary>
public sealed class RingResolver
{
    private readonly IReadOnlyDictionary<string, RingNode> _graph;

    public RingResolver(IReadOnlyDictionary<string, RingNode> graph)
    {
        _graph = graph;
    }

    public RingSlot? Resolve(string ringId, Direction direction)
    {
        if (!_graph.TryGetValue(ringId, out var ring))
        {
            return null;
        }
        return ring.Slots.TryGetValue(direction, out var slot) ? slot : null;
    }

    /// <summary>
    /// 从 Direction 反推 Slot 在该 Ring 的"按下侧键"位置（DirectionLock 反向解析）。
    /// </summary>
    public RingSlot? LockCenter(string ringId, RingPoint center, RingPoint current, float deadZoneRadiusPx)
    {
        var resolver = new DirectionResolver();
        var dir = resolver.Resolve(center, current, deadZoneRadiusPx);
        if (dir == Direction.Center)
        {
            return null;
        }
        return Resolve(ringId, dir);
    }
}