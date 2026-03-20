using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

namespace TestClient.Conversations;

internal sealed class ServerExceptionConversation : BaseConversation
{
    internal ServerExceptionConversation(WebSocketClientSideConnector wsc) : base(wsc)
    {
    }

    protected override async Task ReplyMessageAsync(WebSocketMessage msg, string msgText)
    {
        // Expected conversation:
        //  1) Client: Hello!
        //  2) Server: Hi!
        //  3) Client: Throw an Exception!

        switch (msg.Type, msgText)
        {
            case (WebSocketMessageType.Text, "Hi!"):
                await Task.Delay(TimeSpan.FromSeconds(1));
                await wsc.SendMessageAsync(WebSocketMessageType.Text, "Throw an Exception!", false);
                break;
            default:
                break;
        }
    }
}