using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

namespace TestClient;

internal abstract class BaseConversation
{
    protected readonly WebSocketClientSideConnector wsc;

    protected BaseConversation(WebSocketClientSideConnector wsc) =>
        this.wsc = wsc;

    protected abstract Task ReplyMessageAsync(WebSocketMessage msg, string msgText);

    internal async Task<List<WebSocketMessage>> RunAsync(CancellationToken cancellationToken)
    {
        List<WebSocketMessage> collectedMsgs = new(15);

        Console.WriteLine("--- CONVERSATION BEGIN ---");

        // Expected conversation:
        //  1) Client: Hello!
        //  2) Server: Hi!

        // check here if connection is OK before sending messages
        await wsc.SendMessageAsync(WebSocketMessageType.Text, "Hello!", false);

        int msgCount = 0;
        await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync(cancellationToken))
        {
            collectedMsgs.Add(msg);
            msgCount++;
            string msgText = msg.Type switch
            {
                WebSocketMessageType.Text or WebSocketMessageType.Close => msg.ReadAsUtf8Text()!,
                WebSocketMessageType.Binary when msg.BytesStream is MemoryStream ms => $"(binary, {ms.Length} bytes)",
                WebSocketMessageType.Binary when msg.BytesStream is not MemoryStream => $"(binary, ? bytes)",
                _ => "(unknown)"
            };
            Console.WriteLine($"Message {msgCount}, {msg.Direction}: {msgText}");

            if (msg.Direction == WebSocketMessageDirection.FromClient)
            {
                continue;
            }

            await ReplyMessageAsync(msg, msgText);
        }

        Console.WriteLine("--- CONVERSATION ENDED ---");

        return collectedMsgs;
    }
}