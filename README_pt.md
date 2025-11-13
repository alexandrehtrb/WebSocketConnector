# WebSocketConnector

[Read in english](README.md)

Este projeto é uma camada de abstração construída em cima das implementações-padrão do `System.Net.WebSockets`, para lidar com o ciclo de vida dos WebSockets e para parsear e converter mensagens de / para suas representações em binário.

## Como usar

1) Copiar a pasta `WebSocketExtensions` para o diretório do seu projeto.
2) Se quiser usar o conector para um cliente, confira o código do projeto [ConsoleExample](./ConsoleExample/Program.cs).
3) Se quiser usar o conector para um servidor (API), confira o código do projeto [ApiExample](./ApiExample/Endpoints/BackgroundWebSocketsProcessor.cs).

### Código de exemplo

```cs
WebSocketServerSideConnector wsc = new(ws);

await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
{
    string msgText = msg.ReadAsUtf8Text()!;

    if (msg.Direction == WebSocketMessageDirection.FromServer)
        continue;

    await wsc.SendMessageAsync(WebSocketMessageType.Text, msgText switch
    {
        "Olá!" => "Oi!",
        "Que horas são?" => "Agora são " + DateTime.Now.TimeOfDay,
        "Obrigado!" => "De nada!",
        _ => "Não entendi sua mensagem!"
    }, false);
}
```
