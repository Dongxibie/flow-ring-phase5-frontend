using System;

namespace FlowRing.RingCore;

public enum InputState
{
    Idle,
    Pressed,
    HoldDetected,
    RingOpening,
    Selecting,
    Executing,
    Closing,
}

/// <summary>
/// 输入状态机。合法迁移表：
///   Idle       → Pressed          （按下）
///   HoldingDetected → Selecting /   可扩展（直接进入 Selecting）
///   Pressed    → HoldDetected     （≥150ms 长按）
///   HoldDetected → RingOpening     （打开）
///   RingOpening → Selected        （首帧渲染完成）
///   Selected  → Executing         （松手且方向非 Center）
///   Selected  → Closing         （松手且方向为 Center，或 DeadZone）
///   Executing → Closing         （Action 执行结束）
///   Closing  → Idle              （Ring 消失）
/// 其他迁移都抛 InvalidStateTransitionException。
/// </summary>
public sealed class InputStateMachine
{
    public InputState Current { get; private set; } = InputState.Idle;

    public event EventHandler<InputStateChangedEventArgs>? OnStateChanged
    {
        add => _onStateChanged += value;
        remove => _onStateChanged -= value;
    }

    private event EventHandler<InputStateChangedEventArgs>? _onStateChanged;

    public void Transition(InputState target)
    {
        if (!IsLegal(Current, target))
        {
            throw new InvalidStateTransitionException(Current, target);
        }
        var previous = Current;
        Current = target;
        _onStateChanged?.Invoke(this, new InputStateChangedEventArgs(previous, target));
    }

    public void Reset() => Current = InputState.Idle;

    public static bool IsLegal(InputState from, InputState to) => (from, to) switch
    {
        (InputState.Idle, InputState.Pressed) => true,
        (InputState.Idle, InputState.HoldDetected) => true,
        (InputState.Idle, InputState.RingOpening) => true,
        (InputState.Pressed, InputState.HoldDetected) => true,
        (InputState.Pressed, InputState.Idle) => true,
        (InputState.HoldDetected, InputState.RingOpening) => true,
        (InputState.HoldDetected, InputState.Idle) => true,
        (InputState.RingOpening, InputState.Selecting) => true,
        (InputState.RingOpening, InputState.Closing) => true,
        (InputState.Selecting, InputState.Executing) => true,
        (InputState.Selecting, InputState.Closing) => true,
        (InputState.Executing, InputState.Closing) => true,
        (InputState.Closing, InputState.Selecting) => true, // 子 Ring 进入：先 Closing 再重新 Selecting
        (InputState.Closing, InputState.Idle) => true,
        _ => false,
    };
}

public sealed class InputStateChangedEventArgs : EventArgs
{
    public InputState Previous { get; }
    public InputState Current { get; }
    public InputStateChangedEventArgs(InputState previous, InputState current)
    {
        Previous = previous;
        Current = current;
    }
}

public sealed class InvalidStateTransitionException : InvalidOperationException
{
    public InputState From { get; }
    public InputState To { get; }

    public InvalidStateTransitionException(InputState from, InputState to)
        : base($"非法状态迁移：{from} → {to}")
    {
        From = from;
        To = to;
    }
}