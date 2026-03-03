using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

namespace TestClient;

internal sealed class SubprotocolAndServerDisconnectConversation : BaseConversation
{
    internal SubprotocolAndServerDisconnectConversation(WebSocketClientSideConnector wsc) : base(wsc)
    {
    }

    protected override async Task ReplyMessageAsync(WebSocketMessage msg, string msgText)
    {
        // Expected conversation:
        //  1) Client: Hello!
        //  2) Server: Hi!
        //  3) Client: Which subprotocol are we on?
        //  4.1) Server: No subprotocol specified.
        //  4.2) Server: We are on the subprotocol '{subprotocol}'.

        switch (msg.Type, msgText)
        {
            case (WebSocketMessageType.Text, "Hi!"):
                await Task.Delay(TimeSpan.FromSeconds(1));
                await wsc.SendMessageAsync(WebSocketMessageType.Text, "Which subprotocol are we on?", false);
                break;
            default:
                break;
        }
    }
}