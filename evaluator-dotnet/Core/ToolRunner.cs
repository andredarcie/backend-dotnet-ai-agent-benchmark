using System.Diagnostics;

namespace BackendEvaluator.Core;

/// <summary>Runs external command-line tools and probes their availability (results are cached).</summary>
public sealed class ToolRunner
{
    private readonly Dictionary<string, bool> _availability = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAvailable(string exe, string probeArgs = "--version")
    {
        if (_availability.TryGetValue(exe, out var cached)) return cached;
        bool ok;
        try
        {
            var outcome = Run(exe, probeArgs, null, 15_000);
            // docker is daemon-backed: `docker version` exits non-zero when the daemon is unreachable,
            // so a launchable client isn't enough — require the probe to actually succeed.
            ok = string.Equals(exe, "docker", StringComparison.OrdinalIgnoreCase)
                ? outcome.Success
                : !outcome.NotFound;
        }
        catch { ok = false; }
        _availability[exe] = ok;
        return ok;
    }

    public ToolOutcome Run(string exe, string args, string? workingDir = null, int timeoutMs = 120_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir ?? Environment.CurrentDirectory,
        };

        Process proc;
        try { proc = Process.Start(psi)!; }
        catch (System.ComponentModel.Win32Exception) { return new ToolOutcome(false, true, -1, "", "", false); }
        catch (Exception ex) { return new ToolOutcome(false, true, -1, "", ex.Message, false); }

        using (proc)
        {
            // Start the async reads before waiting to avoid a pipe-buffer deadlock on large output.
            var soTask = proc.StandardOutput.ReadToEndAsync();
            var seTask = proc.StandardError.ReadToEndAsync();

            if (!proc.WaitForExit(timeoutMs))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* best effort */ }
                return new ToolOutcome(true, false, -1, "", "", true);
            }

            try { Task.WaitAll(new Task[] { soTask, seTask }, 5_000); } catch { /* ignore */ }
            string so = soTask.IsCompletedSuccessfully ? soTask.Result : "";
            string se = seTask.IsCompletedSuccessfully ? seTask.Result : "";
            return new ToolOutcome(true, false, proc.ExitCode, so, se, false);
        }
    }
}
