using PortfolioTracker.Models;

namespace PortfolioTracker.Services;

public class StockHoldingViewModel
{
    public string Ticker { get; set; } = string.Empty;
    public decimal SharesHeld { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageHoldingDays { get; set; }
}

public static class StockHoldingsCalculator
{
    public static List<StockHoldingViewModel> Calculate(List<StockTrade> trades)
    {
        var today = DateTime.Today;
        var result = new List<StockHoldingViewModel>();

        var tickers = trades.Select(t => t.Ticker).Distinct().OrderBy(t => t);

        foreach (var ticker in tickers)
        {
            var tickerTrades = trades
                .Where(t => t.Ticker == ticker)
                .OrderBy(t => t.Date)
                .ToList();

            // FIFO lot tracking: each lot is (date, quantity, price)
            var lots = new List<(DateTime Date, decimal Quantity, decimal Price)>();

            foreach (var trade in tickerTrades)
            {
                if (trade.Type == TradeType.Buy)
                {
                    lots.Add((trade.Date, trade.Quantity, trade.Price));
                }
                else // Sell
                {
                    decimal remaining = trade.Quantity;
                    while (remaining > 0 && lots.Count > 0)
                    {
                        if (lots[0].Quantity <= remaining)
                        {
                            remaining -= lots[0].Quantity;
                            lots.RemoveAt(0);
                        }
                        else
                        {
                            lots[0] = (lots[0].Date, lots[0].Quantity - remaining, lots[0].Price);
                            remaining = 0;
                        }
                    }
                }
            }

            if (lots.Count == 0)
                continue;

            decimal totalShares = lots.Sum(l => l.Quantity);
            decimal totalCost = lots.Sum(l => l.Quantity * l.Price);
            decimal avgPrice = totalShares > 0 ? totalCost / totalShares : 0;
            double weightedDays = totalShares > 0
                ? (double)lots.Sum(l => l.Quantity * (decimal)(today - l.Date).TotalDays) / (double)totalShares
                : 0;

            result.Add(new StockHoldingViewModel
            {
                Ticker = ticker,
                SharesHeld = totalShares,
                AverageBuyPrice = avgPrice,
                TotalCost = totalCost,
                AverageHoldingDays = weightedDays,
            });
        }

        return result;
    }
}
