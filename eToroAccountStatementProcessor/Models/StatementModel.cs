using System;
using System.Collections.Generic;
using System.Linq;

namespace eToroAccountStatementProcessor.Models
{
	public class StatementModel
	{
		public List<ClosedPositionRecord> RawData { get; set; } = new List<ClosedPositionRecord>();

		private decimal _ExchangeRate;
		public bool UseLocalCurrency { get; set; }
		public decimal ExchangeRate { get => UseLocalCurrency ? _ExchangeRate : 1; set => _ExchangeRate = value; }

		public List<ClosedPositionViewRecord> GetViewModel()
		{
			var grouped = RawData.GroupBy(x => x.TradeType).Select(
				x => new ClosedPositionViewRecord()
				{
					TradeType = x.Key,
					Revenue = Math.Round(x.Sum(y => y.Revenue) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Expense = Math.Round(x.Sum(y => y.Expense) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Profit = Math.Round(x.Sum(y => y.Profit) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Commision = Math.Round(x.Sum(y => y.Commision) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
					Dividend = Math.Round(x.Sum(y => y.Dividend) * ExchangeRate, 2, MidpointRounding.AwayFromZero),
				});

			var sums = new ClosedPositionViewRecord()
			{
				TradeType = PositionType.Sum, //not trade type
				Revenue = grouped.Sum(s => s.Revenue),
				Expense = grouped.Sum(s => s.Expense),
				Profit = grouped.Sum(s => s.Profit),
				Commision = grouped.Sum(s => s.Commision),
				Dividend = grouped.Sum(s => s.Dividend),
			};

			var complete = grouped.ToList();
			complete.Add(sums);

			return complete;
		}
	}

	public class ClosedPositionRecord
	{
		public PositionType TradeType { get; set; }
		public decimal Revenue { get { return Expense + Profit + Dividend - Commision; } }
		public decimal Expense { get; set; }
		public decimal Profit { get; set; }
		public decimal Commision { get; set; }
		public decimal Dividend { get; set; }
	}

	public class ClosedPositionViewRecord : ClosedPositionRecord
	{
		public new decimal Revenue { get; set; }
	}

	public enum PositionType
	{
		Sum,
		Stock,
		CFD,
		Crypto,
	}
}
