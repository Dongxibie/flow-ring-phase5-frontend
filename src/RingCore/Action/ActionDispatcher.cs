using FlowRing.RingCore.Action;

namespace FlowRing.RingCore;

/// <summary>
/// Action 调度器：根据 ActionKind 找 Executor 并执行。MVP 实现；
/// Phase 6 升级为完整 Pipeline（PermissionCheck → ContextInject → Executor → AuditLog）。
/// </summary>
public sealed class ActionDispatcher
{
    private readonly IReadOnlyDictionary<ActionKind, IActionExecutor> _executors;

    public ActionDispatcher(IReadOnlyDictionary<ActionKind, IActionExecutor> executors)
    {
        _executors = executors;
    }

    public ValueTask<ExecutionResult> DispatchAsync(string actionId, ActionContext ctx, CancellationToken ct)
    {
        // MVP：actionId 形如 "kind:..."，从 kind 解析 ActionKind
        // Phase 6 起改成从 ActionRegistry 查 ID + 完整 payload
        var kind = InferKind(actionId);
        if (!_executors.TryGetValue(kind, out var executor))
        {
            return ValueTask.FromResult(new ExecutionResult(false, $"ActionKind {kind} 无注册 Executor", 0));
        }
        return executor.ExecuteAsync(actionId, ctx, ct);
    }

    private static ActionKind InferKind(string actionId)
    {
        var prefix = actionId.Split('-', 2)[0];
        return prefix switch
        {
            "key" => ActionKind.Keyboard,
            "system" => ActionKind.System,
            "app" => ActionKind.Application,
            "ai" => ActionKind.AI,
            "flow" => ActionKind.Workflow,
            _ => ActionKind.Keyboard,
        };
    }
}