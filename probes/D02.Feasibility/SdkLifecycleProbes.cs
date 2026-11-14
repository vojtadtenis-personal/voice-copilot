// Diagnostic-only opt-in to the pinned SDK's experimental lifecycle RPC surface.
#pragma warning disable GHCP001
using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using System.Collections.Concurrent;

namespace D02.Feasibility;

internal static class SdkLifecycleProbes
{
    public static async Task RunAsync(CopilotClient client, CopilotSession first, DenyUpstreamHandler handler,
        IList<ModelInfo> models, string model, ProbeResults results, CancellationToken ct)
    {
        await first.Rpc.Queue.SetDrainPausedAsync(true, ct);
        const string queuedPrompt = "D02 owned queued prompt. Do not run.";
        var queuedId = await first.SendAsync(new MessageOptions { Prompt = queuedPrompt, ClientTurnId = Guid.NewGuid().ToString("N") }, ct);
        var pending = await first.Rpc.Queue.PendingItemsAsync(ct);
        results.Check("SDK.QUEUE_PENDING", pending.Items.Any(item => item.MessageId == queuedId),
            $"pausedPendingCount={pending.Items.Count}; accepted snapshot discoverable by MessageId");
        var wrong = await first.Rpc.Queue.WithdrawMessageAsync(queuedId, "wrong expected prompt", ct);
        results.Check("SDK.QUEUE_WITHDRAW_GUARD", !wrong.Removed, "Stale expectedPrompt does not remove the accepted queued message.");
        var withdrawn = await first.Rpc.Queue.WithdrawMessageAsync(queuedId, queuedPrompt, ct);
        results.Check("SDK.QUEUE_WITHDRAW", withdrawn.Removed && !withdrawn.Interrupted, "Exact queued message withdrawn before execution; not a general unsend guarantee.");
        var another = await first.SendAsync(new MessageOptions { Prompt = queuedPrompt }, ct);
        pending = await first.Rpc.Queue.PendingItemsAsync(ct);
        var item = pending.Items.Single(entry => entry.MessageId == another);
        var removed = await first.Rpc.Queue.RemoveAtAsync(item.Id, ct);
        results.Check("SDK.QUEUE_REMOVE", removed.Removed, "RemoveAt accepts native queue-item ID, not MessageId.");
        try
        {
            await first.SendAndWaitAsync(new MessageOptions { Prompt = "D02 owned timeout fixture. Do not run." },
                TimeSpan.FromMilliseconds(200), ct);
            results.Add("SDK.WAIT_TIMEOUT", "FAILED", "Unexpected response while upstream inference is held.");
        }
        catch (TimeoutException)
        {
            results.Check("SDK.WAIT_TIMEOUT", handler.ActiveRequests.TryGetValue(first.SessionId, out var firstRequest) && !firstRequest.IsCancellationRequested,
                "Cancelling/timing out the .NET wait did not abort the already active runtime model request.");
        }
        await first.Rpc.Queue.ClearAsync(ct);
        results.Check("SDK.QUEUE_CLEAR", (await first.Rpc.Queue.PendingItemsAsync(ct)).Items.Count == 0,
            "Clear empties native pending items while the accepted running turn remains distinct.");

        var otherModel = models.FirstOrDefault(m => m.Id != model && m.Policy?.State == "enabled" && m.Billing?.TokenPrices is not null);
        if (otherModel is not null)
        {
            var switched = await first.Rpc.Model.SwitchToAsync(new ModelSwitchToRequest
            {
                ModelId = otherModel.Id,
                ContextTier = ContextTier.Default,
                DeferIfModelChangeQueued = true,
            }, ct);
            results.Check("SDK.MODEL_DEFERRED", switched.Deferred == true && !string.IsNullOrEmpty(switched.QueueId) && switched.ModelId == model,
                $"target={otherModel.Id}; deferred={switched.Deferred}; queueIdPresent={!string.IsNullOrEmpty(switched.QueueId)}; status={switched.Status}; confirmationPresent={switched.Confirmation is not null}; live model unchanged");
            await first.Rpc.Queue.ClearAsync(ct);
        }

        var secondEvents = new ConcurrentQueue<SessionEvent>();
        var secondConfig = SdkProbes.InferenceConfig(model, secondEvents.Enqueue);
        secondConfig.Name = $"D02-{Guid.NewGuid():N}";
        secondConfig.PermissionMode = PermissionMode.AllowAll;
        secondConfig.Autopilot = new AutopilotOptions { ContinuationEnabled = false, MaxContinues = 0 };
        await using var second = await client.CreateSessionAsync(secondConfig, ct);
        var permissions = await second.Rpc.Permissions.GetModeAsync(ct);
        results.Check("SDK.ALLOW_ALL", second.PermissionModeResult?.Success == true &&
            second.PermissionModeResult.Mode == PermissionMode.AllowAll && permissions.Mode == PermissionMode.AllowAll,
            "Create applied requested AllowAll before returning; RPC re-read confirms effective mode. Tools still denied in this diagnostic.");
        var setMode = await second.Rpc.Mode.SetAsync(SessionMode.Autopilot, cancellationToken: ct);
        results.Check("SDK.AUTOPILOT", setMode.ModeApplied == true && await second.Rpc.Mode.GetAsync(ct) == SessionMode.Autopilot,
            "Actual session mode set and re-read BEFORE send; continuation options alone are not mode.");
        var secondId = await second.SendAsync(new MessageOptions { Prompt = "D02 own concurrent diagnostic. Reply OK.", ClientTurnId = "D02-second" }, ct);
        var waitUntil = DateTime.UtcNow.AddSeconds(30);
        while (!handler.ActiveRequests.ContainsKey(second.SessionId) && DateTime.UtcNow < waitUntil)
            await Task.Delay(50, ct);
        results.Check("SDK.CONCURRENT", first.SessionId != second.SessionId &&
            handler.ActiveRequests.TryGetValue(first.SessionId, out var request1) && !request1.IsCancellationRequested &&
            handler.ActiveRequests.TryGetValue(second.SessionId, out var request2) && !request2.IsCancellationRequested,
            "Two distinct real sessions have simultaneous held model requests; actual model calls=0.");
        await first.AbortAsync(ct);
        results.Check("SDK.ABORT_ISOLATION", handler.ActiveRequests.TryGetValue(second.SessionId, out var stillRunning) && !stillRunning.IsCancellationRequested,
            "Aborting first session leaves second runtime request active.");
        await second.AbortAsync(ct);
        results.Check("SDK.ROOT_CORRELATION", secondEvents.OfType<UserMessageEvent>().Any(e =>
            string.IsNullOrEmpty(e.AgentId) && e.Data.MessageId == secondId && e.Data.ClientTurnId == "D02-second"),
            "Observed root user message correlates with second accepted send; no actual subagent was spawned.");
        results.Add("SDK.REAL_SUBAGENT_FILTER", "BLOCKED", "No live subagent trajectory; non-empty AgentId filtering is source-confirmed only.");
        await second.DisposeAsync();
        try
        {
            await client.DeleteSessionAsync(second.SessionId, ct);
            results.Add("SDK.SECOND_DELETE", "PASSED", "Only owned second diagnostic history deleted.");
        }
        catch (InvalidOperationException)
        {
            results.Add("SDK.SECOND_DELETE", "BLOCKED", "Native deletion refused after detach; no unrelated session altered.");
        }
        results.Add("SDK.MODEL_APPLY_AFTER_COMPLETION", "BLOCKED", "Deferred entry was explicitly cleared; no live completion/model-change drain ordering claim.");
        results.Add("SDK.NATIVE_IDEMPOTENCY_ROLLBACK", "BLOCKED", "No duplicate-send or side-effect experiment; ClientTurnId is correlation, not a proven idempotency key; abort has no rollback guarantee.");
    }

