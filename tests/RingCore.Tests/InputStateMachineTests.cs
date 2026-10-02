using FlowRing.RingCore;
using FluentAssertions;
using Xunit;

namespace FlowRing.RingCore.Tests;

public sealed class InputStateMachineTests
{
    [Fact]
    public void InitialStateIsIdle()
    {
        var sm = new InputStateMachine();
        sm.Current.Should().Be(InputState.Idle);
    }

    [Theory]
    [InlineData(InputState.Idle, InputState.Pressed, true)]
    [InlineData(InputState.Idle, InputState.HoldDetected, true)]
    [InlineData(InputState.Idle, InputState.RingOpening, true)]
    [InlineData(InputState.Pressed, InputState.HoldDetected, true)]
    [InlineData(InputState.Pressed, InputState.Idle, true)]
    [InlineData(InputState.Pressed, InputState.Selecting, false)] // 跨行非法
    [InlineData(InputState.Selecting, InputState.Idle, false)]
    [InlineData(InputState.Executing, InputState.Selecting, false)]
    public void LegalTransitionTableMatches(InputState from, InputState to, bool legal)
    {
        InputStateMachine.IsLegal(from, to).Should().Be(legal);
    }

    [Fact]
    public void IllegalTransitionRaisesException()
    {
        var sm = new InputStateMachine();
        var act = () => sm.Transition(InputState.Executing);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void OnStateChangedFiresWhenLegalTransition()
    {
        var sm = new InputStateMachine();
        InputState? observed = null;
        sm.OnStateChanged += (_, e) => observed = e.Current;
        sm.Transition(InputState.Pressed);
        observed.Should().Be(InputState.Pressed);
    }

    [Fact]
    public void ResetReturnsToIdle()
    {
        var sm = new InputStateMachine();
        sm.Transition(InputState.Pressed);
        sm.Reset();
        sm.Current.Should().Be(InputState.Idle);
    }

    [Fact]
    public void IllegalTransitionDoesNotFireEvent()
    {
        var sm = new InputStateMachine();
        var fired = false;
        sm.OnStateChanged += (_, _) => fired = true;
        try { sm.Transition(InputState.Executing); } catch { /* 预期异常 */ }
        fired.Should().BeFalse();
    }
}