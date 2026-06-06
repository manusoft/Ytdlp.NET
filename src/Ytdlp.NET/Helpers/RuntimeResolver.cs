namespace ManuHub.Ytdlp.NET.Helpers;

/// <summary>
/// Resolves the runtime path based on user input, supporting direct file paths, directories, and system PATH lookups for supported runtimes (Deno, Node, QuickJS, Bun).
/// </summary>
internal static class RuntimeResolver
{
    private sealed record RuntimeDefinition(string BinaryName);

    private static readonly Dictionary<Runtime, RuntimeDefinition> Definitions = new()
    {
        { Runtime.Deno, new("deno") },
        { Runtime.Node, new("node") },
        { Runtime.QuickJs, new("quickjs") },
        { Runtime.Bun, new("bun") }
    };

    public static string Resolve(Runtime runtime, string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException($"{runtime} location cannot be null or empty.");

        location = location.Replace('\\', '/');

        // CASE 1: direct file
        if (File.Exists(location))
        {
            ToolPermissionManager.EnsureExecutableIfFile(location);
            return location;
        }

        // CASE 2: folder
        if (Directory.Exists(location))
        {
            ValidateFolder(location, runtime);
            return location;
        }

        // CASE 3: PATH or alias
        return location;
    }

    private static void ValidateFolder(string folder, Runtime runtime)
    {
        if (!Definitions.TryGetValue(runtime, out var def))
            throw new NotSupportedException($"Runtime not supported: {runtime}");

        var fileName = OperatingSystem.IsWindows()
            ? $"{def.BinaryName}.exe"
            : def.BinaryName;

        var fullPath = Path.Combine(folder, fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"{runtime} folder must contain binary: {fileName}");

        ToolPermissionManager.EnsureExecutableIfFile(fullPath);
    }
}
