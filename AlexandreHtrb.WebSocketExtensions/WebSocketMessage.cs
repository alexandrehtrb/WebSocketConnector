using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AlexandreHtrb.WebSocketExtensions;

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
    public bool CanRead => BytesStream is MemoryStream;

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

    public byte[] ReadBytes() =>
        BytesStream is MemoryStream ms ?
        ms.ToArray() :
        throw new NotSupportedException("Parsing available only for MemoryStreams.");

    public string? ReadAsUtf8Text() =>
        BytesStream is MemoryStream ms ?
        Encoding.UTF8.GetString(ms.ToArray()) :
        throw new NotSupportedException("Parsing available only for MemoryStreams.");

    public T? ReadAsUtf8Json<T>(JsonTypeInfo<T> jsonTypeInfo) =>
        BytesStream is MemoryStream ms ?
        JsonSerializer.Deserialize(ms.ToArray(), jsonTypeInfo) :
        throw new NotSupportedException("Parsing available only for MemoryStreams.");
}