    public static async Task ResumeAsync(CopilotClient client, CopilotSession session, DenyUpstreamHandler handler,
        string model, ProbeResults results, CancellationToken ct)
    {
        try
        {
            await session.Rpc.Queue.SetDrainPausedAsync(true, ct);
            var pendingBefore = (await session.Rpc.Queue.PendingItemsAsync(ct)).Items.Count;
            var sessionId = session.SessionId;
            await session.DisposeAsync();
            var before = handler.InterceptedModelRequests;
            var events = new ConcurrentQueue<SessionEvent>();
            await using var resumed = await client.ResumeSessionAsync(sessionId, SdkProbes.ResumeConfig(model, events.Enqueue), ct);
            await Task.Delay(1000, ct);
            var after = handler.InterceptedModelRequests;
            results.Check("SDK.RESUME_NO_SEND", before == after,
                $"ContinuePendingWork=false; pendingBeforeDetach={pendingBefore}; newInterceptedModelRequests={after - before}; warm runtime resume only");
            await resumed.Rpc.Queue.SetDrainPausedAsync(true, ct);
            await resumed.Rpc.Queue.ClearAsync(ct);
            await resumed.AbortAsync(ct);
            results.Add("SDK.COLD_RESUME_PENDING", "BLOCKED", "Warm detach/resume is distinct: process restart, abandoned tool calls, locks and persistent queue recovery remain unverified.");
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            results.Add("SDK.RESUME_NO_SEND", "BLOCKED",
                $"Resume RPC refused/missing owned history; exceptionType={error.GetType().Name}; no fallback send or replay.");
        }
        results.Add("SDK.COLD_RESUME_PENDING", "BLOCKED", "No proved cold persisted pending fixture; ContinuePendingWork=false must still be tested across process restart.");
    }
}
#pragma warning restore GHCP001
