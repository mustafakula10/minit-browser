using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
namespace MinitBrowser;
public sealed partial class BrowserWindow
{
    void MigratePreferences()
    {
        var legacy=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MintiBrowser");
        foreach(var name in new[]{"settings.json","session.json","bookmarks.json","history.json"})
        {
            var old=Path.Combine(legacy,name);var target=Path.Combine(dataPath,name);
            try{if(File.Exists(old)&&!File.Exists(target)){var value=File.ReadAllText(old).Replace("minti://home","minit://home");File.WriteAllText(target,value);}}catch{}
        }
    }
    BrowserSettings LoadSettings()
    {
        try { var value=JsonSerializer.Deserialize<BrowserSettings>(File.ReadAllText(Path.Combine(dataPath,"settings.json"))) ?? new(); value.Normalize();return value; } catch {return new();}
    }
    void ApplyTheme()
    {
        palette=Palette.For(settings.Theme);BackColor=palette.Background;ForeColor=palette.Text;
        strip.BackColor=palette.Surface;plus.BackColor=palette.Surface;plus.ForeColor=palette.Text;
        status.BackColor=palette.Surface;status.ForeColor=palette.Muted;
        address.BackColor=palette.Surface;address.ForeColor=palette.Text;address.PlaceholderText="Bir adres yaz veya "+settings.SearchEngine+" ile ara";
        foreach(var nav in Controls.OfType<TableLayoutPanel>())
        {
            nav.BackColor=palette.Background;
            foreach(Control c in nav.Controls) {c.BackColor=palette.Surface;c.ForeColor=palette.Text;if(c is CardPanel card)card.BorderColor=palette.Border;}
        }
        foreach(var t in tabs) {t.Holder.BackColor=palette.Surface;foreach(var close in t.Holder.Controls.OfType<Button>().Where(b=>b!=t.Button)){close.BackColor=palette.Raised;close.ForeColor=palette.Muted;}if(t.View?.CoreWebView2!=null)ApplyViewSettings(t.View);}
        foreach(var b in status.Controls.OfType<Button>()) {b.BackColor=palette.Surface;b.ForeColor=palette.Text;}
        ApplyFrameTheme(palette,settings.TransparentEdges);UpdateTabs();
        if(current?.Saved.Url==Home && current.View==null) {ClearHost();ShowHome();}
    }
    void ApplyViewSettings(WebView2 view)
    {
        view.ZoomFactor=settings.ZoomPercent/100d;
        view.CoreWebView2.Profile.PreferredColorScheme=settings.Theme=="Koyu"?CoreWebView2PreferredColorScheme.Dark:CoreWebView2PreferredColorScheme.Light;
        try { if(Directory.Exists(settings.DownloadFolder))view.CoreWebView2.Profile.DefaultDownloadFolderPath=settings.DownloadFolder; } catch {navigationMessage="İndirme klasörü kullanılamıyor; motorun varsayılan klasörü kullanılacak.";}
    }
    void OpenSettings()
    {
        using var dialog=new SettingsWindow(settings,()=>ManageRecords(true));
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        if(!WriteJson("settings.json",dialog.Value)) {MessageBox.Show(this,navigationMessage,"Ayarlar kaydedilemedi");return;}
        settings=dialog.Value;ApplyTheme();TickMemory();
    }
    void UpdateNavigation()
    {
        var nav=Controls.OfType<TableLayoutPanel>().FirstOrDefault();if(nav==null)return;
        var back=nav.GetControlFromPosition(0,0);var forward=nav.GetControlFromPosition(1,0);
        if(back!=null)back.Enabled=current?.View?.CanGoBack==true;
        if(forward!=null)forward.Enabled=current?.View?.CanGoForward==true;
    }
    void ShowHome()
    {
        var root=new Panel {Dock=DockStyle.Fill,BackColor=palette.Background,AutoScroll=true};
        var column=new FlowLayoutPanel {FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,Width=800,Padding=new Padding(0,4,0,20),BackColor=palette.Background};
        column.Controls.Add(ChromeStyle.Label("MINIT  /  SENİN İNTERNETİN",10,palette.Accent,true));
        column.Controls.Add(ChromeStyle.Label("Daha az yük. Daha çok alan.",30,palette.Text,true));
        column.Controls.Add(ChromeStyle.Label("Hızlı bir başlangıç. Gerektiğinde uyanan sekmeler.",12,palette.Muted));
        column.Controls.Add(ChromeStyle.Label("ARA VEYA ADRES GİR",9,palette.Muted,true));
        var searchCard=new CardPanel {Size=new Size(800,66),BackColor=palette.Surface,BorderColor=palette.Border,Margin=new Padding(0,8,0,18),Padding=new Padding(20,16,10,12)};
        var input=new TextBox {BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",13),ForeColor=palette.Text,BackColor=palette.Surface,PlaceholderText=settings.SearchEngine+" ile ara veya adres yaz",Dock=DockStyle.Fill,AccessibleName="Başlangıç sayfasında ara"};
        var go=new MinitButton {Text="Ara  →",BackColor=palette.Accent,ForeColor=Color.FromArgb(18,43,29),Dock=DockStyle.Right,Width=95};
        void Search(){if(string.IsNullOrWhiteSpace(input.Text))return;address.Text=input.Text;NavigateAddress();}
        go.Click+=(_,_)=>Search();input.KeyDown+=(_,e)=> {if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Search();}};
        searchCard.Controls.Add(input);searchCard.Controls.Add(go);column.Controls.Add(searchCard);
        var shortcuts=new FlowLayoutPanel {Size=new Size(800,62),WrapContents=false,Margin=new Padding(0,0,0,24)};
        foreach(var item in new[]{("GitHub","https://github.com"),("Wikipedia","https://tr.wikipedia.org"),("YouTube","https://www.youtube.com")})
        {
            var b=new MinitButton {Text=item.Item1+"  ↗",Size=new Size(256,48),BackColor=palette.Raised,ForeColor=palette.Text,Margin=new Padding(0,0,14,0)};b.Click+=(_,_)=>AddTab(item.Item2);shortcuts.Controls.Add(b);
        }
        column.Controls.Add(shortcuts);
        column.Controls.Add(ChromeStyle.Label("DAHA SADE BİR GEZİNTİ",9,palette.Muted,true));
        var cards=new FlowLayoutPanel {Size=new Size(800,132),WrapContents=false,Margin=new Padding(0,8,0,18)};
        foreach(var item in new[]{("01  /  HAFİF",settings.SleepTabs?settings.SleepSeconds+" sn uyku":"Uyku kapalı","Pasif sekmeler hafifler."),("02  /  SANA GÖRE",settings.SearchEngine,"Arama motorunu sen seç."),("03  /  YEREL",settings.SaveHistory?"Geçmiş yerelde":"Geçmiş kapalı","Minit hesabı veya eşitleme yok.")})
        {
            var card=new CardPanel {Size=new Size(256,120),BackColor=palette.Surface,BorderColor=palette.Border,Margin=new Padding(0,0,14,0)};
            var text=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};text.Controls.Add(ChromeStyle.Label(item.Item1,9,palette.Accent,true));text.Controls.Add(ChromeStyle.Label(item.Item2,14,palette.Text,true));text.Controls.Add(ChromeStyle.Label(item.Item3,9,palette.Muted));card.Controls.Add(text);cards.Controls.Add(card);
        }
        column.Controls.Add(cards);
        var footer=new FlowLayoutPanel {Width=800,Height=48,WrapContents=false};
        foreach(var item in new[]{("Ayarları düzenle",(Action)OpenSettings),("Yer imlerim",(Action)(()=>ManageRecords(false))),("Geçmiş",(Action)(()=>ManageRecords(true)))})
        {var b=new MinitButton {Text=item.Item1,Size=new Size(165,36),BackColor=palette.Background,ForeColor=palette.Muted,Margin=new Padding(0,0,8,0)};b.Click+=(_,_)=>item.Item2();footer.Controls.Add(b);}
        column.Controls.Add(footer);
        column.Controls.Add(ChromeStyle.Label("Ctrl+L  adres     Ctrl+T  yeni sekme     Ctrl+W  kapat",9,palette.Muted));
        root.Controls.Add(column);
        void LayoutHome()
        {
            int width=Math.Max(680,Math.Min(800,root.ClientSize.Width-72));column.Width=width;column.Location=new Point(Math.Max(24,(root.ClientSize.Width-width)/2),Math.Max(28,(root.ClientSize.Height-550)/3));
            searchCard.Width=width;shortcuts.Width=width;cards.Width=width;footer.Width=width;
            foreach(Control c in shortcuts.Controls)c.Width=(width-28)/3;
            foreach(Control c in cards.Controls)c.Width=(width-28)/3;
        }
        root.Resize+=(_,_)=>LayoutHome();host.Controls.Add(root);LayoutHome();
    }
    List<HistoryEntry> History()
    {
        try{return JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(Path.Combine(dataPath,"history.json")))??[];}catch{return [];}
    }
    void RecordHistory(string url,string title)
    {
        var items=History();items.RemoveAll(h=>h.Url==url);items.Insert(0,new HistoryEntry {Url=url,Title=title,Visited=DateTime.Now});WriteJson("history.json",items.Take(500).ToList());
    }
    void ManageRecords(bool history)
    {
        using var window=new ModernWindow {Text=history?"Minit • Geçmiş":"Minit • Yer imleri",Size=new Size(900,600),MinimumSize=new Size(700,450),StartPosition=FormStartPosition.CenterParent,BackColor=palette.Background,ForeColor=palette.Text,Font=Font};
        var top=new Panel {Dock=DockStyle.Top,Height=80,Padding=new Padding(20)};
        var filter=new TextBox {Dock=DockStyle.Fill,PlaceholderText="Başlık veya adreste ara",BackColor=palette.Surface,ForeColor=palette.Text,Font=new Font("Segoe UI",12)};top.Controls.Add(filter);
        var list=new ListView {Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,BackColor=palette.Surface,ForeColor=palette.Text,BorderStyle=BorderStyle.None,HideSelection=false};
        list.Columns.Add("Sayfa",260);list.Columns.Add("Adres",420);if(history)list.Columns.Add("Tarih",160);
        var bottom=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=64,Padding=new Padding(16,10,10,10)};
        List<HistoryEntry> histories=history?History():[];List<SavedTab> bookmarks=history?[]:Bookmarks();
        void RefreshList(){list.Items.Clear();foreach(var h in histories.Where(h=>(h.Title+" "+h.Url).Contains(filter.Text,StringComparison.OrdinalIgnoreCase))){var row=new ListViewItem(h.Title){Tag=h};row.SubItems.Add(h.Url);row.SubItems.Add(h.Visited.ToString("g"));list.Items.Add(row);}foreach(var b in bookmarks.Where(b=>(b.Title+" "+b.Url).Contains(filter.Text,StringComparison.OrdinalIgnoreCase))){var row=new ListViewItem(b.Title){Tag=b};row.SubItems.Add(b.Url);list.Items.Add(row);}}
        void Open(){if(list.SelectedItems.Count==0)return;var tag=list.SelectedItems[0].Tag;var url=tag is HistoryEntry h?h.Url:((SavedTab)tag!).Url;if(IsWebUrl(url))AddTab(url);window.Close();}
        var open=new MinitButton {Text="Sekmede aç",Size=new Size(140,36),BackColor=palette.Accent,ForeColor=Color.FromArgb(18,43,29)};open.Click+=(_,_)=>Open();
        var remove=new MinitButton {Text="Seçileni sil",Size=new Size(140,36),BackColor=palette.Raised,ForeColor=palette.Text};remove.Click+=(_,_)=>{if(list.SelectedItems.Count==0)return;var tag=list.SelectedItems[0].Tag;if(tag is HistoryEntry h)histories.Remove(h);else if(tag is SavedTab b)bookmarks.Remove(b);if(history)WriteJson("history.json",histories);else WriteJson("bookmarks.json",bookmarks);RefreshList();};
        bottom.Controls.Add(open);bottom.Controls.Add(remove);
        if(history){var clear=new MinitButton {Text="Tüm geçmişi sil",Size=new Size(165,36),BackColor=palette.Raised,ForeColor=palette.Text};clear.Click+=(_,_)=>{if(MessageBox.Show(window,"Bu bilgisayardaki gezinme geçmişi silinsin mi? Çerezler ve site oturumları silinmez.","Geçmişi sil",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){histories.Clear();WriteJson("history.json",histories);RefreshList();}};bottom.Controls.Add(clear);}
        list.DoubleClick+=(_,_)=>Open();filter.TextChanged+=(_,_)=>RefreshList();window.Controls.Add(list);window.Controls.Add(bottom);window.Controls.Add(top);RefreshList();window.BuildChrome(history?"GEÇMİŞ":"YER İMLERİ");window.ApplyFrameTheme(palette,settings.TransparentEdges);window.Shown+=(_,_)=>ChromeStyle.TitleBar(window,settings.Theme=="Koyu");window.ShowDialog(this);
    }
    async Task TestSettingsAndViews(List<string> lines)
    {
        if(FormBorderStyle!=FormBorderStyle.None||Icon==null||ResizeHit(new Point(2,2))!=13||ResizeHit(new Point(ClientSize.Width-2,ClientSize.Height-2))!=17)throw new Exception("Custom frame or resize hit-test failed");
        ApplyFrameTheme(palette,false);if(HasTranslucentRim)throw new Exception("Edge toggle off failed");
        ApplyFrameTheme(palette,true);if(!HasTranslucentRim)throw new Exception("Translucent edge missing");
        ToggleMaximize();if(WindowState!=FormWindowState.Maximized||HasTranslucentRim)throw new Exception("Maximize failed");
        ToggleMaximize();if(WindowState!=FormWindowState.Normal)throw new Exception("Restore failed");
        lines.Add("PASS borderless frame, logo icon, resize corners, translucent rim toggle and maximize/restore");
        var original=settings.Copy();var sample=settings.Copy();sample.TransparentEdges=false;sample.SearchEngine="Bing";sample.Theme="Açık";sample.SleepSeconds=90;sample.SleepTabs=false;sample.SaveHistory=false;sample.ZoomPercent=125;sample.ShowMemory=false;sample.RestoreSession=false;
        sample.DownloadFolder=Path.Combine(dataPath,"DownloadsTest");
        using(var editor=new SettingsWindow(settings,()=>{})) { var saved=editor.SaveForTest(sample); if(editor.DialogResult!=DialogResult.OK || JsonSerializer.Serialize(saved)!=JsonSerializer.Serialize(sample))throw new Exception("Settings UI save failed"); }
        if(!WriteJson("settings.json",sample))throw new Exception("Settings write failed");settings=LoadSettings();
        if(settings.TransparentEdges||settings.SearchEngine!="Bing"||settings.Theme!="Açık"||settings.SleepSeconds!=90||settings.SleepTabs||settings.SaveHistory||settings.ZoomPercent!=125||settings.ShowMemory||settings.RestoreSession)throw new Exception("Settings round trip failed");
        if(!Resolve("test query",settings.SearchEngine).StartsWith("https://www.bing.com/")||!Resolve("test query","DuckDuckGo").StartsWith("https://duckduckgo.com/"))throw new Exception("Search selection failed");
        ApplyTheme();if(current?.View?.ZoomFactor!=1.25)throw new Exception("Zoom not applied");
        var disabled=tabs.First();await Activate(tabs.Last());disabled.HiddenSince=DateTime.UtcNow.AddHours(-1);TickMemory();if(disabled.View==null)throw new Exception("Sleep disabled ignored");
        PerformLayout();Refresh();using(var lightShot=new Bitmap(Width,Height)){DrawToBitmap(lightShot,new Rectangle(0,0,Width,Height));lightShot.Save(Path.Combine(AppContext.BaseDirectory,"home-light.png"));}
        lines.Add("PASS settings UI save, persistence, engine selection, live zoom and disabled sleep");
        settings=original;WriteJson("settings.json",settings);ApplyTheme();var home=AddTab(Home,activate:false);await Activate(home);
        TickMemory();
        foreach(var size in new[]{new Size(900,700),new Size(1200,850),new Size(1500,950)}){Size=size;PerformLayout();Refresh();using var shot=new Bitmap(Width,Height);DrawToBitmap(shot,new Rectangle(0,0,Width,Height));shot.Save(Path.Combine(AppContext.BaseDirectory,$"home-{Width}.png"));}
        using(var dialog=new SettingsWindow(settings,()=>{})){dialog.Show(this);for(int i=0;i<4;i++){dialog.SelectPage(i);dialog.PerformLayout();dialog.Refresh();using var shot=new Bitmap(dialog.Width,dialog.Height);dialog.DrawToBitmap(shot,new Rectangle(0,0,dialog.Width,dialog.Height));shot.Save(Path.Combine(AppContext.BaseDirectory,$"settings-{i}.png"));}dialog.Close();}
        File.WriteAllText(Path.Combine(dataPath,"settings.json"),"broken json");settings=LoadSettings();if(settings.Theme!="Koyu")throw new Exception("Corrupt settings fallback failed");WriteJson("settings.json",original);settings=original;
        var invalid=new BrowserSettings {SleepSeconds=-10,ZoomPercent=999,Theme="invalid",SearchEngine="invalid"};invalid.Normalize();if(invalid.SleepSeconds!=30||invalid.ZoomPercent!=200||invalid.Theme!="Koyu"||invalid.SearchEngine!="DuckDuckGo")throw new Exception("Settings validation failed");
        RecordHistory("https://example.com","A");RecordHistory("https://example.com","B");if(History().Count(h=>h.Url=="https://example.com")!=1)throw new Exception("History deduplication failed");
        foreach(var t in tabs.ToArray())CloseTab(t);Size=new Size(1200,850);TickMemory();PerformLayout();Refresh();
        using(var finalShot=new Bitmap(Width,Height)){DrawToBitmap(finalShot,new Rectangle(0,0,Width,Height));finalShot.Save(Path.Combine(AppContext.BaseDirectory,"Minit-Onizleme.png"));}
        lines.Add("PASS responsive home, four settings pages, corrupt-config recovery, validation and history deduplication");
    }
}
