using ProjectMVC.Models.Requests;
using ProjectMVC.Utils.Sorting;

namespace ProjectInfrastructure.Models
{
    public class UpdatePositionsRequest : PaginationRequest
    {
        public string LeaderboardId { get; set; }
        public SortingParameter SortBy { get; set; } = SortingParameter.Value;
        public SortingType Direction { get; set; } = SortingType.Descending;

        public int Take { get; set; }
    }
}