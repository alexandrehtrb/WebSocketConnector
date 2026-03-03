using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

namespace TestClient;

internal sealed class ServerClosureMessageConversation : BaseConversation
{
    internal ServerClosureMessageConversation(WebSocketClientSideConnector wsc) : base(wsc)
    {
    }

    protected override async Task ReplyMessageAsync(WebSocketMessage msg, string msgText)
    {
        // Expected conversation:
        //  1) Client: Hello!
        //  2) Server: Hi!
        //  3) Client: I can't leave without you!
        //  4) Server: Yes, you can. Bye!

        switch (msg.Type, msgText)
        {
            case (WebSocketMessageType.Text, "Hi!"):
                await Task.Delay(TimeSpan.FromSeconds(1));
                await wsc.SendMessageAsync(WebSocketMessageType.Text, "I can't leave without you!", false);
                break;
            default:
                break;
        }
    }
}