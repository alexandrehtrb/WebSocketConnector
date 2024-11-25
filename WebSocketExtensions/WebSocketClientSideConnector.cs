namespace System.Net.WebSockets;

public sealed class WebSocketClientSideConnector : WebSocketConnector
{
    protected override WebSocketMessageDirection DirectionFromThis => WebSocketMessageDirection.FromClient;

    public HttpStatusCode ConnectionHttpStatusCode { get; private set; }

    public IReadOnlyDictionary<string, IEnumerable<string>>? ConnectionHttpHeaders { get; private set; }

    #region CONNECTION

    public async Task ConnectAsync(ClientWebSocket client, HttpClient httpClient, Uri uri, CancellationToken cancellationToken = default)
    {
        if (ConnectionState == WebSocketConnectionState.Connected || ConnectionState == WebSocketConnectionState.Connecting)
            return;  // Not throwing exception if user tried to connect whilst WebSocket is connected

        try
        {
            SetIsConnecting();
            await client.ConnectAsync(uri!, httpClient, cancellationToken);
            SetupAfterConnected(client);
            SetIsConnected();
        }
        catch (Exception ex)
        {
            SetIsDisconnected(ex);
        }
    }

    protected override void SetupAfterConnected(WebSocket ws)
    {
        base.SetupAfterConnected(ws);

        ConnectionHttpStatusCode = ((ClientWebSocket)ws).HttpStatusCode;
        ConnectionHttpHeaders = ((ClientWebSocket)ws).HttpResponseHeaders;
    }

    #endregion
}