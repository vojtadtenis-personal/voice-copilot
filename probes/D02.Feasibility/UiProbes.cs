using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace D02.Feasibility;

internal static class UiProbes
{
    public static Task RunAsync(ProbeResults results, bool canary)
    {
        if (canary && (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
            Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") != "vojtadtenis-personal/voice-copilot"))
        {
            results.Add("UI.CANARY_HOST", "BLOCKED", "Real clipboard/window/input tests are restricted to this repository's disposable public runner.");
            return Task.CompletedTask;
        }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                RunSta(results, canary);
                completion.TrySetResult();
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(60));
    }

    private static void RunSta(ProbeResults results, bool canary)
    {
        results.Check("UI.STA", Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Dedicated STA thread; WPF objects use their owning dispatcher.");
        var composer = new TextBox { Text = "owned draft", AcceptsReturn = true, IsUndoEnabled = true };
        composer.Select(0, 5);
        var transcript = new TextBlock { Text = "background transcript" };
        transcript.Text += "\nstreamed output";
        results.Check("UI.SELECTION", composer.SelectedText == "owned" && composer.Text == "owned draft",
            "Updating a separate transcript preserves composer text and selection; diagnostic controls, not product UI.");
        results.Check("UI.PNG_ENCODING", RoundTripImage(realClipboard: false), "Injected owned bitmap PNG-encodes and decodes; local clipboard untouched.");
        if (!canary)
        {
            results.Add("UI.LOCAL_LIMITS", "OBSERVED", "No shown window, OS keyboard/focus manipulation or clipboard API call on shared host.");
            return;
        }
        results.Add("UI.RUNNER", "OBSERVED", $"userInteractive={Environment.UserInteractive}; sessionId={System.Diagnostics.Process.GetCurrentProcess().SessionId}");
        if (!Environment.UserInteractive)
        {
            results.Add("UI.UNATTENDED", "FAILED", "Runner has no interactive desktop; UIA/clipboard coverage blocked, not a manual QA handoff.");
            return;
        }

        var before = GetForegroundWindow();
        var invoked = 0;
        var keyEvents = 0;
        var button = new Button { Content = "Own invoke", Height = 32 };
        AutomationProperties.SetAutomationId(button, "D02.Invoke");
        AutomationProperties.SetAutomationId(composer, "D02.Composer");
        button.Click += (_, _) => invoked++;
        var panel = new StackPanel();
        panel.Children.Add(composer);
        panel.Children.Add(button);
        panel.Children.Add(transcript);
        var window = new Window { Title = "D02 owned canary", Content = panel, Width = 400, Height = 200, ShowActivated = false };
        window.PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.F6)
                keyEvents++;
        };
        try
        {
            window.Show();
            PumpUntil(Task.Delay(300));
            var handle = new WindowInteropHelper(window).Handle;
            results.Check("UI.NONACTIVATING_WINDOW", handle != IntPtr.Zero && GetForegroundWindow() == before,
                "ShowActivated=false: owned HWND shown without foreground-handle change.");
            var automation = Task.Run(() =>
            {
                var root = AutomationElement.FromHandle(handle);
                var element = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "D02.Invoke"));
                var editor = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "D02.Composer"));
                if (element is null || editor is null)
                    throw new InvalidOperationException("Owned UIA controls missing.");
                ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue("UIA synthetic draft");
                ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            });
            PumpUntil(automation);
            automation.GetAwaiter().GetResult();
            PumpUntil(Task.Delay(200));
            results.Check("UI.AUTOMATION", invoked == 1 && composer.Text == "UIA synthetic draft",
                "Built-in UIAutomation finds own accessible controls, sets editor value and invokes button from MTA.");
            var posted = PostMessage(handle, 0x0100, new IntPtr(0x75), IntPtr.Zero); // WM_KEYDOWN, VK_F6, own HWND only.
            PumpUntil(Task.Delay(200));
            results.Check("UI.OWN_KEYBOARD", posted && keyEvents > 0,
                $"ownHwndPostMessage={posted}; WPF_F6Events={keyEvents}; no global SendInput or activation");
            results.Check("UI.CLIPBOARD", RoundTripImage(realClipboard: true), "Synthetic bitmap -> real disposable-runner clipboard -> owned PNG -> decode.");
            results.Check("UI.FOREGROUND", GetForegroundWindow() == before, "Foreground HWND unchanged after own-window UIA, directed keyboard and clipboard.");
        }
        finally
        {
            window.Close();
            Clipboard.Clear();
            results.Check("UI.FOREGROUND_CLOSED", GetForegroundWindow() == before, "Foreground HWND unchanged after closing the owned window.");
        }
    }

    private static bool RoundTripImage(bool realClipboard)
    {
        byte[] pixels = [0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255, 255, 255, 255, 255];
        var original = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
        BitmapSource image = original;
        if (realClipboard)
        {
            Clipboard.SetImage(original);
            image = Clipboard.GetImage() ?? throw new InvalidOperationException("Clipboard image unavailable.");
        }
        var directory = Path.GetFullPath(Path.Combine(".assets", "images"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, realClipboard ? "canary.png" : "injected.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var stream = File.Create(path))
            encoder.Save(stream);
        using var read = File.OpenRead(path);
        var decoded = new PngBitmapDecoder(read, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var normalized = new FormatConvertedBitmap(decoded, PixelFormats.Bgra32, null, 0);
        var roundTrip = new byte[pixels.Length];
        normalized.CopyPixels(roundTrip, 8, 0);
        return normalized.PixelWidth == 2 && normalized.PixelHeight == 2 && pixels.SequenceEqual(roundTrip);
    }

    private static void PumpUntil(Task task)
    {
        var frame = new DispatcherFrame();
        var timeout = DateTime.UtcNow.AddSeconds(20);
        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(20), DispatcherPriority.Background,
            (_, _) =>
            {
                if (task.IsCompleted || DateTime.UtcNow >= timeout)
                    frame.Continue = false;
            }, Dispatcher.CurrentDispatcher);
        Dispatcher.PushFrame(frame);
        timer.Stop();
        if (!task.IsCompleted)
            throw new TimeoutException("Owned UIA operation timed out.");
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
