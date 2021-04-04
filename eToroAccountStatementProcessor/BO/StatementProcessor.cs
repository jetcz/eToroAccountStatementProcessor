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

		public List<ClosedPositionRecord> Process(DataTable Data)
		{
			List<ClosedPositionRecord> retval = new List<ClosedPositionRecord>();

			for (int i = 0; i < Data.Rows.Count; i++)
			{
				//System.Threading.Thread.Sleep(1);
				Progress.Progress = (int)Math.Ceiling(((i + 1) / (decimal)Data.Rows.Count) * 100);

				ClosedPositionRecord row = new ClosedPositionRecord();

				DataRow dr = Data.Rows[i];

				if ((string)dr["Is Real"] == "CFD") //cfd must be taxed even if held over 3 yrs
				{
					row.TradeType = PositionType.CFD;
				}
				else
				{
					var OpenDate = Convert.ToDateTime((string)dr["Open Date"]);
					var CloseDate = Convert.ToDateTime((string)dr["Close Date"]);
					TimeSpan span = CloseDate.Subtract(OpenDate);
					bool IsOld = span.Seconds > 94608000; //3 years

					if (IsOld)
					{
						continue;
					}

					string Action = (string)dr["Action"];
					if (Action.Contains(CryptosCheckStrings))
					{
						row.TradeType = PositionType.Crypto;
					}
					else
					{
						row.TradeType = PositionType.Stock;
					}
				}

				row.Profit = Convert.ToDecimal((string)dr["Profit"]);
				row.Expense = Convert.ToDecimal((string)dr["Amount"]);
				row.Commision = Convert.ToDecimal((string)dr["Spread"]);
				row.Dividend = Convert.ToDecimal((string)dr["Rollover Fees And Dividends"]);

				retval.Add(row);
			}

			return retval;

		}
	}
}
