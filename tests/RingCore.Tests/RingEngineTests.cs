using FlowRing.RingCore;
using FlowRing.RingCore.Action;
using FlowRing.RingCore.Geometry;
using FlowRing.RingCore.Profile;
using FlowRing.RingCore.Ring;
using FluentAssertions;
using Xunit;

namespace FlowRing.RingCore.Tests;

public sealed class RingEngineTests
{
    private static ProfileData BuildTestProfile(string id, string rootRingId = "root")
    {
        var meta = new ProfileMetadata(id, id, "1.0.0", "1.0", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, new string('0', 64));
        var root = new RingNode
        {
            Id = rootRingId,
            ProfileId = id,
            Slots = new Dictionary<Direction, RingSlot>
            {
                [Direction.Right] = new ActionSlot { Direction = Direction.Right, ActionRef = "key-t" },
                [Direction.Top] = new ChildRingSlot { Direction = Direction.Top, ChildRingId = "child" },
            },
        };
        var child = new RingNode
        {
            Id = "child",
            ProfileId = id,
            Slots = new Dictionary<Direction, RingSlot>
            {
                [Direction.Right] = new ActionSlot { Direction = Direction.Right, ActionRef = "system-screenshot" },
            },
        };
        return new ProfileData
        {
            Metadata = meta,
            RootRingId = rootRingId,
            RingGraph = new Dictionary<string, RingNode>
            {
                [rootRingId] = root,
                ["child"] = child,
            },
            ActionRefs = new[] { "key-t", "system-screenshot" },
            ContextRules = Array.Empty<ProfileResolverRule>(),
        };
    }

    private sealed class FakeExecutor : IActionExecutor
    {
        public ActionKind Kind { get; }
        public PermissionTier RequiredTier => PermissionTier.Safe;
        public ExecutionResult Result { get; set; } = new(true, null, 1);
        public string? LastActionId { get; private set; }

        public FakeExecutor(ActionKind kind) => Kind = kind;

        public ValueTask<ExecutionResult> ExecuteAsync(string actionId, ActionContext ctx, CancellationToken ct)
        {
            LastActionId = actionId;
            return ValueTask.FromResult(Result);
        }
    }

    [Fact]
    public async Task OnHandStartTransitionsToSelecting()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executors = new Dictionary<ActionKind, IActionExecutor>
        {
            [ActionKind.Keyboard] = new FakeExecutor(ActionKind.Keyboard),
        };
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);

        var result = await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(100f, 100f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        result.Opened.Should().BeTrue();
        engine.State.Should().Be(InputState.Selecting);
        engine.ActiveProfileId.Should().Be("default");
    }

    [Fact]
    public async Task OnMouseMoveResolvesDirectionAndSlot()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executors = new Dictionary<ActionKind, IActionExecutor>
        {
            [ActionKind.Keyboard] = new FakeExecutor(ActionKind.Keyboard),
        };
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(0f, 0f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        var result = await engine.OnMouseMoveAsync(new RingPoint(100f, 0f), CancellationToken.None);

        result.Direction.Should().Be(Direction.Right);
        result.Slot.Should().BeOfType<ActionSlot>();
    }

    [Fact]
    public async Task OnMouseMoveWithinDeadZoneReturnsCenter()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executors = new Dictionary<ActionKind, IActionExecutor>();
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(0f, 0f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        var result = await engine.OnMouseMoveAsync(new RingPoint(10f, 10f), CancellationToken.None);

        result.Direction.Should().Be(Direction.Center);
        result.Slot.Should().BeNull();
    }

    [Fact]
    public async Task OnHandReleaseExecutesAction()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executor = new FakeExecutor(ActionKind.Keyboard);
        var executors = new Dictionary<ActionKind, IActionExecutor>
        {
            [ActionKind.Keyboard] = executor,
        };
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(0f, 0f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        var result = await engine.OnHandReleaseAsync(Direction.Right, CancellationToken.None);

        result.Executed.Should().BeTrue();
        result.Action.Should().NotBeNull();
        executor.LastActionId.Should().Be("key-t");
        engine.State.Should().Be(InputState.Idle);
    }

    [Fact]
    public async Task OnHandReleaseOnCenterCancels()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executors = new Dictionary<ActionKind, IActionExecutor>();
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(0f, 0f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        var result = await engine.OnHandReleaseAsync(Direction.Center, CancellationToken.None);

        result.Executed.Should().BeFalse();
        engine.State.Should().Be(InputState.Idle);
    }

    [Fact]
    public async Task OnHandReleaseEntersChildRing()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executor = new FakeExecutor(ActionKind.System);
        var executors = new Dictionary<ActionKind, IActionExecutor>
        {
            [ActionKind.Keyboard] = new FakeExecutor(ActionKind.Keyboard),
            [ActionKind.System] = executor,
        };
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(0f, 0f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        var result = await engine.OnHandReleaseAsync(Direction.Top, CancellationToken.None);

        result.Executed.Should().BeTrue();
        engine.CurrentRingId.Should().Be("child");
        engine.State.Should().Be(InputState.Selecting);

        // 在 child ring 内选择 Right 触发截图 Action
        var second = await engine.OnHandReleaseAsync(Direction.Right, CancellationToken.None);
        second.Executed.Should().BeTrue();
        executor.LastActionId.Should().Be("system-screenshot");
    }

    [Fact]
    public async Task OnHandReleaseOnUnboundDirectionReturnsFalse()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executors = new Dictionary<ActionKind, IActionExecutor>();
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        await engine.OnHandStartAsync(
            new SpatialIntentEvent(TriggerType.MouseSideButton, new RingPoint(0f, 0f), 1f, 0, ModifierState.None),
            CancellationToken.None);

        // Bottom 没绑定 slot
        var result = await engine.OnHandReleaseAsync(Direction.Bottom, CancellationToken.None);

        result.Executed.Should().BeFalse();
    }

    [Fact]
    public async Task OnHandReleaseWithoutActiveRingReturnsFalse()
    {
        var profiles = new Dictionary<string, ProfileData> { ["default"] = BuildTestProfile("default") };
        var executors = new Dictionary<ActionKind, IActionExecutor>();
        var engine = new RingEngine(profiles, "default", new NullContextEngine(), executors);
        // 不调 OnHandStartAsync，直接释放
        var result = await engine.OnHandReleaseAsync(Direction.Right, CancellationToken.None);
        result.Executed.Should().BeFalse();
    }
}