using System.Text.Json.Serialization;

namespace MVC.Models
{
    
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Gender
        {
            Male,
            Female
        }
    
}
