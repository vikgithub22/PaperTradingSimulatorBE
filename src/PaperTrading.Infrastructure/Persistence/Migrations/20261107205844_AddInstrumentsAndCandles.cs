using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaperTrading.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentsAndCandles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "NSE"),
                    TickSize = table.Column<decimal>(type: "decimal(4,2)", nullable: false, defaultValue: 0.05m),
                    LotSize = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PreviousClose = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Candles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    OpenTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candles", x => x.Id);
                    table.CheckConstraint("CK_Candles_High_Gte_Close", "[High] >= [Close]");
                    table.CheckConstraint("CK_Candles_High_Gte_Low", "[High] >= [Low]");
                    table.CheckConstraint("CK_Candles_High_Gte_Open", "[High] >= [Open]");
                    table.CheckConstraint("CK_Candles_Low_Lte_Close", "[Low] <= [Close]");
                    table.CheckConstraint("CK_Candles_Low_Lte_Open", "[Low] <= [Open]");
                    table.CheckConstraint("CK_Candles_Low_Positive", "[Low] > 0");
                    table.CheckConstraint("CK_Candles_Volume_NonNegative", "[Volume] >= 0");
                    table.ForeignKey(
                        name: "FK_Candles_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Instruments",
                columns: new[] { "Id", "Exchange", "IsActive", "LotSize", "Name", "PreviousClose", "Sector", "Symbol", "TickSize" },
                values: new object[,]
                {
                    { 1, "NSE", true, 1, "Reliance Industries Ltd", 2450.00m, "Energy", "RELIANCE", 0.05m },
                    { 2, "NSE", true, 1, "Tata Consultancy Services Ltd", 3500.00m, "Technology", "TCS", 0.05m },
                    { 3, "NSE", true, 1, "HDFC Bank Ltd", 1650.00m, "Financial Services", "HDFCBANK", 0.05m },
                    { 4, "NSE", true, 1, "ICICI Bank Ltd", 1100.00m, "Financial Services", "ICICIBANK", 0.05m },
                    { 5, "NSE", true, 1, "Infosys Ltd", 1550.00m, "Technology", "INFY", 0.05m },
                    { 6, "NSE", true, 1, "Bharti Airtel Ltd", 1300.00m, "Telecommunication", "BHARTIARTL", 0.05m },
                    { 7, "NSE", true, 1, "ITC Ltd", 440.00m, "Consumer Goods", "ITC", 0.05m },
                    { 8, "NSE", true, 1, "State Bank of India", 750.00m, "Financial Services", "SBIN", 0.05m },
                    { 9, "NSE", true, 1, "Larsen & Toubro Ltd", 3600.00m, "Construction", "LT", 0.05m },
                    { 10, "NSE", true, 1, "Hindustan Unilever Ltd", 2400.00m, "Consumer Goods", "HINDUNILVR", 0.05m },
                    { 11, "NSE", true, 1, "Kotak Mahindra Bank Ltd", 1750.00m, "Financial Services", "KOTAKBANK", 0.05m },
                    { 12, "NSE", true, 1, "Axis Bank Ltd", 1150.00m, "Financial Services", "AXISBANK", 0.05m },
                    { 13, "NSE", true, 1, "Bajaj Finance Ltd", 6800.00m, "Financial Services", "BAJFINANCE", 0.05m },
                    { 14, "NSE", true, 1, "Maruti Suzuki India Ltd", 12200.00m, "Automobile", "MARUTI", 0.05m },
                    { 15, "NSE", true, 1, "Sun Pharmaceutical Industries Ltd", 1600.00m, "Healthcare", "SUNPHARMA", 0.05m },
                    { 16, "NSE", true, 1, "Titan Company Ltd", 3400.00m, "Consumer Durables", "TITAN", 0.05m },
                    { 17, "NSE", true, 1, "Asian Paints Ltd", 2850.00m, "Consumer Goods", "ASIANPAINT", 0.05m },
                    { 18, "NSE", true, 1, "Wipro Ltd", 480.00m, "Technology", "WIPRO", 0.05m },
                    { 19, "NSE", true, 1, "NTPC Ltd", 380.00m, "Utilities", "NTPC", 0.05m },
                    { 20, "NSE", true, 1, "Oil and Natural Gas Corporation Ltd", 260.00m, "Energy", "ONGC", 0.05m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_InstrumentId_OpenTimeUtc",
                table: "Candles",
                columns: new[] { "InstrumentId", "OpenTimeUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Symbol",
                table: "Instruments",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candles");

            migrationBuilder.DropTable(
                name: "Instruments");
        }
    }
}
