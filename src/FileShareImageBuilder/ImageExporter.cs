using System.Diagnostics;

namespace FileShareImageBuilder;

public sealed class ImageExporter
{
    public async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        var env = ConfigurationReader.GetEnvironmentName();
        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var binDirectory = Path.Combine(dataImagePath, "bin");

        if (!Directory.Exists(binDirectory))
            throw new DirectoryNotFoundException($"Bin directory not found: {binDirectory}");

        var imageName = $"fss-data-{env}";
        var dockerfilePath = Path.Combine(binDirectory, "Dockerfile.dataimage");

        // Create a minimal Dockerfile that copies the bin directory into a scratch image.
        // Note: this requires that all files copied into the image are readable and that
        // the Docker engine supports scratch builds.
        await File.WriteAllTextAsync(dockerfilePath, "FROM scratch\n" + "COPY . /bin\n", cancellationToken);

        Console.WriteLine($"[ImageExporter] Building docker image '{imageName}' from '{binDirectory}'...");
        await RunDockerAsync($"build -f \"{dockerfilePath}\" -t {imageName} \"{binDirectory}\"", cancellationToken)
            .ConfigureAwait(false);

        var tarPath = Path.Combine(dataImagePath, $"{imageName}.tar");
        Console.WriteLine($"[ImageExporter] Saving docker image to '{tarPath}'...");

        await RunDockerAsync($"save -o \"{tarPath}\" {imageName}", cancellationToken).ConfigureAwait(false);
        Console.WriteLine("[ImageExporter] Docker export complete.");
    }

    private static async Task RunDockerAsync(string args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdoutClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                stdoutClosed.TrySetResult();
                return;
            }

            if (!string.IsNullOrWhiteSpace(e.Data)) Console.WriteLine($"[docker] {e.Data}");
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                stderrClosed.TrySetResult();
                return;
            }

            if (!string.IsNullOrWhiteSpace(e.Data)) Console.Error.WriteLine($"[docker] {e.Data}");
        };

        if (!p.Start()) throw new InvalidOperationException("Failed to start docker process.");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        // Docker can be quiet for long periods (large context, IO). Emit a heartbeat so users know it's still running.
        var heartbeat = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested && !p.HasExited)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                if (!p.HasExited) Console.WriteLine($"[docker] still running: docker {args}");
            }
        }, cancellationToken);

        await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutClosed.Task, stderrClosed.Task).ConfigureAwait(false);
        try
        {
            await heartbeat.ConfigureAwait(false);
        }
        catch
        {
        }

        if (p.ExitCode != 0) throw new InvalidOperationException($"docker {args} failed with exit code {p.ExitCode}.");
    }
}