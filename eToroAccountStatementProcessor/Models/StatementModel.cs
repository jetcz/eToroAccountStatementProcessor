using System;
using System.Collections.Generic;
using System.Linq;

namespace eToroAccountStatementProcessor.Models
{
	public class StatementModel
	{
		public List<StatementRowData> RawData { get; set; } = new List<StatementRowData>();
		public decimal ExchangeRate { get; set; }

		public List<StatementViewData> GetViewData()
		{
			var summed = RawData.GroupBy(x => x.TradeType).Select(
				x => new
				{
					Type = x.Key,
					Revenue = x.Sum(s => s.Revenue),
					Expense = x.Sum(s => s.Expense),
					Profit = x.Sum(s => s.Profit),
				});

			var localized = summed.Select(
				x => new StatementViewData()
				{
					Type = x.Type.ToString(),
					Revenue_USD = x.Revenue,
					Expense_USD = x.Expense,
					Profit_USD = x.Profit,
					Revenue_LOC = Math.Round(x.Revenue * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Expense_LOC = Math.Round(x.Expense * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Profit_LOC = Math.Round(x.Profit * ExchangeRate, 2, MidpointRounding.AwayFromZero),
				});

			var sums = new StatementViewData()
			{
				Type = "Sum",
				Revenue_USD = localized.Sum(s => s.Revenue_USD),
				Expense_USD = localized.Sum(s => s.Expense_USD),
				Profit_USD = localized.Sum(s => s.Profit_USD),
				Revenue_LOC = localized.Sum(s => s.Revenue_LOC),
				Expense_LOC = localized.Sum(s => s.Expense_LOC),
				Profit_LOC = localized.Sum(s => s.Profit_LOC),
			};

			var complete = localized.ToList();
			complete.Add(sums);

			return complete;
		}
	}

	public class StatementRowData
	{
		public TradeType TradeType { get; set; }
		public decimal Revenue { get { return Expense + Profit; } }
		public decimal Expense { get; set; }
		public decimal Profit { get; set; }
	}

	public enum TradeType
	{
		Stock,
		CFD,
		Crypto,
	}

	public class StatementViewData
	{
		public string Type { get; set; }
		public decimal Revenue_USD { get; set; }
		public decimal Expense_USD { get; set; }
		public decimal Profit_USD { get; set; }
		public decimal Revenue_LOC { get; set; }
		public decimal Expense_LOC { get; set; }
		public decimal Profit_LOC { get; set; }
	}
}
