using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eToroAccountStatementProcessor
{
	public class StatementModel
	{
		public List<StatementRowData> RawData { get; set; } = new List<StatementRowData>();
		public decimal ExchangeRate { get; set; }

		public List<StatementViewData> GetViewData()
		{
			var data = RawData.GroupBy(x => x.TradeType).Select(
				g => new StatementViewData()
				{
					Type = g.Key.ToString(),
					Revenue_USD = g.Sum(s => s.Revenue),
					Expense_USD = g.Sum(s => s.Expense),
					Profit_USD = g.Sum(s => s.Profit),
					Revenue_LOC = Math.Round(g.Sum(s => s.Revenue) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Expense_LOC = Math.Round(g.Sum(s => s.Expense) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Profit_LOC = Math.Round(g.Sum(s => s.Profit) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
				});

			var sum = new StatementViewData()
			{

				Type = "Sum",
				Revenue_USD = data.Sum(s => s.Revenue_USD),
				Expense_USD = data.Sum(s => s.Expense_USD),
				Profit_USD = data.Sum(s => s.Profit_USD),
				Revenue_LOC = data.Sum(s => s.Revenue_LOC),
				Expense_LOC = data.Sum(s => s.Expense_LOC),
				Profit_LOC = data.Sum(s => s.Profit_LOC),
			};

			var list = data.ToList();
			list.Add(sum);

			return list;
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
