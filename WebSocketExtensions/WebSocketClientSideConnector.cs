namespace System.Net.WebSockets;

public sealed class WebSocketClientSideConnector : WebSocketConnector
{
    protected override WebSocketMessageDirection DirectionFromThis => WebSocketMessageDirection.FromClient;

    public HttpStatusCode ConnectionHttpStatusCode { get; private set; }

    public IReadOnlyDictionary<string, IEnumerable<string>>? ConnectionHttpHeaders { get; private set; }

    private readonly HttpClient httpClient;

    public WebSocketClientSideConnector(ClientWebSocket client, HttpClient httpClient) : base(client)
    {
        this.httpClient = httpClient;
    }

    #region CONNECTION

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (ConnectionState == WebSocketConnectionState.Connected || ConnectionState == WebSocketConnectionState.Connecting)
            return;  // Not throwing exception if user tried to connect whilst WebSocket is connected

        try
        {
            SetIsConnecting();
            await ((ClientWebSocket)this.ws).ConnectAsync(uri!, this.httpClient, cancellationToken);
            SetupAfterConnected();
            SetIsConnected();
        }
        catch (Exception ex)
        {
            SetIsDisconnected(ex);
        }
    }

    protected override void SetupAfterConnected()
    {
        base.SetupAfterConnected();

        ConnectionHttpStatusCode = ((ClientWebSocket)this.ws).HttpStatusCode;
        ConnectionHttpHeaders = ((ClientWebSocket)this.ws).HttpResponseHeaders;
    }

    #endregion
}