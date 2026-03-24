# AlexandreHtrb.WebSocketExtensions

[Ler em português](README_pt.md)

This project is a custom abstraction layer built on top of `System.Net.WebSockets` standard implementations, to handle WebSocket lifecycle, parse and convert messages from / to byte arrays. The abstractions are for both client-side and server-side (ASP.NET).

It has full compatibility with NativeAOT and trimming.

- [Installation](#installation)
- [How to use](#how-to-use)
  - [Example code, server](#example-code-server)
  - [Example code, client](#example-code-client)
- [WebSockets configuration on ASP.NET](#websockets-configuration-on-aspnet)
- [Tips and tricks](#tips-and-tricks)
  - [Monitor connection state](#monitor-connection-state)
  - [Keep-Alive](#keep-alive)
  - [Collect sent messages](#collect-sent-messages)
  - [Periodically send a message](#periodically-send-a-message)
  - [End conversation after a certain amount of time](#end-conversation-after-a-certain-amount-of-time)
  - [Sending files](#sending-files)
  - [Retrieve HTTP status code and response headers](#retrieve-http-status-code-and-response-headers)
  - [Authentication and request headers](#authentication-and-request-headers)
  - [Subprotocols](#subprotocols)
  - [Message compression](#message-compression)
  - [WebSockets over HTTP/2](#websockets-over-http2)

## Installation

Add the [NuGet package](https://www.nuget.org/packages/AlexandreHtrb.WebSocketExtensions) to the project file:

```xml
<ItemGroup>
    <PackageReference Include="AlexandreHtrb.WebSocketExtensions" Version="1.1.1" />
</ItemGroup>
```

## How to use

It's quite simple to use. The `WebSocketServerSideConnector` and `WebSocketClientSideConnector` classes receive native `WebSocket` objects from .NET and take care of all WebSocket's lifecycle, connection and disconnection, receiving and sending messages, and conversion from/to byte arrays.

Inside each connector there is an `ExchangedMessagesCollector`, which collects the messages sent and received, and makes them available through an `IAsyncEnumerable`. Thus, the conversation between client and server goes inside an asynchronous `await foreach` loop. When one of the parties disconnects, the execution leaves the loop.

Before entering the conversation loop, one of the parties must take the initiative to send a message.

### Example code, server

```cs
WebSocketServerSideConnector wsc = new(ws, collectOnlyClientSideMessages: true);

await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    await wsc.SendMessageAsync(WebSocketMessageType.Text, msg.ReadAsUtf8Text() switch
    {
        "Hello!" => "Hi!",
        "What time is it?" => "Now it's " + DateTime.Now.TimeOfDay,
        "Thanks!" => "You're welcome!",
        _ => "I don't understand your message!"
    }, false);
}
```

### Example code, client

```cs
using var cws = MakeClientWebSocket();
using var hc = MakeHttpClient(disableSslVerification: true);
Uri uri = new("ws://localhost:5000/test/http1websocket");
WebSocketClientSideConnector wsc = new(collectOnlyServerSideMessages: true);

// Connecting
await wsc.ConnectAsync(cws, hc, uri, cancellationToken);

// Sending first message
await wsc.SendMessageAsync(WebSocketMessageType.Text, "Hello!", false);

// Conversation loop
await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    string? replyTxt = msg.ReadAsUtf8Text() switch
    {
        "Hi!" => "What time is it?",
        string s when s.StartsWith("Now it's") => "Thanks!",
        _ => null
    };
    
    if (replyTxt != null)
        await wsc.SendMessageAsync(WebSocketMessageType.Text, replyTxt, false);
}
```

With the code above, the conversation should go on like this:

```
Client: Hello!
Server: Hi!
Client: What time is it?
Server: Now it's 11:54:53
Client: Thanks!
Server: You're welcome!
```

## WebSockets configuration on ASP.NET

1) Enable `app.UseWebSockets()`:

```cs
private static IApplicationBuilder ConfigureApp(this WebApplication app) =>
    app.MapTestEndpoints()
       .UseWebSockets(new()
       {
           KeepAliveInterval = TimeSpan.FromMinutes(2)
       });
```

2) Map the WebSocket endpoint:

```cs
public static WebApplication MapTestEndpoints(this WebApplication app)
{
    app.MapGet("test/http1websocket", TestHttp1WebSocket);
    return app;
}

private static async Task TestHttp1WebSocket(HttpContext httpCtx, ILogger<BackgroundWebSocketsProcessor> logger)
{
    if (!httpCtx.WebSockets.IsWebSocketRequest)
    {
        byte[] txtBytes = Encoding.UTF8.GetBytes("Only WebSockets requests are accepted here!");
        httpCtx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await httpCtx.Response.BodyWriter.WriteAsync(txtBytes);
    }
    else
    {
        using var webSocket = await httpCtx.WebSockets.AcceptWebSocketAsync();
        TaskCompletionSource<object> socketFinishedTcs = new();

        await BackgroundWebSocketsProcessor.RegisterAndProcessAsync(logger, webSocket, socketFinishedTcs);
        await socketFinishedTcs.Task;
    }
}
```

3) Create a WebSocket processor:

```cs
using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

public static class BackgroundWebSocketsProcessor
{
    public static async Task RegisterAndProcessAsync(ILogger<BackgroundWebSocketsProcessor> logger, WebSocket ws, TaskCompletionSource<object> socketFinishedTcs)
    {
        WebSocketServerSideConnector wsc = new(ws, collectOnlyClientSideMessages: true);

        int msgCount = 0;
        await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
        {
            logger.LogInformation("Message {msgCount}, {direction}: {msgText}", ++msgCount, msg.Direction, msg.FormatForLogging());
                
            // handle messages here
        }
        
        socketFinishedTcs.SetResult(true); // finish connection
    }
}
```

## Tips and tricks

### Monitor connection state

```cs
wsc.OnConnectionChanged = (state, exception) =>
{
    logger.LogInformation("Connection state: {state}", state);
    logger.LogError(exception, "Connection exception");
};
```

Here we can put connection retries.

### Keep-Alive

Keep-Alive is the mechanism to keep the WebSocket alive, without being cut-off by proxies and middleboxes.

On .NET, we can configure a frequency interval, where the party sends a *ping* frame, and a timeout period, which the party awaits for a *pong* response frame after the *ping* frame has been sent. If a *pong* is not received, the connection is considered dead.

This topic is covered in more detail on [this WebSocket.org page](https://websocket.org/guides/heartbeat/).

#### Client-side

```cs
ClientWebSocket cws = new();
cws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
#if NET10_0_OR_GREATER
cws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(45);
#endif

await wsc.ConnectAsync(cws, hc, uri, default);
```

#### Server-side

```cs
private static IApplicationBuilder ConfigureApp(this WebApplication app) =>
    app.MapTestEndpoints()
       .UseWebSockets(new()
       {
           KeepAliveInterval = TimeSpan.FromSeconds(30),
#if NET10_0_OR_GREATER
		   KeepAliveTimeout = TimeSpan.FromSeconds(45)
#endif
       });
```

### Collect sent messages

```cs
WebSocketServerSideConnector wsc = new(ws, collectOnlyClientSideMessages: false);

WebSocketClientSideConnector wsc = new(collectOnlyServerSideMessages: false);
```

The booleans control whether only messages from the opposite side will be collected. Collecting messages from the own side may be interesting for logging.

### Periodically send a message

```cs
while (!cancellationToken.IsCancellationRequested)
{
    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
    await wsc.SendMessageAsync(WebSocketMessageType.Text, "Are you there?", false);
}
```

### End conversation after a certain amount of time

```cs
_ = Task.Run(async () =>
{
    await Task.Delay(maximumLifetimePeriod, cancellationToken);
    await wsc.DisconnectAsync();
});
```

### Sending files

```cs
// don't use 'using'
FileStream fs = new("C:\\Files\my_image.jpg", FileMode.Open);
await wsc.SendMessageAsync(WebSocketMessageType.Binary, fs, false);
```

When using Streams to send messages, don't use the `using` keyword. The Stream will be disposed further, inside the connector.

Also, when sending files, it may be interesting to set a larger buffer size on the WebSocketConnector. When larger buffers are used, less roundtrips are required to read a Stream, which may make for quicker transmissions.

```cs
WebSocketServerSideConnector wsc = new(ws, true, bufferSize: 65_536);

WebSocketClientSideConnector wsc = new(bufferSize: 65_536);
```

### Retrieve HTTP status code and response headers

```cs
ClientWebSocket cws = new();
cws.Options.CollectHttpResponseDetails = true;

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);

var wsHttpStatusCode = wsc.ConnectionHttpStatusCode;
var wsResponseHeaders = wsc.ConnectionHttpHeaders;
```

### Authentication and request headers

```cs
ClientWebSocket cws = new();
cws.Options.SetRequestHeader("Authorization", "Bearer my_token");
cws.Options.SetRequestHeader("Header1", "Value1");

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```

### Subprotocols

#### Client-side

```cs
ClientWebSocket cws = new();
cws.Options.AddSubProtocol("subprotocol1");

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```

#### Server-side

```diff
private static async Task TestHttp1WebSocket(HttpContext httpCtx, ILogger<BackgroundWebSocketsProcessor> logger)
{
    if (!httpCtx.WebSockets.IsWebSocketRequest)
    {
        // ...
    }
    else
    {
        using var webSocket = await httpCtx.WebSockets.AcceptWebSocketAsync();
        TaskCompletionSource<object> socketFinishedTcs = new();
+       string? subprotocol = webSocket.SubProtocol ?? httpCtx.WebSockets.WebSocketRequestedProtocols.FirstOrDefault();

        await BackgroundWebSocketsProcessor.RegisterAndProcessAsync(logger, webSocket, subprotocol, socketFinishedTcs);
        await socketFinishedTcs.Task;
    }
}
```

### Message compression

```cs
ClientWebSocket cws = new();
cws.Options.DangerousDeflateOptions = new()
{
    ClientContextTakeover = true,
    ClientMaxWindowBits = 14,
    ServerContextTakeover = true,
    ServerMaxWindowBits = 14
};

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```

**Important:** Don't pass secrets and encrypted texts in compressed messages, because there is the risk of [BREACH and CRIME attacks](https://www.breachattack.com/). In these cases, disable compression for those messages:

```cs
await wsc.SendMessageAsync(
    WebSocketMessageType.Text,
    $"Encrypted token {token}",
    disableCompression: true);
```

### WebSockets over HTTP/2

#### Client-side

```cs
ClientWebSocket cws = new();
cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
cws.Options.HttpVersion = new(2,0);

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```

#### Server-side

On HTTP/2 WebSockets, the HTTP method CONNECT is used, instead of GET.

```cs
public static WebApplication MapTestEndpoints(this WebApplication app)
{
    app.MapMethods("test/http2websocket", new[] { HttpMethods.Connect }, TestHttp2WebSocket);
    return app;
}
```
