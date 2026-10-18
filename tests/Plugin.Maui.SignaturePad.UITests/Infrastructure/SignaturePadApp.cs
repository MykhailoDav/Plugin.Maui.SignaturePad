using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Plugin.Maui.SignaturePad.UITests.Infrastructure;

/// <summary>
/// The host app driven through Appium. Elements are found by their automation id (see ScenarioPage in the host app for
/// the ids of the pad, the action buttons and the state texts).
/// </summary>
public sealed class SignaturePadApp : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly AppiumDriver _driver;

    private SignaturePadApp(AppiumDriver driver) => _driver = driver;

    /// <summary>Whether the platform's pointer is a finger; otherwise it is a mouse.</summary>
    public static bool IsTouch => UITestSettings.Platform is TestPlatform.iOS or TestPlatform.Android;

    public static SignaturePadApp Launch(Uri server)
    {
        var options = new AppiumOptions { App = UITestSettings.AppPath };
        if (UITestSettings.PlatformVersion is { } platformVersion)
            options.PlatformVersion = platformVersion;

        options.AddAdditionalAppiumOption("newCommandTimeout", 600);

        // Building and starting the driver's agent on the device can take minutes the first time.
        var sessionTimeout = TimeSpan.FromMinutes(10);

        AppiumDriver driver;
        switch (UITestSettings.Platform)
        {
            case TestPlatform.iOS:
                options.PlatformName = "iOS";
                options.AutomationName = "XCUITest";
                options.DeviceName = UITestSettings.Device ?? "iPhone 17";
                options.AddAdditionalAppiumOption("wdaLaunchTimeout", 600_000);
                options.AddAdditionalAppiumOption("wdaConnectionTimeout", 600_000);
                options.AddAdditionalAppiumOption("simulatorStartupTimeout", 300_000);
                options.AddAdditionalAppiumOption("reduceMotion", true);
                options.AddAdditionalAppiumOption("shouldTerminateApp", true);
                driver = new IOSDriver(server, options, sessionTimeout);
                break;

            case TestPlatform.Android:
                options.PlatformName = "Android";
                options.AutomationName = "UiAutomator2";
                options.DeviceName = "Android Emulator";
                if (UITestSettings.Device is { } avd)
                {
                    options.AddAdditionalAppiumOption("avd", avd);
                    options.AddAdditionalAppiumOption("avdLaunchTimeout", 300_000);
                    options.AddAdditionalAppiumOption("avdReadyTimeout", 300_000);
                }

                options.AddAdditionalAppiumOption("disableWindowAnimation", true);

                // Every build has the same version code, so the app is installed again rather than kept.
                options.AddAdditionalAppiumOption("enforceAppInstall", true);
                options.AddAdditionalAppiumOption("uiautomator2ServerInstallTimeout", 120_000);
                options.AddAdditionalAppiumOption("adbExecTimeout", 120_000);
                driver = new AndroidDriver(server, options, sessionTimeout);
                break;

            case TestPlatform.MacCatalyst:
                options.App = null;
                options.PlatformName = "mac";
                options.AutomationName = "Mac2";
                options.AddAdditionalAppiumOption("bundleId", UITestSettings.AppId);
                options.AddAdditionalAppiumOption("appPath", UITestSettings.AppPath);
                options.AddAdditionalAppiumOption("showServerLogs", true);
                driver = new MacDriver(server, options, sessionTimeout);
                break;

            default:
                options.PlatformName = "Windows";
                options.AutomationName = "Windows";
                options.DeviceName = "WindowsPC";
                driver = new WindowsDriver(server, options, sessionTimeout);
                break;
        }

        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        return new SignaturePadApp(driver);
    }

    // ── Elements ─────────────────────────────────────────────────────────────

    /// <summary>The element with <paramref name="automationId"/>, waiting until it exists.</summary>
    public IWebElement Element(string automationId, TimeSpan? timeout = null) =>
        Wait(timeout).Until(driver => driver.FindElements(ByAutomationId(automationId)).FirstOrDefault())
            ?? throw new NoSuchElementException(automationId);

    public bool Exists(string automationId) => _driver.FindElements(ByAutomationId(automationId)).Count > 0;

    /// <summary>The text of the label with <paramref name="automationId"/>.</summary>
    public string Text(string automationId)
    {
        var element = Element(automationId);

        // A Mac Catalyst label exposes its text as its value.
        return UITestSettings.Platform == TestPlatform.MacCatalyst
            ? element.GetAttribute("value") ?? element.Text
            : element.Text;
    }

    /// <summary>Waits until the label with <paramref name="automationId"/> shows <paramref name="expected"/>, and returns what it shows.</summary>
    public string WaitForText(string automationId, string expected, TimeSpan? timeout = null)
    {
        var text = string.Empty;
        try
        {
            Wait(timeout ?? TimeSpan.FromSeconds(5)).Until(_ => (text = Text(automationId)) == expected);
        }
        catch (WebDriverTimeoutException)
        {
            // The caller asserts on the returned text, which gives the better message.
        }

        return text;
    }

    /// <summary>Waits until the label with <paramref name="automationId"/> shows a text that satisfies <paramref name="condition"/>.</summary>
    public string WaitForText(string automationId, Func<string, bool> condition, TimeSpan? timeout = null)
    {
        var text = string.Empty;
        try
        {
            Wait(timeout ?? TimeSpan.FromSeconds(5)).Until(_ => condition(text = Text(automationId)));
        }
        catch (WebDriverTimeoutException)
        {
            // The caller asserts on the returned text, which gives the better message.
        }

        return text;
    }

    public void Tap(string automationId) => Element(automationId).Click();

    /// <summary>Taps the element that shows <paramref name="text"/>, such as a tab, which has no automation id.</summary>
    public void TapText(string text)
    {
        var by = UITestSettings.Platform == TestPlatform.Android
            ? By.XPath($"//*[@text='{text}' or @content-desc='{text}']")
            : MobileBy.AccessibilityId(text);
        (Wait(null).Until(driver => driver.FindElements(by).FirstOrDefault()) ?? throw new NoSuchElementException(text)).Click();
    }

    // ── Scenarios ────────────────────────────────────────────────────────────

    /// <summary>Opens the scenario of the host app named <paramref name="name"/> (see ScenarioCatalog).</summary>
    public void OpenScenario(string name)
    {
        if (!Exists("ScenarioEntry"))
        {
            if (Exists("BackButton"))
                Tap("BackButton");
            else
                Restart();
        }

        var entry = Element("ScenarioEntry");
        entry.Click();
        entry.Clear();
        entry.SendKeys(name);
        Tap("GoButton");

        var title = WaitForText("ScenarioTitle", name, DefaultTimeout);
        if (title != name)
            throw new InvalidOperationException($"The scenario '{name}' did not open (the title shows '{title}').");

        Element("Pad");
    }

    private void Restart()
    {
        _driver.TerminateApp(UITestSettings.AppId);
        _driver.ActivateApp(UITestSettings.AppId);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws one stroke through <paramref name="points"/>, given as fractions of the element's width and height, in
    /// <paramref name="duration"/>. The pointer moves in many small steps, as a finger does, so the pad gets the samples
    /// it would get from real input. It is a finger on iOS and Android and a mouse on the desktop.
    /// </summary>
    /// <param name="automationId">The element to draw on.</param>
    /// <param name="points">The stroke's corners, from (0, 0) at the top left to (1, 1) at the bottom right.</param>
    /// <param name="duration">How long the stroke takes.</param>
    public void Draw(string automationId, IReadOnlyList<PointF> points, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(points.Count, 1);

        var element = Element(automationId);
        var rect = new Rectangle(element.Location, element.Size);
        var kind = IsTouch ? PointerKind.Touch : PointerKind.Mouse;
        var button = IsTouch ? MouseButton.Touch : MouseButton.Left;

        // W3C input sources keep their kind for the whole session, so each kind needs a name of its own.
        var pointer = new PointerInputDevice(kind, kind.ToString().ToLowerInvariant());
        var stroke = new ActionSequence(pointer, 0);

        var start = ToViewport(rect, points[0]);
        stroke.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, start.X, start.Y, TimeSpan.Zero));
        stroke.AddAction(pointer.CreatePointerDown(button));

        // Every corner is split into steps of a few pixels, each taking the same share of the duration.
        var path = Interpolate(rect, points, stepPixels: 6);
        var step = path.Count > 0 ? duration / path.Count : TimeSpan.Zero;
        foreach (var point in path)
            stroke.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, point.X, point.Y, step));

        if (path.Count == 0)
            stroke.AddAction(pointer.CreatePause(TimeSpan.FromMilliseconds(80)));
        stroke.AddAction(pointer.CreatePointerUp(button));
        _driver.PerformActions([stroke]);
    }

    /// <summary>
    /// Draws a straight stroke with a stylus on Android. The UiAutomator2 driver turns every W3C pointer into a finger,
    /// so this goes through <c>adb shell input stylus</c>, which sends real stylus events (with full pressure).
    /// </summary>
    public void DrawWithStylus(string automationId, PointF from, PointF to, TimeSpan duration)
    {
        if (UITestSettings.Platform != TestPlatform.Android)
            throw new PlatformNotSupportedException("Only Android can be driven with a stylus.");

        var element = Element(automationId);
        var rect = new Rectangle(element.Location, element.Size);
        var start = ToViewport(rect, from);
        var end = ToViewport(rect, to);
        Adb.Run($"shell input stylus swipe {start.X} {start.Y} {end.X} {end.Y} {(int)duration.TotalMilliseconds}");
    }

    /// <summary>Draws a zigzag across the element, as a quick signature.</summary>
    public void Scribble(string automationId = "Pad", float top = 0.3f) =>
        Draw(automationId, [new(0.1f, top + 0.3f), new(0.3f, top), new(0.5f, top + 0.3f), new(0.7f, top), new(0.9f, top + 0.3f)], TimeSpan.FromMilliseconds(700));

    /// <summary>Touches the element at one point, as a dot.</summary>
    public void Dot(string automationId, float x, float y) => Draw(automationId, [new(x, y)], TimeSpan.Zero);

    private static Point ToViewport(Rectangle rect, PointF point) =>
        new(rect.X + (int)Math.Round(point.X * rect.Width), rect.Y + (int)Math.Round(point.Y * rect.Height));

    private static List<Point> Interpolate(Rectangle rect, IReadOnlyList<PointF> points, int stepPixels)
    {
        var path = new List<Point>();
        for (var i = 1; i < points.Count; i++)
        {
            var from = ToViewport(rect, points[i - 1]);
            var to = ToViewport(rect, points[i]);
            var distance = Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
            var steps = Math.Max(1, (int)(distance / stepPixels));
            for (var s = 1; s <= steps; s++)
            {
                path.Add(new Point(
                    from.X + (int)Math.Round((to.X - from.X) * (double)s / steps),
                    from.Y + (int)Math.Round((to.Y - from.Y) * (double)s / steps)));
            }
        }

        return path;
    }

    /// <summary>A quick swipe from one point of the element to another, given as fractions of its size.</summary>
    public void Swipe(string automationId, PointF from, PointF to) =>
        Draw(automationId, [from, to], TimeSpan.FromMilliseconds(250));

    // ── Screenshots ──────────────────────────────────────────────────────────

    /// <summary>
    /// A PNG screenshot of the element with <paramref name="automationId"/>, taken once two screenshots in a row are the
    /// same, so layout passes that are still running never end up in it.
    /// </summary>
    public byte[] Screenshot(string automationId)
    {
        var element = Element(automationId);
        var previous = ((ITakesScreenshot)element).GetScreenshot().AsByteArray;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            Thread.Sleep(300);
            var current = ((ITakesScreenshot)element).GetScreenshot().AsByteArray;
            if (current.AsSpan().SequenceEqual(previous))
                return current;

            previous = current;
        }

        return previous;
    }

    /// <summary>Saves the screen and the element tree, to see what the app showed when a test failed.</summary>
    public IReadOnlyList<string> SaveDiagnostics(string name)
    {
        Directory.CreateDirectory(UITestSettings.ResultsDirectory);
        var screen = Path.Combine(UITestSettings.ResultsDirectory, name + ".screen.png");
        var source = Path.Combine(UITestSettings.ResultsDirectory, name + ".source.xml");
        _driver.GetScreenshot().SaveAsFile(screen);
        File.WriteAllText(source, _driver.PageSource);
        return [screen, source];
    }

    private WebDriverWait Wait(TimeSpan? timeout) => new(_driver, timeout ?? DefaultTimeout)
    {
        PollingInterval = TimeSpan.FromMilliseconds(250),
    };

    // ── Elements (private) ───────────────────────────────────────────────────

    // MAUI makes the automation id the view's resource id on Android, and its accessibility identifier elsewhere.
    private static By ByAutomationId(string automationId) => UITestSettings.Platform == TestPlatform.Android
        ? By.Id(automationId)
        : MobileBy.AccessibilityId(automationId);

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
