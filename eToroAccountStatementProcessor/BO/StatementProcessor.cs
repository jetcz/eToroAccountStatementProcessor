using eToroAccountStatementProcessor.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace eToroAccountStatementProcessor.BO
{
	public class StatementProcessor
	{
		public ProgressModel Progress { get; set; } = new ProgressModel() { Minimum = 0, Maximum = 100, Progress = 0 };

		private readonly List<string> Cryptos = new List<string>() {
			"Bitcoin", "Ethereum", "Bitcoin Cash", "Ripple", "Dash", "Litecoin", "Ethereum Classic", "Cardano", "IOTA", "Stellar", "EOS", "NEO", "TRON", "ZCASH", "Binance Coin", "Tezos"
		};

		private readonly List<string> CryptosCheckStrings = new List<string>();

		public StatementProcessor()
		{
			foreach (var item in Cryptos)
			{
				CryptosCheckStrings.Add($"Buy {item}");
				CryptosCheckStrings.Add($"Sell {item}");
			}
		}

		public IEnumerable<ClosedPositionRecord> Process(DataTable Data)
		{
			for (int i = 0; i < Data.Rows.Count; i++)
			{
				//System.Threading.Thread.Sleep(1);
				Progress.Progress = (int)Math.Ceiling(((i + 1) / (decimal)Data.Rows.Count) * 100);

				ClosedPositionRecord rec = new ClosedPositionRecord();

				DataRow dr = Data.Rows[i];

				if ((string)dr["Is Real"] == "CFD") //cfd must be taxed even if held over 3 yrs
				{
					rec.TradeType = PositionType.CFD;
					rec.IncludeToTaxReport = true;
				}
				else
				{
					var OpenDate = Convert.ToDateTime((string)dr["Open Date"]);
					var CloseDate = Convert.ToDateTime((string)dr["Close Date"]);
					TimeSpan span = CloseDate.Subtract(OpenDate);
					rec.IncludeToTaxReport = span.TotalSeconds < 94608000; //3 years					

					string Action = (string)dr["Action"];
					if (Action.Contains(CryptosCheckStrings))
					{
						rec.TradeType = PositionType.Crypto;
					}
					else
					{
						rec.TradeType = PositionType.Stock;
					}
				}

				rec.Profit = Convert.ToDecimal((string)dr["Profit"]);
				rec.Expense = Convert.ToDecimal((string)dr["Amount"]);
				rec.Commision = Convert.ToDecimal((string)dr["Spread"]);
				//rec.Dividend = Convert.ToDecimal((string)dr["Rollover Fees And Dividends"]);

				yield return rec;
			}
		}
	}
}
