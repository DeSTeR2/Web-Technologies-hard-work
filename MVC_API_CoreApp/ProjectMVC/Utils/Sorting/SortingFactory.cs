namespace ProjectMVC.Utils.Sorting;

public class SortingFactory
{
    public SortingStrategy GetStrategy(SortingParameter sortBy, SortingType dirrection)
    {
        switch (dirrection)
        {
            case SortingType.Ascending:
                return new AscendingSort(sortBy);
            case SortingType.Descending:
                return new DescendingSort(sortBy);
            default:
                throw new Exception($"Now valid sorting type {sortBy}");
        }
        
    }
}