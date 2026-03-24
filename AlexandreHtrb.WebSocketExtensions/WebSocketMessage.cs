using System.Diagnostics;
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

#if DEBUG
[DebuggerDisplay("{DescriptionForDebugger,nq}")]
#endif
public sealed class WebSocketMessage
{
    internal bool IsStreamBased => BytesStream is not null;
    internal byte[]? Bytes { get; }
    internal Stream? BytesStream { get; }
    public WebSocketMessageDirection Direction { get; }
    public WebSocketMessageType Type { get; }
    internal bool DisableCompression { get; }

#if DEBUG
    private string DescriptionForDebugger => FormatForLogging();
#endif

    internal WebSocketMessage(WebSocketMessageDirection direction, WebSocketMessageType type, byte[] bytes, bool disableCompression)
    {
        Direction = direction;
        Type = type;
        DisableCompression = disableCompression;
        Bytes = bytes;
    }

    internal WebSocketMessage(WebSocketMessageDirection direction, WebSocketMessageType type, string txt, bool disableCompression)
    {
        Direction = direction;
        Type = type;
        DisableCompression = disableCompression;
        Bytes = Encoding.UTF8.GetBytes(txt);
    }

    internal WebSocketMessage(WebSocketMessageDirection direction, WebSocketMessageType type, Stream bytesStream, bool disableCompression)
    {
        Direction = direction;
        Type = type;
        DisableCompression = disableCompression;
        BytesStream = bytesStream;
    }

    internal WebSocketMessageFlags DetermineFlags()
    {
        var flags = WebSocketMessageFlags.None;

        if (!IsStreamBased || ReachedEndOfStream())
            flags |= WebSocketMessageFlags.EndOfMessage;

        if (DisableCompression)
            flags |= WebSocketMessageFlags.DisableCompression;

        return flags;
    }

    internal bool ReachedEndOfStream() =>
        // CanRead check below is required to avoid exceptions
        BytesStream is not null && (!BytesStream.CanRead || (BytesStream.Position == BytesStream.Length));

    public byte[] ReadBytes() =>
        Bytes is not null ?
        Bytes :
        BytesStream is MemoryStream ms ?
        ms.ToArray() :
        throw new NotSupportedException("Parsing available only for MemoryStreams.");

    public Stream ReadAsStream() =>
        BytesStream is not null ?
        BytesStream :
        new MemoryStream(Bytes!);

    public string? ReadAsUtf8Text() =>
        Bytes is not null ?
        Encoding.UTF8.GetString(Bytes) :
        BytesStream is MemoryStream ms ?
        Encoding.UTF8.GetString(ms.ToArray()) :
        throw new NotSupportedException("Parsing available only for MemoryStreams.");

    public T ReadAsUtf8Json<T>(JsonTypeInfo<T> jsonTypeInfo) =>
        Bytes is not null ?
        JsonSerializer.Deserialize(Bytes.AsSpan(), jsonTypeInfo)! :
        BytesStream is MemoryStream ms ?
        JsonSerializer.Deserialize(ms.ToArray(), jsonTypeInfo)! :
        throw new NotSupportedException("Parsing available only for MemoryStreams.");

    public string FormatForLogging() => Type switch
    {
        WebSocketMessageType.Text or WebSocketMessageType.Close when Bytes is not null || BytesStream is MemoryStream => ReadAsUtf8Text()!,
        WebSocketMessageType.Text when Bytes is null && (BytesStream is null || BytesStream is not MemoryStream) => "(text, ? bytes)",
        WebSocketMessageType.Close when Bytes is null && (BytesStream is null || BytesStream is not MemoryStream) => "(close, ? bytes)",
        WebSocketMessageType.Binary when Bytes is not null => $"(binary, {Bytes.Length} bytes)",
        WebSocketMessageType.Binary when BytesStream is MemoryStream ms => $"(binary, {ms.Length} bytes)",
        WebSocketMessageType.Binary when BytesStream is not MemoryStream => "(binary, ? bytes)",
        _ => "(unknown)"
    };
}