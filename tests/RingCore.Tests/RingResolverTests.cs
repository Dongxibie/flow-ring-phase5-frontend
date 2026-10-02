using FlowRing.RingCore.Geometry;
using FlowRing.RingCore.Ring;
using FluentAssertions;
using Xunit;

namespace FlowRing.RingCore.Tests;

public sealed class RingResolverTests
{
    private static RingNode BuildNode(Dictionary<Direction, RingSlot>? slots = null)
    {
        return new RingNode
        {
            Id = "test-node",
            ProfileId = "test",
            Slots = slots ?? new Dictionary<Direction, RingSlot>(),
        };
    }

    [Fact]
    public void ResolvesActionSlotByDirection()
    {
        var actionSlot = new ActionSlot { Direction = Direction.Right, ActionRef = "key-ctrl-shift-t" };
        var ring = BuildNode(new Dictionary<Direction, RingSlot> { [Direction.Right] = actionSlot });
        var resolver = new RingResolver(new Dictionary<string, RingNode> { ["root"] = ring });
        resolver.Resolve("root", Direction.Right).Should().BeSameAs(actionSlot);
    }

    [Fact]
    public void ResolvesChildRingSlot()
    {
        var childSlot = new ChildRingSlot { Direction = Direction.Left, ChildRingId = "child" };
        var ring = BuildNode(new Dictionary<Direction, RingSlot> { [Direction.Left] = childSlot });
        var resolver = new RingResolver(new Dictionary<string, RingNode> { ["root"] = ring });
        resolver.Resolve("root", Direction.Left).Should().BeSameAs(childSlot);
    }

    [Fact]
    public void ReturnsNullForUnknownDirection()
    {
        var ring = BuildNode();
        var resolver = new RingResolver(new Dictionary<string, RingNode> { ["root"] = ring });
        resolver.Resolve("root", Direction.Top).Should().BeNull();
    }

    [Fact]
    public void ReturnsNullForUnknownRingId()
    {
        var resolver = new RingResolver(new Dictionary<string, RingNode>());
        resolver.Resolve("missing", Direction.Right).Should().BeNull();
    }

    [Fact]
    public void LockCenterReturnsSlotWhenFarEnough()
    {
        var actionSlot = new ActionSlot { Direction = Direction.Right, ActionRef = "key-t" };
        var ring = BuildNode(new Dictionary<Direction, RingSlot> { [Direction.Right] = actionSlot });
        var resolver = new RingResolver(new Dictionary<string, RingNode> { ["root"] = ring });
        var result = resolver.LockCenter("root", new RingPoint(0f, 0f), new RingPoint(100f, 0f), 30f);
        result.Should().BeSameAs(actionSlot);
    }

    [Fact]
    public void LockCenterReturnsNullWithinDeadZone()
    {
        var actionSlot = new ActionSlot { Direction = Direction.Right, ActionRef = "key-t" };
        var ring = BuildNode(new Dictionary<Direction, RingSlot> { [Direction.Right] = actionSlot });
        var resolver = new RingResolver(new Dictionary<string, RingNode> { ["root"] = ring });
        var result = resolver.LockCenter("root", new RingPoint(0f, 0f), new RingPoint(10f, 10f), 30f);
        result.Should().BeNull();
    }
}