namespace MinitBrowser;
static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"error.txt"),e.Exception.ToString()); Application.Exit(); };
        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserWindow(args.Contains("--self-test"), args.Contains("--visual-test")));
    }
}
