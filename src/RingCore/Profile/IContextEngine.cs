namespace FlowRing.RingCore.Profile;

/// <summary>
/// 上下文引擎接口。OS 无关；实际检测走 DesktopBridge.IContextDetector。
/// MVP：仅查询 ApplicationContext + 触发 ProfileResolver。
/// </summary>
public interface IContextEngine
{
    Task<ApplicationContext> QueryAsync(CancellationToken ct);
}

public sealed class NullContextEngine : IContextEngine
{
    public Task<ApplicationContext> QueryAsync(CancellationToken ct)
        => Task.FromResult(new ApplicationContext("unknown", string.Empty, nint.Zero, DateTimeOffset.UtcNow));
}