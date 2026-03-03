using TestServer.Endpoints;

namespace TestServer;

public static class Program
{
    public static int Main(string[] args)
    {
        BuildApp(args).Run();
        return 0;
    }

    private static WebApplication BuildApp(string[] args)
    {
        var webAppBuilder = WebApplication.CreateBuilder(args);

        webAppBuilder.Logging.ClearProviders();
        webAppBuilder.Logging.AddConsole();

        var webApp = webAppBuilder.Build();
        webApp.ConfigureApp();

        return webApp;
    }

    private static IApplicationBuilder ConfigureApp(this WebApplication app) =>
        app.MapTestEndpoints()
           .UseDefaultFiles()
           .UseStaticFiles()
           .UseWebSockets(new()
           {
               KeepAliveInterval = TimeSpan.FromMinutes(2)
           });
}