using System.Diagnostics;

namespace Plugin.Maui.SignaturePad.UITests.Infrastructure;

/// <summary>
/// Starts the Appium server installed in this folder (<c>npm ci</c> installs it with its drivers), listening on the loopback
/// address only, and stops it at the end of the run. APPIUM_HOME points to this folder, so Appium finds the drivers listed
/// in package.json.
/// </summary>
internal sealed class AppiumServer : IDisposable
{
    private const int Port = 4723;

    private readonly Process _process;
    private readonly StreamWriter _log;

    public Uri Url { get; } = new($"http://127.0.0.1:{Port}/");

    private AppiumServer(Process process, StreamWriter log)
    {
        _process = process;
        _log = log;
    }

    public static AppiumServer Start()
    {
        var appium = Path.Combine(UITestSettings.ProjectDirectory, "node_modules", "appium", "index.js");
        if (!File.Exists(appium))
        {
            throw new InvalidOperationException($"Appium is not installed. Run 'npm ci' in {UITestSettings.ProjectDirectory}.");
        }

        Directory.CreateDirectory(UITestSettings.ResultsDirectory);
        var log = new StreamWriter(Path.Combine(UITestSettings.ResultsDirectory, "appium.log")) { AutoFlush = true };

        var startInfo = new ProcessStartInfo("node", [appium, "--address", "127.0.0.1", "--port", Port.ToString(System.Globalization.CultureInfo.InvariantCulture), "--log-no-colors"])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["APPIUM_HOME"] = UITestSettings.ProjectDirectory;
        SetDefaultAndroidEnvironment(startInfo);

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Appium could not be started.");
        process.OutputDataReceived += (_, e) => Write(log, e.Data);
        process.ErrorDataReceived += (_, e) => Write(log, e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var server = new AppiumServer(process, log);
        server.WaitUntilReady();
        return server;
    }

    /// <summary>
    /// The Android driver needs ANDROID_HOME and Java. When they are not set, the default install locations
    /// are used: the SDK where Android Studio puts it, and on macOS the default JDK.
    /// </summary>
    private static void SetDefaultAndroidEnvironment(ProcessStartInfo startInfo)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_HOME"))
            && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sdk = OperatingSystem.IsMacOS() ? Path.Combine(home, "Library", "Android", "sdk")
                : OperatingSystem.IsWindows() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk")
                : Path.Combine(home, "Android", "Sdk");

            if (Directory.Exists(sdk))
            {
                startInfo.Environment["ANDROID_HOME"] = sdk;
            }
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JAVA_HOME")) && OperatingSystem.IsMacOS() && File.Exists("/usr/libexec/java_home"))
        {
            using var javaHome = Process.Start(new ProcessStartInfo("/usr/libexec/java_home") { RedirectStandardOutput = true, RedirectStandardError = true });
            var path = javaHome?.StandardOutput.ReadToEnd().Trim();
            javaHome?.WaitForExit();
            if (javaHome?.ExitCode == 0 && Directory.Exists(path))
            {
                startInfo.Environment["JAVA_HOME"] = path;
            }
        }
    }

    private static void Write(StreamWriter log, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (log)
        {
            log.WriteLine(line);
        }
    }

    private void WaitUntilReady()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"Appium exited with code {_process.ExitCode}. See {Path.Combine(UITestSettings.ResultsDirectory, "appium.log")}.");
            }

            try
            {
                if (http.GetAsync(new Uri(Url, "status")).Result.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (AggregateException)
            {
                // Not listening yet.
            }

            Thread.Sleep(500);
        }

        throw new TimeoutException("Appium did not start within 60 seconds.");
    }

    public void Dispose()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }

        _process.Dispose();
        lock (_log)
        {
            _log.Dispose();
        }
    }
}
