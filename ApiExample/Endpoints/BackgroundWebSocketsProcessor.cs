using System.Net.WebSockets;

namespace WebSocketApiExample.Endpoints;

public static class BackgroundWebSocketsProcessor
{
    private static readonly TimeSpan maximumLifetimePeriod = TimeSpan.FromSeconds(10);

    public static async Task RegisterAndProcessAsync(ILogger<WebSocketServerSideConnector> logger, WebSocket ws, string? subprotocol, TaskCompletionSource<object> socketFinishedTcs)
    {
        WebSocketServerSideConnector wsc = new(ws);

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            await wsc.SendMessageAsync(WebSocketMessageType.Text, "I will close this connection in around 8s.", false);
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(maximumLifetimePeriod);
            await wsc.DisconnectAsync();
        });

        int msgCount = 0;
        await foreach (var msg in wsc.ExchangedMessagesCollector!.ReadAllAsync())
        {
            msgCount++;
            string msgText = msg.ReadAsUtf8Text()!;
            logger.LogInformation("Message {msgCount}, {direction}: {msgText}", msgCount, msg.Direction, msgText);

            if (msg.Direction == WebSocketMessageDirection.FromServer)
            {
                continue;
            }

            await wsc.SendMessageAsync(WebSocketMessageType.Text, msgText switch
            {
                "Hello!" => "Hi!",
                "What time is it?" => "Now it's " + DateTime.Now.TimeOfDay,
                "Thanks!" => "You're welcome!",
                _ => "I don't understand this message!"
            }, false);
        }

        socketFinishedTcs.SetResult(true);
    }
}