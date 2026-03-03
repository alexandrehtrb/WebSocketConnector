using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

namespace TestClient;

internal sealed class ClientClosureMessageConversation : BaseConversation
{
    internal ClientClosureMessageConversation(WebSocketClientSideConnector wsc) : base(wsc)
    {
    }

    protected override async Task ReplyMessageAsync(WebSocketMessage msg, string msgText)
    {
        // Expected conversation:
        //  1) Client: Hello!
        //  2) Server: Hi!
        //  3) Client: Adiós muchacho

        switch (msg.Type, msgText)
        {
            case (WebSocketMessageType.Text, "Hi!"):
                await Task.Delay(TimeSpan.FromSeconds(1));
                await wsc.SendMessageAsync(WebSocketMessageType.Close, "Adiós muchacho", false);
                break;
            default:
                break;
        }
    }
}