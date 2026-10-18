using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace MinitBrowser;

public sealed class BrowserSettings
{
    public string Theme { get; set; } = "Koyu";
    public string SearchEngine { get; set; } = "DuckDuckGo";
    public bool TransparentEdges { get; set; } = true;
    public bool RestoreSession { get; set; } = true;
    public bool SleepTabs { get; set; } = true;
    public int SleepSeconds { get; set; } = 120;
    public int ZoomPercent { get; set; } = 100;
    public bool SaveHistory { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public string DownloadFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public void Normalize()
    {
        if (Theme != "Koyu" && Theme != "Açık") Theme = "Koyu";
        if (!new[] { "DuckDuckGo", "Google", "Bing" }.Contains(SearchEngine)) SearchEngine = "DuckDuckGo";
        SleepSeconds = Math.Clamp(SleepSeconds, 30, 3600); ZoomPercent = Math.Clamp(ZoomPercent, 50, 200);
        if (string.IsNullOrWhiteSpace(DownloadFolder) || !Path.IsPathFullyQualified(DownloadFolder)) DownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }
    public BrowserSettings Copy() => JsonSerializer.Deserialize<BrowserSettings>(JsonSerializer.Serialize(this))!;
}
public sealed class HistoryEntry
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Visited { get; set; } = DateTime.Now;
}
public sealed record Palette(Color Background, Color Surface, Color Raised, Color Text, Color Muted, Color Border, Color Accent)
{
    public static Palette For(string theme) => theme == "Açık"
        ? new(Color.FromArgb(243,246,244), Color.White, Color.FromArgb(231,238,233), Color.FromArgb(22,43,34), Color.FromArgb(88,109,99), Color.FromArgb(214,225,218), Color.FromArgb(65,184,130))
        : new(Color.FromArgb(15,22,20), Color.FromArgb(23,33,29), Color.FromArgb(34,47,40), Color.FromArgb(232,242,235), Color.FromArgb(151,175,160), Color.FromArgb(47,64,54), Color.FromArgb(159,235,185));
}
public sealed class MinitButton : Button
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 10;
    bool hover;
    public MinitButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true); }
    public static GraphicsPath Shape(Rectangle r, int radius)
    {
        var p = new GraphicsPath(); int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d < 2) { p.AddRectangle(r); return p; }
        p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Shape(new Rectangle(0,0,Width-1,Height-1), Radius);
        Color color = hover ? ControlPaint.Light(BackColor, .13f) : BackColor;
        using var fill = new SolidBrush(color); e.Graphics.FillPath(fill,path);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
        flags |= TextAlign == ContentAlignment.MiddleLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter;
        TextRenderer.DrawText(e.Graphics, Text, Font, Rectangle.Inflate(ClientRectangle, Width < 70 ? -2 : -8, 0), Enabled ? ForeColor : SystemColors.GrayText, flags);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle,-4,-4), ForeColor,color);
    }
}
public sealed class CardPanel : Panel
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; }
    public CardPanel() { DoubleBuffered = true; Padding = new Padding(16); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var pen=new Pen(BorderColor); using var p=MinitButton.Shape(new Rectangle(0,0,Width-1,Height-1),14); e.Graphics.DrawPath(pen,p); }
}
public static class ChromeStyle
{
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    public static void TitleBar(Form form, bool dark) { try { int v=dark?1:0; DwmSetWindowAttribute(form.Handle,20,ref v,4); } catch { } }
    public static Label Label(string text, float size, Color color, bool bold=false) => new() { Text=text, AutoSize=true, Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular), ForeColor=color, BackColor=Color.Transparent, Margin=new Padding(0,0,0,8) };
}
