using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MinitBrowser;

public sealed class SavedTab
{
    public string Url { get; set; } = "minit://home";
    public string Title { get; set; } = "Yeni sekme";
}

public sealed partial class BrowserWindow : ModernWindow
{
    Palette palette = Palette.For("Koyu");
    BrowserSettings settings = new();
    string navigationMessage = "Hazır";
    readonly ToolTip hints = new();
    readonly FlowLayoutPanel strip = new() { Dock = DockStyle.Top, Height = 48, WrapContents = false, AutoScroll = true, Padding = new Padding(8, 6, 8, 4) };
    readonly Panel host = new() { Dock = DockStyle.Fill };
    readonly TextBox address = new() { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12), Dock = DockStyle.Fill, AccessibleName = "Adres veya arama" };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 2000 };
    readonly List<TabState> tabs = [];
    readonly Button plus = new MinitButton() { Text = "+", Size = new Size(38, 33), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(25, 37, 40), AccessibleName = "Yeni sekme" };
    readonly string dataPath;
    readonly bool selfTest;
    Task<CoreWebView2Environment>? environment;
    TabState? current;
    bool closing;
    const string Home = "minit://home";
    sealed class TabState
    {
        public SavedTab Saved = new();
        public WebView2? View;
        public Button Button = null!;
        public Panel Holder = null!;
        public DateTime HiddenSince;
        public bool Pinned;
        public HashSet<CoreWebView2DownloadOperation> Downloads = [];
    }
    public BrowserWindow(bool test, bool visual = false)
    {
        selfTest = test;
        dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MinitBrowser" + (test || visual ? "-Test-" + Environment.ProcessId : ""));
        Directory.CreateDirectory(dataPath);
        if(!test && !visual)MigratePreferences();
        settings = LoadSettings(); palette = Palette.For(settings.Theme);
        Text = "Minit • Hafif gezinti"; Size = new Size(1200, 820); MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterScreen; BackColor = palette.Background; ForeColor = palette.Text;
        Font = new Font("Segoe UI", 10); KeyPreview = true;
        var nav = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = 7, RowCount = 1, Padding = new Padding(10, 8, 10, 8) };
        nav.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (int width in new[] { 40, 40, 40 }) nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int width in new[] { 50, 115, 100 }) nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
        nav.Controls.Add(ActionButton("←", () => { if (current?.View?.CanGoBack == true) current.View.GoBack(); }, "Geri"), 0, 0);
        nav.Controls.Add(ActionButton("→", () => { if (current?.View?.CanGoForward == true) current.View.GoForward(); }, "İleri"), 1, 0);
        nav.Controls.Add(ActionButton("↻", () => current?.View?.Reload(), "Yenile"), 2, 0);
        var field = new CardPanel { Dock = DockStyle.Fill, BackColor = palette.Surface, BorderColor = palette.Border, Padding = new Padding(14, 9, 8, 2) }; field.Controls.Add(address); nav.Controls.Add(field, 3, 0);
        nav.Controls.Add(ActionButton("Git", NavigateAddress), 4, 0);
        nav.Controls.Add(ActionButton("★ Yer imleri", BookmarkMenu), 5, 0);
        nav.Controls.Add(ActionButton("Ayarlar", OpenSettings), 6, 0);
        address.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; NavigateAddress(); } };
        strip.BackColor = palette.Surface; status.BackColor = palette.Surface;
        var menu = ActionButton("•••", MainMenu,"Tarayıcı menüsü"); menu.Dock=DockStyle.Right; menu.Width=48; status.Controls.Add(menu);
        Controls.Add(host); Controls.Add(status); Controls.Add(nav); Controls.Add(strip);
        KeyDown += (_, e) => { if (!e.Control) return; if (e.KeyCode == Keys.L) { address.Focus(); address.SelectAll(); e.SuppressKeyPress = true; } if (e.KeyCode == Keys.H) { ManageRecords(true); e.SuppressKeyPress = true; } if (e.KeyCode == Keys.T) { AddTab(Home); e.SuppressKeyPress = true; } if (e.KeyCode == Keys.W) { CloseTab(current); e.SuppressKeyPress = true; } };
        BuildChrome("BROWSER  /  PREVIEW 0.3");
        ApplyTheme();
        plus.Click += (_, _) => AddTab(Home);
        timer.Tick += (_, _) => TickMemory();
        Shown += async (_, _) => { if (selfTest) { await RunSelfTest(); return; } Restore(); ApplyTheme(); timer.Start(); };
        FormClosing += (_, _) => { closing = true; timer.Stop(); if (!selfTest) SaveSession(); foreach (var t in tabs) t.View?.Dispose(); };
    }
    Button ActionButton(string text, Action action, string? accessible = null)
    {
        var b = new MinitButton { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = palette.Surface, ForeColor = palette.Text, Cursor = Cursors.Hand, AccessibleName = accessible ?? text, Margin = new Padding(2) };
        hints.SetToolTip(b, accessible ?? text); b.FlatAppearance.BorderSize = 0; b.Click += (_, _) => action(); return b;
    }
    Task<CoreWebView2Environment> EnvironmentTask() => environment ??= CoreWebView2Environment.CreateAsync(null, Path.Combine(dataPath, "WebData"));
    void Restore()
    {
        try { if (!settings.RestoreSession) { AddTab(Home); return; } var saved = JsonSerializer.Deserialize<List<SavedTab>>(File.ReadAllText(Path.Combine(dataPath, "session.json"))); foreach (var s in saved?.Take(40) ?? []) AddTab(IsWebUrl(s.Url) || s.Url == Home ? s.Url : Home, s.Title, false); } catch { }
        if (tabs.Count == 0) AddTab(Home); else _ = Activate(tabs[0]);
    }
    TabState AddTab(string url, string title = "Yeni sekme", bool activate = true)
    {
        var t = new TabState { Saved = new SavedTab { Url = url, Title = title } };
        t.Button = new MinitButton { Size = new Size(190, 33), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(47, 61, 63), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 4, 0), Margin = new Padding(0, 0, 5, 0) };
        t.Holder = new Panel { Size=new Size(218,36), Margin=new Padding(0,0,6,0), BackColor=palette.Surface };
        t.Button.Dock=DockStyle.Fill; t.Button.Margin=Padding.Empty;
        var close = new MinitButton { Text="×", Dock=DockStyle.Right, Width=30, BackColor=palette.Raised, ForeColor=palette.Muted, AccessibleName="Sekmeyi kapat" };
        close.Click+=(_,_)=>CloseTab(t); hints.SetToolTip(close,"Sekmeyi kapat • Ctrl+W");
        t.Holder.Controls.Add(t.Button); t.Holder.Controls.Add(close);
        t.Button.FlatAppearance.BorderSize = 0;
        t.Button.Click += async (_, _) => await Activate(t);
        t.Button.MouseUp += (_, e) => { if (e.Button == MouseButtons.Middle) CloseTab(t); if (e.Button == MouseButtons.Right) { var m = new ContextMenuStrip(); m.Items.Add(t.Pinned ? "Bellekte tutmayı kapat" : "Bellekte tut (form / medya)", null, (_, _) => { t.Pinned = !t.Pinned; UpdateTabs(); }); m.Items.Add("Sekmeyi kapat", null, (_, _) => CloseTab(t)); m.Show(t.Button, e.Location); } };
        tabs.Add(t); UpdateTabs(); if (activate) _ = Activate(t); return t;
    }
    void UpdateTabs()
    {
        bool rebuild = strip.Controls.Count != tabs.Count + 1 || tabs.Any(t => !strip.Controls.Contains(t.Holder));
        if (rebuild) strip.Controls.Clear();
        foreach (var t in tabs) { string name = (t.View == null && t != current ? "○ " : "") + (t.Pinned ? "• " : "") + t.Saved.Title; hints.SetToolTip(t.Button,t.Saved.Title+"\n"+t.Saved.Url); t.Button.Text = name.Length > 23 ? name[..22] + "…" : name; t.Button.BackColor = t == current ? palette.Accent : palette.Raised; t.Button.ForeColor = t == current ? Color.FromArgb(18,43,29) : palette.Text; if (rebuild) strip.Controls.Add(t.Holder); }
        if (rebuild) strip.Controls.Add(plus);
    }
    async Task Activate(TabState t)
    {
        if (closing || !tabs.Contains(t)) return;
        if (current != t && current?.View != null) { current.View.Visible = false; current.HiddenSince = DateTime.UtcNow; }
        current = t; ClearHost(); address.Text = t.Saved.Url == Home ? "" : t.Saved.Url;
        if (t.Saved.Url == Home && t.View == null) { ShowHome(); UpdateTabs(); return; }
        if (t.View != null) { host.Controls.Add(t.View); t.View.Visible = true; UpdateTabs(); UpdateNavigation(); return; }
        var view = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White }; t.View = view; host.Controls.Add(view); UpdateTabs();
        try
        {
            await view.EnsureCoreWebView2Async(await EnvironmentTask());
            if (closing || t.View != view) return;
            var core = view.CoreWebView2;
            core.Settings.IsStatusBarEnabled = true;
            ApplyViewSettings(view);
            core.NavigationStarting += (_, e) => { if (!IsWebUrl(e.Uri) && e.Uri != "about:blank" && !e.Uri.StartsWith("data:text/html",StringComparison.OrdinalIgnoreCase)) { e.Cancel=true; navigationMessage="Bu adres türü desteklenmiyor."; } };
            core.HistoryChanged += (_,_)=>UpdateNavigation();
            core.DocumentTitleChanged += (_, _) => { t.Saved.Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "Sayfa" : core.DocumentTitle; UpdateTabs(); };
            core.SourceChanged += (_, _) => { t.Saved.Url = core.Source; if (current == t) address.Text = core.Source; };
            core.NavigationCompleted += (_, e) => { if (current == t) navigationMessage=e.IsSuccess ? "Hazır" : "Sayfa açılamadı: "+e.WebErrorStatus; if(e.IsSuccess && settings.SaveHistory && IsWebUrl(core.Source)) RecordHistory(core.Source, core.DocumentTitle); UpdateNavigation(); };
            core.NewWindowRequested += (_, e) => { e.Handled = true; if (IsWebUrl(e.Uri)) AddTab(e.Uri); };
            core.PermissionRequested += (_, e) => { e.State = MessageBox.Show(this, $"{e.Uri}\n\n{e.PermissionKind} izni istiyor. İzin verilsin mi?", "Site izni", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny; };
            core.DownloadStarting += (_, e) => { var op = e.DownloadOperation; t.Downloads.Add(op); op.StateChanged += (_, _) => { if (op.State == CoreWebView2DownloadState.InProgress) t.Downloads.Add(op); else t.Downloads.Remove(op); }; };
            core.ProcessFailed += (_, _) => { if (current == t) status.Text = "Sayfa işlemi durdu. Sekmeyi kapatıp yeniden açabilirsin."; };
            core.Navigate(t.Saved.Url);
        }
        catch (Exception ex) { if (t.View == view) { view.Dispose(); t.View = null; } if (!closing && current == t) { ClearHost(); host.Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "Web motoru başlatılamadı.\nMicrosoft Edge WebView2 Runtime kurulu olmalı.\n\n" + ex.Message }); } if (selfTest) throw; }
        if (current != t && t.View == view) view.Visible = false;
    }
    static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http");
    internal static string Resolve(string value, string engine = "Google")
    {
        value = value.Trim(); if (IsWebUrl(value)) return value;
        if (!value.Contains(' ') && !value.Contains("://") && (value.Contains('.') || value.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))) { var candidate = (value.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase) ? "http://" : "https://") + value; if (IsWebUrl(candidate)) return candidate; }
        return (engine == "DuckDuckGo" ? "https://duckduckgo.com/?q=" : engine == "Bing" ? "https://www.bing.com/search?q=" : "https://www.google.com/search?q=") + Uri.EscapeDataString(value);
    }
    void NavigateAddress()
    {
        if (current == null) return; current.Saved.Url = Resolve(address.Text,settings.SearchEngine);
        if (current.View?.CoreWebView2 != null) current.View.CoreWebView2.Navigate(current.Saved.Url); else _ = Activate(current);
    }
    void ClearHost()
    {
        foreach (Control c in host.Controls.Cast<Control>().ToArray()) { host.Controls.Remove(c); if (c is not WebView2) c.Dispose(); }
    }
    bool Unload(TabState t)
    {
        if (t == current || t.Pinned || t.Downloads.Count > 0 || t.View == null || t.View.CoreWebView2 == null || t.View.CoreWebView2.IsDocumentPlayingAudio) return false;
        t.View.Dispose(); t.View = null; return true;
    }
    void TickMemory()
    {
        foreach (var t in tabs.ToArray()) if (settings.SleepTabs && t != current && t.View != null && DateTime.UtcNow - t.HiddenSince > TimeSpan.FromSeconds(settings.SleepSeconds)) Unload(t);
        UpdateTabs(); UpdateNavigation();
        try { long bytes = Process.GetCurrentProcess().WorkingSet64; if (environment?.IsCompletedSuccessfully == true) foreach (var p in environment.Result.GetProcessInfos()) { try { using var process = Process.GetProcessById(p.ProcessId); bytes += process.WorkingSet64; } catch { } } status.Text = (settings.ShowMemory ? $"RAM ≈ {bytes / 1048576} MB   •   " : "") + $"{tabs.Count(t => t.View != null)} aktif / {tabs.Count} sekme   •   " + (settings.SleepTabs ? $"Uyku {settings.SleepSeconds} sn" : "Uyku kapalı") + "   •   " + navigationMessage; } catch { }
    }
    void CloseTab(TabState? t)
    {
        if (t == null) return; t.View?.Dispose(); t.Holder.Dispose(); tabs.Remove(t);
        if (t == current) { current = null; if (tabs.Count == 0) AddTab(Home); else _ = Activate(tabs[^1]); } UpdateTabs();
    }
    List<SavedTab> Bookmarks()
    {
        try { return JsonSerializer.Deserialize<List<SavedTab>>(File.ReadAllText(Path.Combine(dataPath, "bookmarks.json"))) ?? []; } catch { return []; }
    }
    void BookmarkMenu()
    {
        var menu = new ContextMenuStrip(); menu.Items.Add("Yer imlerini yönet",null,(_,_)=>ManageRecords(false)); menu.Items.Add("Bu sayfayı kaydet", null, (_, _) => { if (current == null || !IsWebUrl(current.Saved.Url)) return; var list = Bookmarks(); if (!list.Any(b => b.Url == current.Saved.Url)) list.Add(new SavedTab { Url = current.Saved.Url, Title = current.Saved.Title }); WriteJson("bookmarks.json", list); });
        foreach (var b in Bookmarks()) menu.Items.Add(b.Title.Length > 65 ? b.Title[..65] : b.Title, null, (_, _) => AddTab(b.Url)); menu.Show(Cursor.Position);
    }
    void MainMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Ayarlar",null,(_,_)=>OpenSettings());
        menu.Items.Add("Geçmiş",null,(_,_)=>ManageRecords(true));
        menu.Items.Add("Yeni sekme", null, (_, _) => AddTab(Home));
        menu.Items.Add("Pasif sekmeleri şimdi boşalt", null, (_, _) => { foreach (var t in tabs) Unload(t); UpdateTabs(); });
        menu.Items.Add("Bu sekmeyi kapat", null, (_, _) => CloseTab(current));
        menu.Items.Add("İndirmeler", null, (_, _) => { if (current?.View?.CoreWebView2 != null) current.View.CoreWebView2.OpenDefaultDownloadDialog(); });
        menu.Items.Add("Hakkında", null, (_, _) => MessageBox.Show(this, "Minit 0.3 • Windows tarayıcı prototipi\nMicrosoft Edge WebView2 motoru\n\nPasif sekmelerin sayfa durumu saklanmaz. Formlar için Bellekte tut kullan.\nRAM değeri işlem çalışma kümelerinin yaklaşık toplamıdır; paylaşılan bellek iki kez sayılabilir.\nChrome karşılaştırması yapılmadı. Eklenti / senkronizasyon yok.", "Minit")); menu.Show(Cursor.Position);
    }
    bool WriteJson<T>(string name, T value)
    {
        try { var path = Path.Combine(dataPath, name); File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(value)); File.Move(path + ".tmp", path, true); return true; } catch(Exception ex) { navigationMessage="Kaydedilemedi: "+ex.Message; return false; }
    }
    void SaveSession() => WriteJson("session.json", tabs.Select(t => t.Saved).ToList());
    async Task RunSelfTest()
    {
        var lines = new List<string>();
        try
        {
            if (Resolve("example.com") != "https://example.com" || !Resolve("hello world").Contains("q=hello%20world") || !Resolve("javascript:alert(1)").StartsWith("https://www.google.com/")) throw new Exception("Address resolver failed");
            lines.Add("PASS address resolution and blocked script scheme");
            var first = AddTab("about:blank", activate: false); await Activate(first);
            var loaded = new TaskCompletionSource<bool>(); first.View!.CoreWebView2.DocumentTitleChanged += (_, _) => { if (first.View.CoreWebView2.DocumentTitle == "Minit Test") loaded.TrySetResult(true); };
            first.View.CoreWebView2.NavigateToString("<html><title>Minit Test</title><body><input id='v' value='working'><h1>Render test</h1></body></html>");
            if (!await loaded.Task.WaitAsync(TimeSpan.FromSeconds(20))) throw new Exception("Navigation failed");
            var result = await first.View.CoreWebView2.ExecuteScriptAsync("document.getElementById('v').value"); if (result != "\"working\"") throw new Exception("DOM rendering failed"); lines.Add("PASS actual WebView2 rendering and JavaScript");
            first.Saved.Url = "about:blank";
            var second = AddTab(Home, activate: false); await Activate(second);
            first.Pinned = true; if (Unload(first)) throw new Exception("Pinned tab unloaded"); first.Pinned = false;
            first.HiddenSince = DateTime.UtcNow.AddSeconds(-settings.SleepSeconds-1); TickMemory(); if (first.View != null) throw new Exception("Timed background unload failed"); if (Unload(second)) throw new Exception("Active tab unloaded"); lines.Add("PASS configured sleep expiry disposes inactive renderer; pinned and active tabs protected");
            PerformLayout(); Refresh();
            foreach (var b in Controls.OfType<TableLayoutPanel>().SelectMany(p => p.Controls.OfType<Button>())) { if (b.Width < 20 || b.Height < 20) throw new Exception("Navigation button layout failed: " + b.Text + " " + b.Bounds); }
            lines.Add("PASS navigation button layout");
            using (var shot = new Bitmap(Width, Height)) { DrawToBitmap(shot, new Rectangle(0, 0, Width, Height)); shot.Save(Path.Combine(AppContext.BaseDirectory, "preview.png")); }
            await Activate(first); if (first.View?.CoreWebView2 == null) throw new Exception("Reload failed"); lines.Add("PASS unloaded tab recreated");
            SaveSession(); if (JsonSerializer.Deserialize<List<SavedTab>>(File.ReadAllText(Path.Combine(dataPath, "session.json")))?.Count != 2) throw new Exception("Session persistence failed"); lines.Add("PASS session URLs persisted");
            await TestSettingsAndViews(lines);
            lines.Add("SUCCESS");
        }
        catch (Exception ex) { lines.Add("FAIL " + ex); Environment.ExitCode = 1; }
        File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "self-test.txt"), lines); Close();
    }
}
