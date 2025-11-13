# WebSocketConnector

[Ler em português](README_pt.md)

This project is a custom abstraction layer built on top of `System.Net.WebSockets` standard implementations, to handle WebSocket lifecycle, parse and convert messages from / to binary representations.

## How to use

1) Copy the `WebSocketExtensions` folder to your project's folder.
2) If you want to use this connector for a client, check out the [ConsoleExample](./ConsoleExample/Program.cs) code.
3) If you want to use this connector on a server endpoint, check out the [ApiExample](./ApiExample/Endpoints/BackgroundWebSocketsProcessor.cs) code.

### Sample code

```cs
WebSocketServerSideConnector wsc = new(ws);

await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    string msgText = msg.ReadAsUtf8Text()!;

    if (msg.Direction == WebSocketMessageDirection.FromServer)
        continue;

    await wsc.SendMessageAsync(WebSocketMessageType.Text, msgText switch
    {
        "Hello!" => "Hi!",
        "What time is it?" => "Now it's " + DateTime.Now.TimeOfDay,
        "Thanks!" => "You're welcome!",
        _ => "I don't understand this message!"
    }, false);
}
```
