using AlexandreHtrb.WebSocketExtensions;
using System.Net.WebSockets;

namespace TestClient.Conversations
{
    internal sealed class ClientDisconnectConversation : BaseConversation
    {
        internal ClientDisconnectConversation(WebSocketClientSideConnector wsc) : base(wsc)
        {
        }

        protected override async Task ReplyMessageAsync(WebSocketMessage msg, string msgText)
        {
            switch (msg.Type, msgText)
            {
                case (WebSocketMessageType.Text, "Hi!"):
                    await wsc.DisconnectAsync(default);
                    break;
                default:
                    break;
            }
        }
    }
}