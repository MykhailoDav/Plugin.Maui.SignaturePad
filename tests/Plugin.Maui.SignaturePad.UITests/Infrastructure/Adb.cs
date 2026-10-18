using System.Diagnostics;

namespace Plugin.Maui.SignaturePad.UITests.Infrastructure;

/// <summary>Runs adb from the Android SDK, for what Appium's Android driver cannot do.</summary>
internal static class Adb
{
    public static string Run(string arguments)
    {
        var sdk = Environment.GetEnvironmentVariable("ANDROID_HOME") is { Length: > 0 } home ? home
            : Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT") is { Length: > 0 } root ? root
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk");
        var adb = Path.Combine(sdk, "platform-tools", OperatingSystem.IsWindows() ? "adb.exe" : "adb");

        using var process = Process.Start(new ProcessStartInfo(adb, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException($"adb could not be started from {adb}.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode == 0 ? output : throw new InvalidOperationException($"adb {arguments} failed: {error}");
    }
}
