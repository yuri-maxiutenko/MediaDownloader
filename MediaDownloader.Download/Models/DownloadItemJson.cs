using System.Text.Json.Serialization;

namespace MediaDownloader.Download.Models;

public class DownloadItemJson
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("ext")]
    public string? Ext { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("webpage_url")]
    public string? WebpageUrl { get; init; }

    [JsonPropertyName("entries")]
    public DownloadItemJson[]? Entries { get; init; }

    [JsonPropertyName("requested_formats")]
    public DownloadItemFormatJson[]? RequestedFormats { get; init; }
}

public class DownloadItemFormatJson
{
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("ext")]
    public string? Ext { get; set; }

    [JsonPropertyName("vcodec")]
    public string? VideoCodec { get; set; }

    [JsonPropertyName("acodec")]
    public string? AudioCodec { get; set; }
}
