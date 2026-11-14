using System.Collections.Concurrent;
using System.Text.Json;

namespace D02.Feasibility;

internal sealed class ProbeResults
{
    private readonly ConcurrentQueue<ProbeResult> results = new();

    public bool Failed => results.Any(result => result.Status == "FAILED");

    public void Add(string id, string status, string detail)
    {
        var result = new ProbeResult(id, status, detail);
        results.Enqueue(result);
        Console.WriteLine(JsonSerializer.Serialize(result));
    }

    public void Check(string id, bool condition, string detail) =>
        Add(id, condition ? "PASSED" : "FAILED", detail);

    private sealed record ProbeResult(string Id, string Status, string Detail);
}
