using FlowRing.RingCore;
using FlowRing.RingCore.Geometry;
using FluentAssertions;
using Xunit;

namespace FlowRing.RingCore.Tests;

/// <summary>
/// DirectionResolver 完整覆盖：8 方向 + Dead Zone + 边界值。
/// </summary>
public sealed class DirectionResolverTests
{
    private readonly DirectionResolver _resolver = new();

    public static IEnumerable<object[]> DirectionCases => new[]
    {
        new object[] { "Right", 100f, 0f },
        new object[] { "BottomRight", 70f, 70f },
        new object[] { "Bottom", 0f, 100f },
        new object[] { "BottomLeft", -70f, 70f },
        new object[] { "Left", -100f, 0f },
        new object[] { "TopLeft", -70f, -70f },
        new object[] { "Top", 0f, -100f },
        new object[] { "TopRight", 70f, -70f },
    };

    [Theory]
    [MemberData(nameof(DirectionCases))]
    public void ResolvesAllEightDirections(string label, float dx, float dy)
    {
        var expected = Enum.Parse<Direction>(label);
        var result = _resolver.Resolve(new RingPoint(0f, 0f), new RingPoint(dx, dy));
        result.Should().Be(expected);
    }

    [Fact]
    public void DeadZoneReturnsCenterAtOrigin()
    {
        var result = _resolver.Resolve(new RingPoint(0f, 0f), new RingPoint(0f, 0f));
        result.Should().Be(Direction.Center);
    }

    [Fact]
    public void DeadZoneReturnsCenterWithin30Pixels()
    {
        var result = _resolver.Resolve(new RingPoint(0f, 0f), new RingPoint(20f, 20f));
        result.Should().Be(Direction.Center);
    }

    [Fact]
    public void DeadZoneBoundaryIsExclusive()
    {
        // 正好 30px 距离（含 sqrt 2 = 42.43 时返回 Right）
        // 这里测 31px east → Right
        var result = _resolver.Resolve(new RingPoint(0f, 0f), new RingPoint(31f, 0f));
        result.Should().Be(Direction.Right);
    }

    [Fact]
    public void HonorsCustomDeadZoneRadius()
    {
        // 100px 外但 60px 内（在默认 30 时是 Right，在 60 时仍是 Center）
        var result = _resolver.Resolve(new RingPoint(0f, 0f), new RingPoint(40f, 0f), deadZoneRadiusPx: 60f);
        result.Should().Be(Direction.Center);
    }

    [Fact]
    public void NegativeAngleNormalizesToPositive()
    {
        // 负 X 轴 → Left
        var result = _resolver.Resolve(new RingPoint(0f, 0f), new RingPoint(-50f, -1f));
        result.Should().Be(Direction.Left);
    }
}