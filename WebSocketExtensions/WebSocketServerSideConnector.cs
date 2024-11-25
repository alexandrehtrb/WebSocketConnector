namespace System.Net.WebSockets;

public sealed class WebSocketServerSideConnector : WebSocketConnector
{
    protected override WebSocketMessageDirection DirectionFromThis => WebSocketMessageDirection.FromServer;

    public WebSocketServerSideConnector(WebSocket ws)
    {
        // when this connector gets created, the connection is already established
        SetIsConnected();
        base.SetupAfterConnected(ws);
    }
}