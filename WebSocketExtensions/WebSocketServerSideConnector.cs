namespace System.Net.WebSockets;

public sealed class WebSocketServerSideConnector : WebSocketConnector
{
    protected override WebSocketMessageDirection DirectionFromThis => WebSocketMessageDirection.FromServer;

    public WebSocketServerSideConnector(WebSocket client) : base(client)
    {
        // when this connector gets created, the connection is already established
        SetIsConnected();
        base.SetupAfterConnected();
    }
}