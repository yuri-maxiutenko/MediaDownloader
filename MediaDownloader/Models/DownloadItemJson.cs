using Newtonsoft.Json;

namespace MediaDownloader.Models
{
    public class DownloadItemJson
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "ext")]
        public string Ext { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "webpage_url")]
        public string WebpageUrl { get; set; }

        [JsonProperty(PropertyName = "entries")]
        public DownloadItemJson[] Entries { get; set; }

        [JsonProperty(PropertyName = "requested_formats")]
        public DownloadItemFormatJson[] RequestedFormats { get; set; }
    }

    public class DownloadItemFormatJson
    {
        [JsonProperty(PropertyName = "format")]
        public string Format { get; set; }

        [JsonProperty(PropertyName = "ext")]
        public string Ext { get; set; }

        [JsonProperty(PropertyName = "vcodec")]
        public string VideoCodec { get; set; }

        [JsonProperty(PropertyName = "acodec")]
        public string AudioCodec { get; set; }
    }
}