using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectInfrastructure.Models;

public class LeaderboardModel
{
    public string Id { get; set; }

    [Length(3, 30)]
    public string? Name { get; set; } = null!;
    public string? UserId { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<LeaderboardRecordModel>? Records { get; set; }

    public void Update(LeaderboardModel leaderboardModel)
    {
        if (!string.IsNullOrEmpty(leaderboardModel.Name))
            Name = leaderboardModel.Name;
        
        if (leaderboardModel.Records is not null && leaderboardModel.Records.Count > 0)
            Records = leaderboardModel.Records;
    }

    public void AddRecord(LeaderboardRecordModel record)
    {
        if (Records == null)
        {
            Records = new List<LeaderboardRecordModel>();
        }
        
        Records.Add(record);
    }

    public void RemoveRecord(LeaderboardRecordModel record)
    {
        if (Records != null && Records.Contains(record))
            Records.Remove(record);
    }
}