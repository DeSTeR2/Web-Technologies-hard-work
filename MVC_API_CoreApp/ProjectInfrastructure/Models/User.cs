using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace ProjectInfrastructure.Models;

public class User : IdentityUser
{
    [JsonIgnore]
    public ICollection<string>? LeaderboaradIds { get; set; }
}