using ProjectInfrastructure.Models;

namespace ProjectMVC.Utils.Sorting;

public class DescendingSort(SortingParameter sortingParameter) : SortingStrategy(sortingParameter)
{
    public override List<LeaderboardRecordModel> Sort(List<LeaderboardRecordModel> records)
    {
        records.Sort((rec1, rec2) =>
        {
            int compare = CompareBy(rec1, rec2);
            if (compare != 0) return compare * -1;

            return rec1.UpdatedTime.CompareTo(rec2.UpdatedTime);
        });

        return records;
    }
}