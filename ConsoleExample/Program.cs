// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.WebSockets;

using var cws = MakeClientWebSocket();
using var hc = MakeHttpClient(disableSslVerification: true);
var wsc = new WebSocketClientSideConnector();
var uri = new Uri("wss://localhost:5001/test/http1websocket");

Console.WriteLine("--- CONVERSATION BEGIN ---");

await wsc.ConnectAsync(cws, hc, uri);

// check here if connection is OK before sending messages
await wsc.SendMessageAsync(WebSocketMessageType.Text, "Hello!", false);

int msgCount = 0;
await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    msgCount++;
    string msgText = msg.ReadAsUtf8Text()!;
    Console.WriteLine($"Message {msgCount}, {msg.Direction}: {msgText}");
    
    if (msg.Direction == WebSocketMessageDirection.FromClient)
    {
        continue;
    }

    if (msgText == "Hi!")
    {
        await wsc.SendMessageAsync(WebSocketMessageType.Text, "What time is it?", false);
    }
    else if (msgText.StartsWith("Now it's"))
    {
        await wsc.SendMessageAsync(WebSocketMessageType.Text, "Thanks!", false);
        //await wsc.DisconnectAsync();
    }
}

Console.WriteLine("--- CONVERSATION ENDED ---");

#region SETUP

static ClientWebSocket MakeClientWebSocket()
{
    ClientWebSocket cws = new();
    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    cws.Options.HttpVersion = new(1,1);
    cws.Options.CollectHttpResponseDetails = true;
    return cws;
}

static HttpClient MakeHttpClient(bool disableSslVerification)
{
    /*
    https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#alternatives-to-ihttpclientfactory-1
    https://stackoverflow.com/questions/48778580/singleton-httpclient-vs-creating-new-httpclient-request/68495953#68495953
    */
    SocketsHttpHandler httpHandler = new()
    {
        // Sets how long a connection can be in the pool to be considered reusable (by default - infinite)
        PooledConnectionLifetime = TimeSpan.FromMinutes(20),
        AutomaticDecompression = DecompressionMethods.All
    };

    if (disableSslVerification)
    {
        httpHandler.SslOptions.RemoteCertificateValidationCallback =
            (sender, certificate, chain, sslPolicyErrors) => true;
    }

    HttpClient httpClient = new(httpHandler, disposeHandler: false)
    {
        Timeout = TimeSpan.FromMinutes(5)
    };
    return httpClient;
}

#endregion