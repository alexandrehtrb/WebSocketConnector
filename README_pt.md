# WebSocketConnector

[Read in english](README.md)

Este pacote é uma camada de abstração construída em cima das implementações-padrão do `System.Net.WebSockets`, para lidar com o ciclo de vida dos WebSockets e para parsear e converter suas mensagens de byte arrays. Estas abstrações são tanto para cliente como para servidor (ASP.NET).

Ele é compatível com compilação NativeAOT e trimming.

- [Como usar](#como-usar)
  - [Código de exemplo, servidor](#código-de-exemplo-servidor)
  - [Código de exemplo, cliente](#código-de-exemplo-cliente)
- [Configuração de WebSockets no ASP.NET](#configuração-de-websockets-no-asp-net)
- [Dicas](#dicas)
  - [Monitorar estado da conexão](#monitorar-estado-da-conexão)
  - [Enviar mensagem periodicamente](#enviar-mensagem-periodicamente)
  - [Encerrar conversa após determinado tempo](#encerrar-conversa-após-determinado-tempo)
  - [Pegar HTTP status code e headers de resposta](#pegar-http-status-code-e-headers-de-resposta)
  - [Autenticação e headers de requisição](#autenticação-e-headers-adicionais-de-requisição)
  - [Subprotocolos](#subprotocolos)
  - [Compressão de mensagens](#compressão-de-mensagens)
  - [WebSockets em HTTP/2](#websockets-em-http-2)

## Como usar

A utilização é bem simples. As classes `WebSocketServerSideConnector` e `WebSocketClientSideConnector` recebem objetos `WebSocket` nativos do .NET e cuidam de todo o ciclo de vida dos WebSockets, conexão e desconexão, recebimento e envio de mensagens, e conversão de/para bytes.

Dentro de cada conector há um `ExchangedMessagesCollector`, que coleta as mensagens enviadas e recebidas, e as disponibiliza em um `IAsyncEnumerable`. Assim, a conversa entre cliente e servidor entra em um loop `await foreach` (assíncrono). Quando uma das partes se desconecta, o loop é encerrado.

Antes de entrar no loop da conversa, uma das partes deve ter a iniciativa de mandar uma mensagem.

### Código de exemplo, servidor

```cs
WebSocketServerSideConnector wsc = new(ws);

await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    if (msg.Direction == WebSocketMessageDirection.FromServer)
        continue; // ignorar msgs do próprio lado

    await wsc.SendMessageAsync(WebSocketMessageType.Text, msg.ReadAsUtf8Text() switch
    {
        "Olá!" => "Oi!",
        "Que horas são?" => "Agora são " + DateTime.Now.TimeOfDay,
        "Obrigado!" => "De nada!",
        _ => "Não entendi sua mensagem!"
    }, false);
}
```

### Código de exemplo, cliente

```cs
using var cws = MakeClientWebSocket();
using var hc = MakeHttpClient(disableSslVerification: true);
Uri uri = new("ws://localhost:5000/test/http1websocket");
WebSocketClientSideConnector wsc = new();

// Conectando
await wsc.ConnectAsync(cws, hc, uri, cancellationToken);

// Envio da primeira mensagem
await wsc.SendMessageAsync(WebSocketMessageType.Text, "Olá!", false);

// Loop da conversa
await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    if (msg.Direction == WebSocketMessageDirection.FromClient)
        continue; // ignorar msgs do próprio lado

    await wsc.SendMessageAsync(WebSocketMessageType.Text, msg.ReadAsUtf8Text() switch
    {
        "Oi!" => "Que horas são?",
        string s when s.StartsWith("Agora são") => "Obrigado!",
        _ => "Não entendi sua mensagem!"
    }, false);
}
```

Com o código acima, a conversa se desenrolará assim:

```
Cliente: Olá!
Servidor: Oi!
Cliente: Que horas são?
Servidor: Agora são 11:54:53
Cliente: Obrigado!
Servidor: De nada!
```

## Configuração de WebSockets no ASP.NET

1) Habilitar `app.UseWebSockets()`:

```cs
private static IApplicationBuilder ConfigureApp(this WebApplication app) =>
	app.MapTestEndpoints()
	   .UseWebSockets(new()
	   {
		   KeepAliveInterval = TimeSpan.FromMinutes(2)
	   });
```

2) Mapear o endpoint para comunicação WebSocket:

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

3) Criar um processador para o WebSocket:

```cs
using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

public static class BackgroundWebSocketsProcessor
{
    private static readonly TimeSpan maximumLifetimePeriod = TimeSpan.FromSeconds(6);

    public static async Task RegisterAndProcessAsync(ILogger<BackgroundWebSocketsProcessor> logger, WebSocket ws, TaskCompletionSource<object> socketFinishedTcs)
    {
        WebSocketServerSideConnector wsc = new(ws);

        int msgCount = 0;
        await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
        {
            msgCount++;
            string msgText = msg.Type switch
            {
                WebSocketMessageType.Text or WebSocketMessageType.Close => msg.ReadAsUtf8Text()!,
                WebSocketMessageType.Binary when msg.BytesStream is MemoryStream ms => $"(binary, {ms.Length} bytes)",
                WebSocketMessageType.Binary when msg.BytesStream is not MemoryStream => $"(binary, ? bytes)",
                _ => "(unknown)"
            };
            logger.LogInformation("Message {msgCount}, {direction}: {msgText}", msgCount, msg.Direction, msgText);

            if (msg.Direction == WebSocketMessageDirection.FromServer)
				continue; // ignorar msgs do próprio lado
				
			// tratamento das mensagens aqui
		}
		
		socketFinishedTcs.SetResult(true); // fim da conversa
	}
}
```

## Dicas

### Monitorar estado da conexão

```cs
wsc.OnConnectionChanged = (state, exception) =>
{
    logger.LogInformation("Connection state: {state}", state);
    logger.LogError(exception, "Connection exception");
};
```

Aqui é possível colocar tentativas de reconexão.

### Enviar mensagem periodicamente

```cs
while (!cancellationToken.IsCancellationRequested)
{
	_ = Task.Run(async () =>
	{
		await Task.Delay(TimeSpan.FromSeconds(15));
		await wsc.SendMessageAsync(WebSocketMessageType.Text, "Alô", false);
	});
}
```

### Encerrar conversa após determinado tempo

```cs
_ = Task.Run(async () =>
{
    await Task.Delay(maximumLifetimePeriod);
    await wsc.DisconnectAsync();
});
```

### Pegar HTTP status code e headers de resposta

```cs
ClientWebSocket cws = new();
cws.Options.CollectHttpResponseDetails = true;

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);

var wsHttpStatusCode = wsc.ConnectionHttpStatusCode;
var wsResponseHeaders = wsc.ConnectionHttpHeaders;
```

### Autenticação e headers de requisição

```cs
ClientWebSocket cws = new();
cws.Options.SetRequestHeader("Authorization", "Bearer my_token");
cws.Options.SetRequestHeader("Header1", "Value1");

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```

### Subprotocolos

#### Do lado do cliente

```cs
ClientWebSocket cws = new();
cws.Options.AddSubProtocol("subprotocol1");

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```

#### Do lado do servidor

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
+		string? subprotocol = webSocket.SubProtocol ?? httpCtx.WebSockets.WebSocketRequestedProtocols.FirstOrDefault();

		await BackgroundWebSocketsProcessor.RegisterAndProcessAsync(logger, webSocket, subprotocol, socketFinishedTcs);
		await socketFinishedTcs.Task;
	}
}
```

### Compressão de mensagens

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

**Importante:** Não se deve passar segredos e mensagens criptografadas ao mesmo tempo em que se usa compressão, pois há risco de [ataques BREACH e CRIME](https://www.breachattack.com/). Nesses casos, deve-se desabilitar a compressão nessas mensagens:

```cs
await wsc.SendMessageAsync(
    WebSocketMessageType.Text,
    $"Token criptografado {token}",
    disableCompression: true);
```

### WebSockets em HTTP/2

```cs
ClientWebSocket cws = new();
cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
cws.Options.HttpVersion = new(2,0);

await wsc.ConnectAsync(cws, hc, uri, cancellationToken);
```
