using ProjectInfrastructure.Models;
using ProjectMVC.Controllers;

namespace ProjectMVC.Utils.Sorting;

public abstract class SortingStrategy
{
    private readonly SortingParameter _sortingParameter;

    public SortingStrategy(SortingParameter sortingParameter)
    {
        _sortingParameter = sortingParameter;
    }

    public abstract List<LeaderboardRecordModel> Sort(List<LeaderboardRecordModel> records);

    protected int CompareBy(LeaderboardRecordModel rec1, LeaderboardRecordModel rec2)
    {
        switch (_sortingParameter)
        {
            case SortingParameter.Value:
                return rec1.Value < rec2.Value ? -1 : rec1.Value > rec2.Value ? 1 : 0;
            case SortingParameter.Place:
                return rec1.Place < rec2.Place ? -1 : rec1.Place > rec2.Place ? 1 : 0;
            case SortingParameter.Updated:
                return rec1.UpdatedTime.CompareTo(rec2.UpdatedTime);
            case SortingParameter.Name:
                return string.Compare(rec1.Name, rec2.Name, StringComparison.OrdinalIgnoreCase);
            default:
                throw new ArgumentOutOfRangeException(nameof(_sortingParameter), $"Unknown sort parameter: {_sortingParameter}");
        }
    }
}