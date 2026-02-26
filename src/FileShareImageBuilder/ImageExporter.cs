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
        {
            throw new DirectoryNotFoundException($"Bin directory not found: {binDirectory}");
        }

        var imageName = $"fss-data-{env}";
        var dockerfilePath = Path.Combine(dataImagePath, "Dockerfile.dataimage");

        // Create a minimal Dockerfile that copies the bin directory into a scratch image.
        // Note: this requires that all files copied into the image are readable and that
        // the Docker engine supports scratch builds.
        File.WriteAllText(dockerfilePath,
            "FROM scratch\n" +
            "COPY bin /bin\n");

        await RunDockerAsync($"build -f \"{dockerfilePath}\" -t {imageName} \"{dataImagePath}\"", cancellationToken).ConfigureAwait(false);

        var tarPath = Path.Combine(dataImagePath, $"{imageName}.tar");
        await RunDockerAsync($"save -o \"{tarPath}\" {imageName}", cancellationToken).ConfigureAwait(false);
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
            CreateNoWindow = true,
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start docker process.");
        var stdout = await p.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await p.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker {args} failed with exit code {p.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }
}
