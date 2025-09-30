using ProjectMVC.Utils.Sorting;

namespace ProjectMVC.Models.Requests;

public class UpdatePositionsRequest
{
    public string LeaderboardId { get; set; }
    public SortingParameter SortBy { get; set; } = SortingParameter.Value;
    public SortingType Direction { get; set; } = SortingType.Descending;
    public int? Take { get; set; }
}
