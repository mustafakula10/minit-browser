using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace MinitBrowser;

public class ModernWindow : Form
{
    Panel? titlebar;
    Label? brand, subtitle;
    MinitButton? minimize, maximize, close;
    GlassRim? rim;
    Palette colors=Palette.For("Koyu");
    bool transparentEdges=true;
    public ModernWindow()
    {
        FormBorderStyle=FormBorderStyle.None;Padding=new Padding(7);DoubleBuffered=true;
        SetStyle(ControlStyles.ResizeRedraw,true);
        var stream=typeof(ModernWindow).Assembly.GetManifestResourceStream("MinitBrowser.Assets.minit.ico");
        if(stream!=null){using(stream)Icon=new Icon(stream);}
    }
    internal void BuildChrome(string detail,bool allowMaximize=true)
    {
        titlebar=new Panel {Dock=DockStyle.Top,Height=44,Padding=new Padding(12,0,0,0)};
        var logo=new LogoMark {Dock=DockStyle.Left,Width=30,Margin=Padding.Empty};
        brand=new Label {Text="minit",Dock=DockStyle.Left,Width=86,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",14,FontStyle.Bold)};
        subtitle=new Label {Text=detail,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",9)};
        var controls=new Panel {Dock=DockStyle.Right,Width=allowMaximize?132:88};
        close=CaptionButton("×","Pencereyi kapat",()=>Close());close.Dock=DockStyle.Right;
        maximize=CaptionButton("□","Büyüt veya geri yükle",ToggleMaximize);maximize.Dock=DockStyle.Right;maximize.Visible=allowMaximize;
        minimize=CaptionButton("—","Simge durumuna küçült",()=>WindowState=FormWindowState.Minimized);minimize.Dock=DockStyle.Right;
        controls.Controls.Add(minimize);if(allowMaximize)controls.Controls.Add(maximize);controls.Controls.Add(close);
        titlebar.Controls.Add(subtitle);titlebar.Controls.Add(brand);titlebar.Controls.Add(logo);titlebar.Controls.Add(controls);
        void Drag(object? sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}
        foreach(Control c in new Control[]{titlebar,logo,brand,subtitle}) {c.MouseDown+=Drag;if(allowMaximize)c.DoubleClick+=(_,_)=>ToggleMaximize();}
        Controls.Add(titlebar);ApplyFrameTheme(colors,transparentEdges);
    }
    MinitButton CaptionButton(string text,string accessible,Action action)
    {
        var b=new MinitButton {Text=text,Width=42,Font=new Font("Segoe UI",13),AccessibleName=accessible,Radius=8};b.Click+=(_,_)=>action();return b;
    }
    internal void ToggleMaximize()
    {
        if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;
        MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;
        WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;
    }
    internal void ApplyFrameTheme(Palette p,bool edges)
    {
        colors=p;transparentEdges=edges;
        if(titlebar!=null){titlebar.BackColor=p.Background;brand!.ForeColor=p.Text;subtitle!.ForeColor=p.Muted;foreach(var b in new[]{minimize,maximize,close})if(b!=null){b.BackColor=p.Background;b.ForeColor=p.Muted;}}
        rim?.SetColor(p.Background,edges);Invalidate();UpdateRim();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=MinitButton.Shape(new Rectangle(0,0,ClientSize.Width-1,ClientSize.Height-1),16);using var pen=new Pen(colors.Border);e.Graphics.DrawPath(pen,path);
    }
    protected override void OnShown(EventArgs e){base.OnShown(e);UpdateRim();}
    protected override void OnActivated(EventArgs e){base.OnActivated(e);UpdateRim();}
    protected override void OnMove(EventArgs e){base.OnMove(e);UpdateRim();}
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if(ClientSize.Width>0 && ClientSize.Height>0){var old=Region;using var path=MinitButton.Shape(ClientRectangle,WindowState==FormWindowState.Maximized?0:16);Region=new Region(path);old?.Dispose();}
        if(maximize!=null)maximize.Text=WindowState==FormWindowState.Maximized?"▣":"□";
        UpdateRim();
    }
    void UpdateRim()
    {
        if(!IsHandleCreated || IsDisposed)return;
        if(!Visible||WindowState!=FormWindowState.Normal||!transparentEdges){rim?.Hide();return;}
        if(rim==null){rim=new GlassRim();rim.SetColor(colors.Background,true);}
        rim.Bounds=new Rectangle(Left-8,Top-8,Width+16,Height+16);
        if(!rim.Visible)rim.Show();
        SetWindowPos(rim.Handle,Handle,Left-8,Top-8,Width+16,Height+16,0x0010);
    }
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);UpdateRim();}
    protected override void OnFormClosed(FormClosedEventArgs e){rim?.Dispose();rim=null;base.OnFormClosed(e);}
    protected override void Dispose(bool disposing){if(disposing){rim?.Dispose();rim=null;}base.Dispose(disposing);}
    internal bool HasTranslucentRim => rim?.Visible == true && rim.Opacity < 1;
    internal int ResizeHit(Point p)
    {
        if(WindowState!=FormWindowState.Normal)return 1;
        const int grip=7;bool left=p.X<grip,right=p.X>=ClientSize.Width-grip,top=p.Y<grip,bottom=p.Y>=ClientSize.Height-grip;
        return top&&left?13:top&&right?14:bottom&&left?16:bottom&&right?17:left?10:right?11:top?12:bottom?15:1;
    }
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if(m.Msg==0x84 && m.Result==(IntPtr)1){long raw=m.LParam.ToInt64();var p=PointToClient(new Point((short)(raw&0xffff),(short)((raw>>16)&0xffff)));m.Result=(IntPtr)ResizeHit(p);}
    }
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
    [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
    sealed class GlassRim : Form
    {
        public GlassRim(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Opacity=.48;Enabled=false;}
        protected override bool ShowWithoutActivation=>true;
        protected override CreateParams CreateParams{get{var c=base.CreateParams;c.ExStyle|=0x08000000|0x20|0x80;return c;}}
        public void SetColor(Color color,bool enabled){BackColor=color;Opacity=enabled ? .48 : 1;}
        protected override void OnResize(EventArgs e){base.OnResize(e);if(Width>0&&Height>0){using var path=MinitButton.Shape(ClientRectangle,24);var old=Region;Region=new Region(path);old?.Dispose();}}
    }
}
public sealed class LogoMark : Control
{
    public LogoMark(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;AccessibleName="Minit logosu";}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        float size=Math.Min(Width,Height)-4, x=(Width-size)/2,y=(Height-size)/2;
        using var tile=MinitButton.Shape(new Rectangle((int)x,(int)y,(int)size,(int)size),Math.Max(4,(int)size/5));using var fill=new SolidBrush(Color.FromArgb(24,49,36));e.Graphics.FillPath(fill,tile);
        using var pen=new Pen(Color.FromArgb(163,240,191),size*.09f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        e.Graphics.DrawLines(pen,new[]{new PointF(x+size*.24f,y+size*.69f),new PointF(x+size*.24f,y+size*.32f),new PointF(x+size*.5f,y+size*.56f),new PointF(x+size*.76f,y+size*.32f),new PointF(x+size*.76f,y+size*.69f)});
    }
}
