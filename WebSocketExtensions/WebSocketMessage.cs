using System.Text;
using System.Text.Json;

namespace System.Net.WebSockets;

public enum WebSocketMessageDirection
{
    FromClient = 0,
    FromServer = 1
}

public sealed class WebSocketMessage
{
    public WebSocketMessageDirection Direction { get; }
    public WebSocketMessageType Type { get; }
    public Stream BytesStream { get; }
    public bool DisableCompression { get; }

    internal WebSocketMessage(WebSocketMessageDirection direction, WebSocketMessageType type, Stream bytesStream, bool disableCompression)
    {
        Direction = direction;
        Type = type;
        BytesStream = bytesStream;
        DisableCompression = disableCompression;
    }

    internal WebSocketMessage(WebSocketMessageDirection direction, WebSocketMessageType type, byte[] bytes, bool disableCompression)
    {
        Direction = direction;
        Type = type;
        BytesStream = new MemoryStream(bytes);
        DisableCompression = disableCompression;
    }

    internal WebSocketMessage(WebSocketMessageDirection direction, WebSocketMessageType type, string text, bool disableCompression)
    {
        Direction = direction;
        Type = type;
        BytesStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        DisableCompression = disableCompression;
    }

    internal WebSocketMessageFlags DetermineFlags()
    {
        var flags = WebSocketMessageFlags.None;

        if (ReachedEndOfStream())
            flags |= WebSocketMessageFlags.EndOfMessage;

        if (DisableCompression)
            flags |= WebSocketMessageFlags.DisableCompression;

        return flags;
    }

    internal bool ReachedEndOfStream() =>
        !BytesStream.CanRead || (BytesStream.Position == BytesStream.Length);
        // CanRead check above is required to avoid exceptions

    public string? ReadAsUtf8Text() =>
        BytesStream is MemoryStream ms ?
        Encoding.UTF8.GetString(ms.ToArray()) :
        throw new Exception("Parsing available only for MemoryStreams.");

    public T? ReadAsJson<T>(JsonSerializerOptions? options = default) =>
        BytesStream is MemoryStream ms ?
        JsonSerializer.Deserialize<T>(ms.ToArray(), options) :
        throw new Exception("Parsing available only for MemoryStreams.");
}