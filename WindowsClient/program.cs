using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using CommunityToolkit.WinUI.Notifications;

class Settings
{
    public string AppUserModelId { get; set; } = "com.example.notifier";
    public string AppDisplayName { get; set; } = "Notifier";
    public string ApiBaseUrl { get; set; } = "https://node-emsphere.onrender.com/";
    public string UserId { get; set; } = "user123";
    public string ApiToken { get; set; } = "dev-token";
    public int PollIntervalMs { get; set; } = 5000;
}

class Program
{
    static readonly string ShortcutName = "eMSphere Notifier.lnk";

    static async Task Main()
    {
        var settings = LoadSettings();

        // Required for Win32 toast: Start Menu shortcut + AppID
        // EnsureStartMenuShortcut(settings);

        // Show a “started” toast (optional)
        new ToastContentBuilder().AddText("Notifier started").AddText("Listening for messages…").Show();

        var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);

        Console.WriteLine("Windows notifier running. Ctrl+C to exit.");
        while (true)
        {
            try
            {
                var url = $"{settings.ApiBaseUrl.TrimEnd('/')}/api/poll?userId={Uri.EscapeDataString(settings.UserId)}";
                var json = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in items.EnumerateArray())
{
    var title = it.GetProperty("title").GetString() ?? "Notification";
    var message = it.GetProperty("message").GetString() ?? "";

    new ToastContentBuilder()
        .AddAppLogoOverride(new Uri(Path.Combine(AppContext.BaseDirectory, "emSphereLogo.png")), ToastGenericAppLogoCrop.Circle)
        .AddText(title, hintMaxLines: 1)
        .AddText(message)
        .AddText("eMSphere Notifications", hintStyle: AdaptiveTextStyle.CaptionSubtle)
        .AddButton(new ToastButton()
            .SetContent("Open App")
            .AddArgument("action", "open")
            .SetBackgroundActivation())
        .AddButton(new ToastButtonDismiss("Dismiss"))
        // Make it persistent in Action Center
        .SetToastScenario(ToastScenario.Default)   // Default scenario keeps it in Action Center
        .SetToastDuration(ToastDuration.Long)     // Optional: makes it linger longer
        .Show(toast =>
        {
            toast.Tag = Guid.NewGuid().ToString();
            toast.Group = "eMSphere";
            // Do NOT set ExpirationTime or set it far in the future if you want persistence
            // toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(30); 
        });
}

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Poll error: {ex.Message}");
            }
            await Task.Delay(settings.PollIntervalMs);
        }
    }

    static Settings LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return new Settings();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Settings();
    }

    // Create Start Menu shortcut so toasts attribute to this app (critical for Win32).
    static void EnsureStartMenuShortcut(Settings s)
    {
        string startMenuDir = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        string programsDir = Path.Combine(startMenuDir, "Programs");
        Directory.CreateDirectory(programsDir);

        string shortcutPath = Path.Combine(programsDir, "eMSphere Notifier.lnk");
        if (File.Exists(shortcutPath)) return;

        string exePath = Process.GetCurrentProcess().MainModule!.FileName;

        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
        dynamic lnk = shell.CreateShortcut(shortcutPath);
        lnk.TargetPath = exePath;
        lnk.Arguments = "";
        lnk.WorkingDirectory = Path.GetDirectoryName(exePath);
        lnk.WindowStyle = 1;
        lnk.Description = s.AppDisplayName;

        // Set AppUserModelID on shortcut to bind toast identity
        lnk.AppUserModelID = s.AppUserModelId;
        lnk.Save();
    }
}
