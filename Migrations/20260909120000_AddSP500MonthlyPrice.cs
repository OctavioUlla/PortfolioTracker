using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSP500MonthlyPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SP500MonthlyPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP500MonthlyPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SP500MonthlyPrices_Year_Month",
                table: "SP500MonthlyPrices",
                columns: new[] { "Year", "Month" },
                unique: true);

            // Backfill: for every month that already has a balance, take the S&P 500 price
            // from the most recent cash transaction that carried one on or before that
            // month's end. Months predating the first priced transaction are skipped rather
            // than seeded with 0, which would silently value the benchmark at nothing.
            // Dates are stored as TEXT ("yyyy-MM-dd HH:mm:ss.FFFFFFF"), so comparisons go
            // through julianday() instead of relying on the exact literal shape.
            migrationBuilder.Sql(@"
                WITH months(y, m) AS (SELECT DISTINCT ""Year"", ""Month"" FROM ""MonthlyBalances""),
                priced AS (
                    SELECT months.y AS y, months.m AS m, (
                        SELECT t.""SP500Price"" FROM ""CashTransactions"" t
                        WHERE t.""SP500Price"" > 0
                          AND julianday(t.""Date"") < julianday(printf('%04d-%02d-01', months.y, months.m), '+1 month')
                        ORDER BY julianday(t.""Date"") DESC, t.""Id"" DESC LIMIT 1
                    ) AS price FROM months
                )
                INSERT OR IGNORE INTO ""SP500MonthlyPrices"" (""Year"", ""Month"", ""Price"")
                SELECT y, m, price FROM priced WHERE price IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SP500MonthlyPrices");
        }
    }
}
