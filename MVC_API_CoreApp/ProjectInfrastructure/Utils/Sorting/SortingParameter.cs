using System.Text.Json.Serialization;

namespace ProjectMVC.Utils.Sorting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortingParameter
{
    Name,
    Value,
    Place,
    Updated,
}