namespace MinitBrowser;
public sealed class SettingsWindow : ModernWindow
{
    public BrowserSettings Value { get; }
    readonly Palette palette;
    readonly ComboBox theme = new(), search = new();
    readonly CheckBox restore = new(), sleep = new(), history = new(), memory = new(), edges = new();
    readonly NumericUpDown delay = new(), zoom = new();
    readonly TextBox folder = new();
    readonly FlowLayoutPanel content = new() { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false, AutoScroll=true, Padding=new Padding(26,20,20,20) };
    readonly List<Control> pages=[];
    public SettingsWindow(BrowserSettings value, Action historyAction)
    {
        Value=value.Copy(); palette=Palette.For(value.Theme);
        Text="Minit • Ayarlar"; Size=new Size(850,690); MinimumSize=new Size(780,600); StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10); BackColor=palette.Background; ForeColor=palette.Text;
        var sidebar=new FlowLayoutPanel { Dock=DockStyle.Left, Width=180, FlowDirection=FlowDirection.TopDown, WrapContents=false, BackColor=palette.Surface, Padding=new Padding(18,24,10,0) };
        sidebar.Controls.Add(ChromeStyle.Label("minit",24,palette.Accent,true)); sidebar.Controls.Add(ChromeStyle.Label("AYARLAR",9,palette.Muted));
        var bottom=new Panel { Dock=DockStyle.Bottom, Height=72, Padding=new Padding(20), BackColor=palette.Surface };
        var save=Button("Kaydet",palette.Accent,Color.FromArgb(18,43,29)); save.Dock=DockStyle.Right; save.Width=110;
        var cancel=Button("Vazgeç",palette.Raised,palette.Text); cancel.Dock=DockStyle.Right; cancel.Width=100;
        cancel.Click+=(_,_)=>DialogResult=DialogResult.Cancel; save.Click+=(_,_)=>Save();
        bottom.Controls.Add(cancel); bottom.Controls.Add(save); Controls.Add(content); Controls.Add(bottom); Controls.Add(sidebar);
        AcceptButton=save; CancelButton=cancel;
        ConfigureCombo(theme,["Koyu","Açık"],Value.Theme); ConfigureCombo(search,["DuckDuckGo","Google","Bing"],Value.SearchEngine);
        edges.Text="Yarı saydam pencere kenarları";edges.Checked=Value.TransparentEdges;
        restore.Text="Son oturumdaki sekmeleri yeniden aç"; restore.Checked=Value.RestoreSession;
        sleep.Text="Kullanılmayan sekmeleri otomatik boşalt"; sleep.Checked=Value.SleepTabs;
        history.Text="Ziyaret ettiğim sayfaları bu bilgisayarda sakla"; history.Checked=Value.SaveHistory;
        memory.Text="Alt çubukta yaklaşık RAM tüketimini göster"; memory.Checked=Value.ShowMemory;
        delay.Minimum=30; delay.Maximum=3600; delay.Increment=30; delay.Value=Value.SleepSeconds;
        zoom.Minimum=50; zoom.Maximum=200; zoom.Increment=10; zoom.Value=Value.ZoomPercent;
        folder.Text=Value.DownloadFolder; folder.Width=450;
        var browse=Button("Klasör seç",palette.Raised,palette.Text); browse.Click+=(_,_)=> { using var dialog=new FolderBrowserDialog { InitialDirectory=folder.Text, Description="İndirilenlerin kaydedileceği klasör" }; if(dialog.ShowDialog(this)==DialogResult.OK) folder.Text=dialog.SelectedPath; };
        pages.Add(Page("Genel", "Tarayıcıyı kendine göre ayarla.", Field("Arama motoru",search), Field("Başlangıç",restore), Field("İndirme klasörü",folder,browse)));
        pages.Add(Page("Görünüm", "Sade, okunaklı ve dikkat dağıtmayan.", Field("Uygulama teması",theme),Field("Varsayılan sayfa yakınlaştırması (%)",zoom), Field("Pencere kenarları",edges),Field("Durum çubuğu",memory)));
        pages.Add(Page("Performans", "RAM kullanımını sekmeler üzerinden yönet.",Field("Sekme uykusu",sleep), Field("Arka planda bekleme süresi (saniye)",delay), Note("Uyuyan sekmeler açıldığında yeniden yüklenir. Kaydedilmemiş form verileri ve sayfa konumu korunmaz. Sekmeye sağ tık → Bellekte tut. Ses çalan ve indirme yapan sekmeler otomatik boşaltılmaz.")));
        var openHistory=Button("Geçmişi yönet",palette.Raised,palette.Text); openHistory.Width=180; openHistory.Click+=(_,_)=>historyAction();
        pages.Add(Page("Gizlilik", "Verilerin bu bilgisayarda tutulur.",Field("Gezinme geçmişi",history),Field("Kaydedilen ziyaretler",openHistory),Note("Geçmiş kaydını kapatmak mevcut kayıtları silmez. Geçmişi yönet ekranından silebilirsin. Bu sürümde gizli mod ve çerez temizleme aracı yoktur. Sitelerin izin istekleri ayrıca sorulur.")));
        for(int i=0;i<pages.Count;i++) { int index=i; var b=Button(new[]{"Genel","Görünüm","Performans","Gizlilik"}[i],palette.Surface,palette.Text); b.Width=145; b.TextAlign=ContentAlignment.MiddleLeft; b.Margin=new Padding(0,8,0,0); b.Click+=(_,_)=>SelectPage(index); sidebar.Controls.Add(b); }
        BuildChrome("AYARLAR",false);ApplyFrameTheme(palette,Value.TransparentEdges);
        Shown+=(_,_)=> { ChromeStyle.TitleBar(this,Value.Theme=="Koyu"); SelectPage(0); };
        FormClosed+=(_,_)=> { foreach(var page in pages) page.Dispose(); };
    }
    MinitButton Button(string text,Color back,Color fore)=>new() { Text=text,BackColor=back,ForeColor=fore,Size=new Size(130,38),Margin=new Padding(0,5,0,0) };
    void ConfigureCombo(ComboBox c,string[] values,string selected) { c.FlatStyle=FlatStyle.Flat;c.DropDownStyle=ComboBoxStyle.DropDownList; c.Items.AddRange(values);c.SelectedItem=selected;c.Width=450; }
    Control Note(string text) => new Label { Text=text,ForeColor=palette.Muted,BackColor=palette.Surface,MaximumSize=new Size(500,0),AutoSize=true,Padding=new Padding(18),Margin=new Padding(0,14,0,0) };
    Control Field(string label, params Control[] controls)
    {
        var box=new FlowLayoutPanel { FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,Width=510,Margin=new Padding(0,12,0,8) };
        box.Controls.Add(ChromeStyle.Label(label,10,palette.Muted));
        foreach(var c in controls) { c.ForeColor=palette.Text; if(c is not System.Windows.Forms.Button) c.BackColor=palette.Raised; if(c is CheckBox cb) {cb.BackColor=palette.Background;cb.AutoSize=true;cb.MaximumSize=new Size(490,0);} if(c is NumericUpDown n)n.Width=170; box.Controls.Add(c); } return box;
    }
    Control Page(string title,string subtitle,params Control[] controls)
    {
        var page=new FlowLayoutPanel { FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,Width=520 };
        page.Controls.Add(ChromeStyle.Label(title,25,palette.Text,true));page.Controls.Add(ChromeStyle.Label(subtitle,10,palette.Muted)); foreach(var c in controls)page.Controls.Add(c);return page;
    }
    internal void SelectPage(int i) { content.Controls.Clear(); content.Controls.Add(pages[i]); content.AutoScrollPosition=Point.Empty; }
    internal BrowserSettings SaveForTest(BrowserSettings sample)
    {
        theme.SelectedItem=sample.Theme;search.SelectedItem=sample.SearchEngine;restore.Checked=sample.RestoreSession;sleep.Checked=sample.SleepTabs;history.Checked=sample.SaveHistory;memory.Checked=sample.ShowMemory;edges.Checked=sample.TransparentEdges;delay.Value=sample.SleepSeconds;zoom.Value=sample.ZoomPercent;folder.Text=sample.DownloadFolder;Save();return Value;
    }
    void Save()
    {
        if(!Path.IsPathFullyQualified(folder.Text.Trim())) { MessageBox.Show(this,"Geçerli, tam bir klasör yolu seç.","İndirme klasörü");return; }
        try { Directory.CreateDirectory(folder.Text.Trim()); } catch(Exception ex) {MessageBox.Show(this,"Klasör kullanılamıyor: "+ex.Message);return;}
        Value.TransparentEdges=edges.Checked;Value.Theme=theme.Text;Value.SearchEngine=search.Text;Value.RestoreSession=restore.Checked;Value.SleepTabs=sleep.Checked;Value.SleepSeconds=(int)delay.Value;Value.ZoomPercent=(int)zoom.Value;Value.ShowMemory=memory.Checked;Value.SaveHistory=history.Checked;Value.DownloadFolder=folder.Text.Trim();Value.Normalize();DialogResult=DialogResult.OK;
    }
}
