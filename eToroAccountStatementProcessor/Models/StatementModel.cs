using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace eToroAccountStatementProcessor.Models
{
    public class StatementModel
    {
        public ConcurrentBag<ClosedPositionRecord> RawData { get; set; } = new ConcurrentBag<ClosedPositionRecord>(); //instead of list - we need thread safe "addrange" because we are adding data in parallel to this collection
        public bool UseLocalCurrency { get; set; }
        public CultureInfo Culture { get; set; }

        private decimal _ExchangeRate;
        public decimal ExchangeRate { get => UseLocalCurrency ? _ExchangeRate : 1; set => _ExchangeRate = value; }

        private IEnumerable<ClosedPositionViewRecord> GetAggregatedData(bool IncludeAll)
        {
            var culture = Culture ?? CultureInfo.InvariantCulture;
            return RawData.Where(x => IncludeAll || x.IncludeToTaxReport).GroupBy(x => x.TradeType).Select(
                x =>
                {
                    var revenue = Math.Round(x.Sum(y => y.Revenue) * ExchangeRate, 2, MidpointRounding.AwayFromZero);
                    var expense = Math.Round(x.Sum(y => y.Expense) * ExchangeRate, 2, MidpointRounding.AwayFromZero);
                    var profit = Math.Round(x.Sum(y => y.Profit) * ExchangeRate, 2, MidpointRounding.AwayFromZero);
                    var commision = Math.Round(x.Sum(y => y.Commision) * ExchangeRate, 2, MidpointRounding.AwayFromZero);

                    return new ClosedPositionViewRecord()
                    {
                        TradeType = x.Key,
                        Revenue = revenue,
                        Expense = expense,
                        Profit = profit,
                        Commision = commision,
                        FormattedRevenue = revenue.ToString("N2", culture),
                        FormattedExpense = expense.ToString("N2", culture),
                        FormattedProfit = profit.ToString("N2", culture),
                        FormattedCommision = commision.ToString("N2", culture),
                        //Dividend = Math.Round(x.Sum(y => y.Dividend) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
                    };
                }).OrderBy(x => x.TradeType); ;
        }

        public IEnumerable<ClosedPositionViewRecord> GetSummaryViewModel()
        {
            var grouped = GetAggregatedData(true);
            var culture = Culture ?? CultureInfo.InvariantCulture;

            var sumRevenue = grouped.Sum(s => s.Revenue);
            var sumExpense = grouped.Sum(s => s.Expense);
            var sumProfit = grouped.Sum(s => s.Profit);
            var sumCommision = grouped.Sum(s => s.Commision);

            var sums = new ClosedPositionViewRecord()
            {
                TradeType = PositionType.Sum, //not trade type
                Revenue = sumRevenue,
                Expense = sumExpense,
                Profit = sumProfit,
                Commision = sumCommision,
                FormattedRevenue = sumRevenue.ToString("N2", culture),
                FormattedExpense = sumExpense.ToString("N2", culture),
                FormattedProfit = sumProfit.ToString("N2", culture),
                FormattedCommision = sumCommision.ToString("N2", culture),
                //Dividend = grouped.Sum(s => s.Dividend),
            };

            return grouped.Append(sums);
        }
        public IEnumerable<TaxReportRecord> GetTaxReportModel()
        {
            var grouped = GetAggregatedData(false);
            var culture = Culture ?? CultureInfo.InvariantCulture;

            foreach (var item in grouped)
            {
                var revenue = Math.Round(item.Revenue, 0, MidpointRounding.AwayFromZero);
                var expense = Math.Round(item.Expense, 0, MidpointRounding.AwayFromZero);
                var profit = revenue - expense;

                TaxReportRecord rec = new TaxReportRecord
                {
                    Expense = expense,
                    Revenue = revenue,
                    FormattedRevenue = revenue.ToString("N0", culture),
                    FormattedExpense = expense.ToString("N0", culture),
                    FormattedProfit = profit.ToString("N0", culture)
                };

                switch (item.TradeType)
                {
                    case PositionType.StockAndETF:
                        rec.RevenueType = "D - prodej cenných papírů";
                        rec.Description = "Obchodování s akciemi v zahraničí";
                        break;
                    case PositionType.CFD:
                        rec.RevenueType = "F - jiné ostatní příjmy";
                        rec.Description = "Obchodování s deriváty v zahraničí";
                        break;
                    case PositionType.Crypto:
                        rec.RevenueType = "C - prodej movitých věcí";
                        rec.Description = "Obchodování s kryptoměnami";
                        break;
                }

                yield return rec;
            }
        }
    }

    public enum PositionType
    {
        Sum, //not a position type

        Crypto,
        StockAndETF,
        CFD,
    
    }

    public class ClosedPositionRecord
    {
        public PositionType TradeType { get; set; }
        public decimal Revenue { get { return Expense + Profit; } }
        public decimal Expense { get; set; }
        public decimal Profit { get; set; }
        public decimal Commision { get; set; } //already included in revenue and expense
                                               //public decimal Dividend { get; set; } //already taxed by etoro
        public bool IncludeToTaxReport { get; set; } //true for positions held under 3 years or cfd
    }

    public class ClosedPositionViewRecord : ClosedPositionRecord
    {
        public new decimal Revenue { get; set; }
        public string FormattedRevenue { get; set; }
        public string FormattedExpense { get; set; }
        public string FormattedProfit { get; set; }
        public string FormattedCommision { get; set; }
    }

    public class TaxReportRecord
    {
        public string RevenueType { get; set; }
        public string Description { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public decimal Profit { get { return Revenue - Expense; } }
        public string FormattedRevenue { get; set; }
        public string FormattedExpense { get; set; }
        public string FormattedProfit { get; set; }
    }
}
