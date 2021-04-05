using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace eToroAccountStatementProcessor.Models
{
	public class StatementModel
	{
		public ConcurrentBag<ClosedPositionRecord> RawData { get; set; } = new ConcurrentBag<ClosedPositionRecord>(); //instead of list - we need thread safe "addrange" because we are adding data in parallel to this collection
		public bool UseLocalCurrency { get; set; }

		private decimal _ExchangeRate;
		public decimal ExchangeRate { get => UseLocalCurrency ? _ExchangeRate : 1; set => _ExchangeRate = value; }

		private IEnumerable<ClosedPositionViewRecord> GetAggregatedData(bool IncludeAll)
		{
			return RawData.Where(x => IncludeAll || x.IncludeToTaxReport).GroupBy(x => x.TradeType).Select(
				x => new ClosedPositionViewRecord()
				{
					TradeType = x.Key,
					Revenue = Math.Round(x.Sum(y => y.Revenue) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Expense = Math.Round(x.Sum(y => y.Expense) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Profit = Math.Round(x.Sum(y => y.Profit) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Commision = Math.Round(x.Sum(y => y.Commision) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					//Dividend = Math.Round(x.Sum(y => y.Dividend) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
				}).OrderBy(x => x.TradeType); ;
		}

		public IEnumerable<ClosedPositionViewRecord> GetSummaryViewModel()
		{
			var grouped = GetAggregatedData(true);

			var sums = new ClosedPositionViewRecord()
			{
				TradeType = PositionType.Sum, //not trade type
				Revenue = grouped.Sum(s => s.Revenue),
				Expense = grouped.Sum(s => s.Expense),
				Profit = grouped.Sum(s => s.Profit),
				Commision = grouped.Sum(s => s.Commision),
				//Dividend = grouped.Sum(s => s.Dividend),
			};

			return grouped.Append(sums);
		}
		public IEnumerable<TaxReportRecord> GetTaxReportModel()
		{
			var grouped = GetAggregatedData(false);

			foreach (var item in grouped)
			{
				TaxReportRecord rec = new TaxReportRecord
				{
					Expense = Math.Round(item.Expense, 0, MidpointRounding.AwayFromZero),
					Revenue = Math.Round(item.Revenue, 0, MidpointRounding.AwayFromZero)
				};

				switch (item.TradeType)
				{
					case PositionType.Stock:
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
		Stock,
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
	}

	public class TaxReportRecord
	{
		public string RevenueType { get; set; }
		public string Description { get; set; }
		public decimal Revenue { get; set; }
		public decimal Expense { get; set; }
		public decimal Profit { get { return Revenue - Expense; } }
	}
}
