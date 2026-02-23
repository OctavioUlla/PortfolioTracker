using ClosedXML.Excel;
using PortfolioTracker.Models;

namespace PortfolioTracker.Services;

public class ExcelImportService
{
    private static readonly string OrangeRowColor = "FFFF9900";

    public ImportPreviewViewModel ParseExcelFile(Stream stream, int brokerId)
    {
        var result = new ImportPreviewViewModel { BrokerId = brokerId };

        using var workbook = new XLWorkbook(stream);

        IXLWorksheet ws;
        try
        {
            ws = workbook.Worksheet("Registro");
        }
        catch
        {
            result.Warnings.Add("Sheet 'Registro' not found. Please upload the correct Excel file.");
            return result;
        }

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = ws.Row(rowNum);

            var aCell = row.Cell(1);  // A: Date
            var eCell = row.Cell(5);  // E: Inversiones (ticker or APORTE)
            var fCell = row.Cell(6);  // F: Compra/Venta (quantity)
            var gCell = row.Cell(7);  // G: Precio (price)
            var iCell = row.Cell(9);  // I: Aporte (deposit amount)
            var kCell = row.Cell(11); // K: Observación (notes)
            var mCell = row.Cell(13); // M: Total (monthly portfolio balance)
            var oCell = row.Cell(15); // O: Precio S&P 500

            // Detect month-end summary rows by their orange background color
            var aBgColor = GetBackgroundColor(aCell);
            bool isMonthEndRow = aBgColor == OrangeRowColor;

            if (isMonthEndRow)
            {
                // Monthly balance row
                if (!aCell.TryGetValue(out DateTime balanceDate)) continue;
                if (!TryGetDecimal(mCell, out decimal balance) || balance <= 0) continue;

                result.MonthlyBalances.Add(new MonthlyBalance
                {
                    Year = balanceDate.Year,
                    Month = balanceDate.Month,
                    Balance = balance,
                    BrokerId = brokerId
                });
            }
            else
            {
                var eValue = eCell.GetString().Trim();
                if (string.IsNullOrEmpty(eValue)) continue;

                if (eValue == "APORTE")
                {
                    // Cash transaction (deposit or withdrawal)
                    if (!aCell.TryGetValue(out DateTime txDate)) continue;
                    if (!TryGetDecimal(iCell, out decimal amount) || amount == 0) continue;

                    TryGetDecimal(oCell, out decimal sp500Price);

                    result.CashTransactions.Add(new CashTransaction
                    {
                        Date = txDate,
                        Type = amount > 0 ? TransactionType.Deposit : TransactionType.Withdrawal,
                        Amount = Math.Abs(amount),
                        SP500Price = sp500Price,
                        Notes = kCell.GetString().Trim().NullIfEmpty(),
                        BrokerId = brokerId
                    });
                }
                else if (TryGetDecimal(fCell, out decimal quantity) && quantity != 0
                         && TryGetDecimal(gCell, out decimal price) && price > 0)
                {
                    // Stock trade (buy if quantity > 0, sell if quantity < 0)
                    if (!aCell.TryGetValue(out DateTime tradeDate))
                    {
                        // Some rows may inherit the date from the row above; skip if missing
                        result.Warnings.Add($"Row {rowNum}: Missing date for trade '{eValue}', skipped.");
                        continue;
                    }

                    result.StockTrades.Add(new StockTrade
                    {
                        Date = tradeDate,
                        Type = quantity > 0 ? TradeType.Buy : TradeType.Sell,
                        Ticker = eValue,
                        Quantity = Math.Abs(quantity),
                        Price = price,
                        Commission = 0,
                        Notes = kCell.GetString().Trim().NullIfEmpty(),
                        BrokerId = brokerId
                    });
                }
                // else: account balance rows (BALANZ, CRIPTO, etc.) — ignored as per spec
            }
        }

        return result;
    }

    private static string GetBackgroundColor(IXLCell cell)
    {
        var fill = cell.Style.Fill;
        if (fill.PatternType == XLFillPatternValues.Solid)
        {
            var color = fill.BackgroundColor.Color;
            return $"{(uint)color.ToArgb():X8}";
        }
        return string.Empty;
    }

    private static bool TryGetDecimal(IXLCell cell, out decimal value)
    {
        value = 0;
        if (cell.IsEmpty()) return false;

        // Try direct numeric value first
        if (cell.TryGetValue(out double d))
        {
            value = (decimal)d;
            return true;
        }

        // Try parsing string representation
        var str = cell.GetString().Trim();
        return decimal.TryParse(str, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
