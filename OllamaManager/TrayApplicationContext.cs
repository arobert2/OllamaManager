using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace OllamaManager;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon _trayIcon;
    private ContextMenuStrip _menu;
    private ToolStripMenuItem _toggleItem;
    private System.Windows.Forms.Timer _pollTimer;
    private bool _isRunning = false;

    private readonly string[] MODELS;
    private const string OLLAMA_HOST = "http://localhost:11434";

    public TrayApplicationContext()
    {
        _toggleItem = new ToolStripMenuItem("Start Model", null, OnToggle);

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_toggleItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, OnExit);

        _trayIcon = new NotifyIcon()
        {
            Icon = CreateIcon(Color.Red),
            ContextMenuStrip = _menu,
            Visible = true,
            Text = "Ollama Manager — Model not loaded"
        };

        _pollTimer = new System.Windows.Forms.Timer();
        _pollTimer.Interval = 5000;
        _pollTimer.Tick += async (s, e) => await PollModelStatus();
        _pollTimer.Start();

        _ = PollModelStatus();

        MODELS = JsonDocument.Parse(File.ReadAllText("models.json")).RootElement.GetProperty("models").EnumerateArray().Select(m => m.GetString()).Where(m => m != null).ToArray()!;
    }

    private Icon CreateIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        return Icon.FromHandle(bmp.GetHicon());
    }

    private async Task PollModelStatus()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{OLLAMA_HOST}/api/ps");
            var json = await response.Content.ReadAsStringAsync();
            var ps = System.Text.Json.JsonDocument.Parse(json);
            _isRunning = ps.RootElement.GetProperty("models").GetArrayLength() > 0;
            UpdateUI();
        }
        catch
        {
            _isRunning = false;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        _trayIcon.Icon = CreateIcon(_isRunning ? Color.Green : Color.Red);
        _trayIcon.Text = _isRunning ? "Ollama Manager — Model loaded" : "Ollama Manager — Model not loaded";
        _toggleItem.Text = _isRunning ? "Stop Model" : "Start Model";
    }

    private async void OnToggle(object? sender, EventArgs e)
    {
        try
        {
            StringBuilder sb = new();
            foreach (var mods in MODELS)
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);

                var body = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        model = mods,
                        messages = Array.Empty<object>(),
                        keep_alive = _isRunning ? 0 : -1
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{OLLAMA_HOST}/api/chat", body);

                if (!response.IsSuccessStatusCode)
                    sb.Append($"{mods} {response.StatusCode}");
            }
            if (sb.Length > 0)
                MessageBox.Show($"Request failed: {sb.ToString()}", "Ollama Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);

            await PollModelStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Ollama Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }
}