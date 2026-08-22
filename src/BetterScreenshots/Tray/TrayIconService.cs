using System.Drawing;
using System.Windows.Forms;

namespace BetterScreenshots.Tray;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;
    public TrayIconService(Action capture, Action copyText, Action fullscreen, Action activeWindow, Action<int> delayedCapture, Action openFolder, Action openSettings, Action exit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Capture region", null, (_, _) => capture());
        menu.Items.Add("Copy text", null, (_, _) => copyText());
        menu.Items.Add("Capture full screen", null, (_, _) => fullscreen());
        menu.Items.Add("Capture active window", null, (_, _) => activeWindow());
        var delay = new ToolStripMenuItem("Delayed capture");
        foreach (var seconds in new[] { 3, 5, 10 }) delay.DropDownItems.Add($"{seconds} seconds", null, (_, _) => delayedCapture(seconds));
        menu.Items.Add(delay);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open screenshots folder", null, (_, _) => openFolder());
        menu.Items.Add("Settings", null, (_, _) => openSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit Better Screenshots", null, (_, _) => exit());
        _icon = new NotifyIcon { Icon = SystemIcons.Application, Text = "Better Screenshots", ContextMenuStrip = menu, Visible = true };
        _icon.DoubleClick += (_, _) => openSettings();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
