using System.Text.Json;
using System.Text.Json.Serialization;

namespace INTERCAL.Dap;

/// <summary>
/// Low-level DAP protocol message types and serialization.
/// DAP uses "Content-Length: N\r\n\r\n{json}" framing over stdin/stdout.
/// </summary>

public class DapMessage
{
    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public class DapRequest : DapMessage
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; set; }

    public DapRequest() { Type = "request"; }
}

public class DapResponse : DapMessage
{
    [JsonPropertyName("request_seq")]
    public int RequestSeq { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; }

    public DapResponse() { Type = "response"; }
}

public class DapEvent : DapMessage
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = "";

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; }

    public DapEvent() { Type = "event"; }
}

/// <summary>
/// Reads and writes DAP messages with Content-Length framing.
/// </summary>
public class DapTransport
{
    private readonly Stream _input;
    private readonly Stream _output;
    private readonly object _writeLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public DapTransport(Stream input, Stream output)
    {
        _input = input;
        _output = output;
    }

    public async Task<DapRequest?> ReadRequestAsync()
    {
        var headers = await ReadHeadersAsync();
        if (headers == null) return null;

        if (!headers.TryGetValue("Content-Length", out var lengthStr) ||
            !int.TryParse(lengthStr, out var length))
            return null;

        var buffer = new byte[length];
        int read = 0;
        while (read < length)
        {
            var n = await _input.ReadAsync(buffer.AsMemory(read, length - read));
            if (n == 0) return null;
            read += n;
        }

        return JsonSerializer.Deserialize<DapRequest>(buffer, JsonOptions);
    }

    public void Send(DapMessage message)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message, message.GetType(), JsonOptions);
        var header = $"Content-Length: {json.Length}\r\n\r\n";
        var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);

        lock (_writeLock)
        {
            _output.Write(headerBytes);
            _output.Write(json);
            _output.Flush();
        }
    }

    private async Task<Dictionary<string, string>?> ReadHeadersAsync()
    {
        var headers = new Dictionary<string, string>();
        var line = await ReadLineAsync();
        if (line == null) return null;

        while (!string.IsNullOrEmpty(line))
        {
            var colon = line.IndexOf(':');
            if (colon > 0)
                headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            line = await ReadLineAsync();
            if (line == null) return null;
        }

        return headers.Count > 0 ? headers : null;
    }

    private async Task<string?> ReadLineAsync()
    {
        var bytes = new List<byte>();
        var buf = new byte[1];
        while (true)
        {
            var n = await _input.ReadAsync(buf);
            if (n == 0) return null;
            if (buf[0] == '\n')
            {
                if (bytes.Count > 0 && bytes[^1] == '\r')
                    bytes.RemoveAt(bytes.Count - 1);
                return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
            }
            bytes.Add(buf[0]);
        }
    }
}
