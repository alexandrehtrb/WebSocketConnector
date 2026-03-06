using System.Net.WebSockets;

namespace AlexandreHtrb.WebSocketExtensions;

public sealed class WebSocketServerSideConnector : WebSocketConnector
{
    protected override WebSocketMessageDirection DirectionFromThis => WebSocketMessageDirection.FromServer;

    public WebSocketServerSideConnector(WebSocket ws, bool collectOnlyClientSideMessages) : base(collectOnlyClientSideMessages)
    {
        // when this connector gets created, the connection is already established
        SetIsConnected();
        base.SetupAfterConnected(ws);
    }
}