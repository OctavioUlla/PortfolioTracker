namespace PortfolioTracker.Models;

public class StockWatchListViewModel
{
    public List<StockWatchListItem> Items { get; set; } = new();
    public StockWatchListItemFormViewModel Form { get; set; } = new();
}
