using ExampleWebRemote.Components;
using Photino.Blazor;
using Photino.NET;

public static class Apis {
    public static PhotinoWindow Window;
}

public class Program {
    [STAThread] 
    static void Main(string[] args) {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);
        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();
        app.MainWindow
            .SetTitle("Pot Remote")
            .SetSize(1280, 720)
            .Center();

        Apis.Window = app.MainWindow;



        app.Run();
    }
}