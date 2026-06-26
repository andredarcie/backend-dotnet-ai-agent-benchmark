using System.Text;

namespace BackendEvaluator.Core;

/// <summary>Minimal HTTP probe for dynamic checks against the live system (used by the harness).</summary>
/// <remarks>
/// The synchronous, blocking style (<c>.GetAwaiter().GetResult()</c> in <see cref="Get"/>/<see cref="Send"/>
/// and the blocking <see cref="Burst"/>) is a deliberate choice for this sequential CLI orchestration: the
/// contract oracle must complete each probe before issuing the next request, so async would yield no
/// throughput benefit and only ripple <c>await</c> through the otherwise simple static call sites.
/// </remarks>
public static class HttpProbe
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    public sealed record ProbeResult(bool Reached, int Status);

    /// <summary>Full response capture for contract checks: status, body and the headers we assert on.</summary>
    public sealed record Response(bool Reached, int Status, string Body, string? Location, string? ContentType);

    public static ProbeResult Get(string url)
    {
        try
        {
            using var resp = Client.GetAsync(url).GetAwaiter().GetResult();
            return new ProbeResult(true, (int)resp.StatusCode);
        }
        catch
        {
            return new ProbeResult(false, 0);
        }
    }

    /// <summary>Issues a request (any verb) with an optional JSON body and captures the full response.</summary>
    public static Response Send(string method, string url, string? json = null)
    {
        try
        {
            using var req = new HttpRequestMessage(new HttpMethod(method), url);
            if (json != null)
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = Client.SendAsync(req).GetAwaiter().GetResult();
            string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            string? location = resp.Headers.Location?.ToString()
                               ?? (resp.Headers.TryGetValues("Location", out var v) ? v.FirstOrDefault() : null);
            string? contentType = resp.Content.Headers.ContentType?.MediaType;
            return new Response(true, (int)resp.StatusCode, body, location, contentType);
        }
        catch
        {
            return new Response(false, 0, "", null, null);
        }
    }

    public sealed record BurstResult(int Sent, int ServerErrors, int Failures, long MaxMs);

    /// <summary>Fires <paramref name="total"/> GETs at up to <paramref name="concurrency"/> in flight and
    /// reports 5xx responses, transport failures and the worst latency — a light concurrency smoke test.</summary>
    public static BurstResult Burst(string url, int total, int concurrency)
    {
        int serverErrors = 0, failures = 0;
        long maxMs = 0;
        Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = concurrency }, _ =>
        {
            var start = Environment.TickCount64;
            var r = Get(url);
            var elapsed = Environment.TickCount64 - start;
            if (!r.Reached) Interlocked.Increment(ref failures);
            else if (r.Status >= 500) Interlocked.Increment(ref serverErrors);
            long prev;
            do { prev = Interlocked.Read(ref maxMs); if (elapsed <= prev) break; }
            while (Interlocked.CompareExchange(ref maxMs, elapsed, prev) != prev);
        });
        return new BurstResult(total, serverErrors, failures, maxMs);
    }

    /// <summary>Polls a URL until it returns 2xx/3xx or the timeout elapses.</summary>
    public static bool WaitForOk(string url, int timeoutMs, Action<string>? log = null)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        int attempt = 0;
        while (Environment.TickCount64 < deadline)
        {
            var r = Get(url);
            if (r.Reached && r.Status is >= 200 and < 400) return true;
            if (++attempt % 5 == 0) log?.Invoke($"  waiting for {url} (status {(r.Reached ? r.Status : 0)}) ...");
            Thread.Sleep(2000);
        }
        return false;
    }
}
