using System.Text.Json.Serialization;

namespace ProjectMVC.Utils.Sorting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortingType
{
    Ascending,
    Descending
}