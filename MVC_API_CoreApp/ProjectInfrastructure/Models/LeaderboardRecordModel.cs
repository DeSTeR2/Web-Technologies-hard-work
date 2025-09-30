using System.Text.Json.Serialization;

namespace ProjectInfrastructure.Models;

public class LeaderboardRecordModel
{
    public string? Id { get; set; }
    public int? Place { get; set; }
    public string? Name { get; set; }
    public int? Value { get; set; }
    public DateTime UpdatedTime { get; set; }
    
    public string LeaderboardId { get; set; }
    
    [JsonIgnore]
    public LeaderboardModel? Leaderboard { get; set; }

    public void Update(LeaderboardRecordModel record)
    {
        if (string.IsNullOrEmpty(record.Name))
        {
            Name = record.Name;
        }

        Value = record.Value;
        UpdatedTime = DateTime.Now;
    }
}