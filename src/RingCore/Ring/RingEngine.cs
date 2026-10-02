using FlowRing.RingCore.Action;
using FlowRing.RingCore.Geometry;
using FlowRing.RingCore.Profile;

namespace FlowRing.RingCore.Ring;

/// <summary>
/// Ring 引擎：编排输入适配 → 状态机 → ProfileResolver → RingResolver → ActionDispatcher。
/// Phase 4 实现：完成事件分发骨架；Phase 6 接入真实 Executor。
/// </summary>
public sealed class RingEngine
{
    private readonly InputStateMachine _stateMachine = new();
    private readonly IContextEngine _contextEngine;
    private readonly ProfileResolver _profileResolver;
    private readonly Dictionary<string, ProfileData> _profiles;
    private readonly RingResolver _ringResolver;
    private readonly ActionDispatcher _dispatcher;
    private string _currentRingId = string.Empty;
    private string _activeProfileId = string.Empty;
    private SpatialEvent? _pendingEvent;
    private RingPoint? _currentCenter;
    private float _deadZoneRadiusPx = 30f;

    public RingEngine(
        IReadOnlyDictionary<string, ProfileData> profiles,
        string defaultProfileId,
        IContextEngine contextEngine,
        IReadOnlyDictionary<ActionKind, IActionExecutor> executors)
    {
        _profiles = new Dictionary<string, ProfileData>(profiles);
        _profileResolver = new ProfileResolver(_profiles, defaultProfileId);
        _contextEngine = contextEngine;
        _ringResolver = new RingResolver(profiles[defaultProfileId].RingGraph);
        _currentRingId = profiles[defaultProfileId].RootRingId;
        _activeProfileId = defaultProfileId;
        _dispatcher = new ActionDispatcher(executors);
    }

    public InputState State => _stateMachine.Current;
    public string CurrentRingId => _currentRingId;
    public string ActiveProfileId => _activeProfileId;

    public event EventHandler<RingActionResolvedEventArgs>? ActionResolved;
    public event EventHandler<InputStateChangedEventArgs>? StateChanged;

    public async Task<RingOpenResult> OnHandStartAsync(SpatialIntentEvent evt, CancellationToken ct)
    {
        var ctx = await _contextEngine.QueryAsync(ct).ConfigureAwait(false);
        _activeProfileId = _profileResolver.Resolve(ctx);
        _currentRingId = _profiles[_activeProfileId].RootRingId;
        _currentCenter = evt.OriginPoint;
        _pendingEvent = new SpatialEvent(evt, _activeProfileId);
        try
        {
            _stateMachine.Transition(InputState.HoldDetected);
            _stateMachine.Transition(InputState.RingOpening);
            _stateMachine.Transition(InputState.Selecting);
            RaiseStateChanged();
            return new RingOpenResult(true, _activeProfileId, _currentRingId, _deadZoneRadiusPx);
        }
        catch (InvalidStateTransitionException)
        {
            return new RingOpenResult(false, _activeProfileId, _currentRingId, _deadZoneRadiusPx);
        }
    }

    public async Task<RingResolveResult> OnMouseMoveAsync(RingPoint current, CancellationToken ct)
    {
        await Task.Yield();
        if (_currentCenter is null || _stateMachine.Current != InputState.Selecting)
        {
            return new RingResolveResult(Direction.Center, null);
        }
        var dir = new DirectionResolver().Resolve(_currentCenter.Value, current, _deadZoneRadiusPx);
        if (dir == Direction.Center)
        {
            return new RingResolveResult(dir, null);
        }
        var slot = _ringResolver.Resolve(_currentRingId, dir);
        return new RingResolveResult(dir, slot);
    }

    public async Task<RingExecuteResult> OnHandReleaseAsync(Direction direction, CancellationToken ct)
    {
        if (_stateMachine.Current != InputState.Selecting)
        {
            return new RingExecuteResult(false, null);
        }
        if (direction == Direction.Center)
        {
            _stateMachine.Transition(InputState.Closing);
            _stateMachine.Transition(InputState.Idle);
            RaiseStateChanged();
            return new RingExecuteResult(false, null);
        }
        _stateMachine.Transition(InputState.Executing);
        var slot = _ringResolver.Resolve(_currentRingId, direction);
        if (slot is null)
        {
            _stateMachine.Transition(InputState.Closing);
            _stateMachine.Transition(InputState.Idle);
            RaiseStateChanged();
            return new RingExecuteResult(false, null);
        }
        if (slot is ChildRingSlot childSlot)
        {
            _stateMachine.Transition(InputState.Closing);
            _currentRingId = childSlot.ChildRingId;
            _stateMachine.Transition(InputState.Selecting);
            RaiseStateChanged();
            return new RingExecuteResult(true, null);
        }
        if (slot is ActionSlot actionSlot && _pendingEvent is not null)
        {
            var ctx = new ActionContext(
                _activeProfileId,
                await _contextEngine.QueryAsync(ct).ConfigureAwait(false),
                _pendingEvent.TriggerEvent.TimestampMs);
            var result = await _dispatcher.DispatchAsync(actionSlot.ActionRef, ctx, ct).ConfigureAwait(false);
            ActionResolved?.Invoke(this, new RingActionResolvedEventArgs(actionSlot.ActionRef, result));
            _stateMachine.Transition(InputState.Closing);
            _stateMachine.Transition(InputState.Idle);
            RaiseStateChanged();
            return new RingExecuteResult(result.Success, actionSlot);
        }
        _stateMachine.Transition(InputState.Closing);
        _stateMachine.Transition(InputState.Idle);
        RaiseStateChanged();
        return new RingExecuteResult(false, null);
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new InputStateChangedEventArgs(InputState.Idle, _stateMachine.Current));
    }
}

public sealed record SpatialEvent(SpatialIntentEvent TriggerEvent, string ActiveProfileId)
{
    public long TimestringMs => TriggerEvent.TimestampMs;
}

public sealed record RingOpenResult(bool Opened, string ActiveProfileId, string RootRingId, float DeadZoneRadiusPx);
public sealed record RingResolveResult(Direction Direction, RingSlot? Slot);
public sealed record RingExecuteResult(bool Executed, ActionSlot? Action);
public sealed class RingActionResolvedEventArgs : EventArgs
{
    public string ActionRef { get; }
    public ExecutionResult Result { get; }
    public RingActionResolvedEventArgs(string actionRef, ExecutionResult result)
    {
        ActionRef = actionRef;
        Result = result;
    }
}