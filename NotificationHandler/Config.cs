using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NotificationHandler
{
    internal class Config
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("user")]
        public string User { get; set; }
        [JsonPropertyName("device")]
        public string Device { get; set; }
    }
}
