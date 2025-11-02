namespace ProjectMVC.Utils.Errors;

public class LeaderboardError : IDbError
{
    public string Error(string objectId)
    {
        return $"Leaderboard with ID: {objectId} is not present in db";
    }
}