using eToroAccountStatementProcessor.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace eToroAccountStatementProcessor.BO
{
	public class StatementProcessor
	{
		public ProgressModel Progress { get; set; } = new ProgressModel() { Minimum = 0, Maximum = 100, Progress = 0 };

		public static readonly List<string> Cryptos = new List<string>() {
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
			//resolve culture of the data
			CultureInfo culture = null;
		
			string date = (string)Data.Rows[0]["Open Date"];

			CultureInfo[] cultures = { new CultureInfo("cs-CZ"), CultureInfo.InvariantCulture, new CultureInfo("en-US") };

			foreach (var c in cultures)
			{
				if (DateTime.TryParse(date, c, DateTimeStyles.None, out _))
				{
					culture = c; //this unfortunately does not mean that the decimal point in the excel is also of this culture, hence we replace "," with "." manually
					break;
				}
			}

			if (culture is null)
			{
				throw new Exception("Could not determine culture of the excel file. Contact support and attach the excel file.");
			} 

			//process the data
			for (int i = 0; i < Data.Rows.Count; i++)
			{
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
					var OpenDate = Convert.ToDateTime((string)dr["Open Date"], culture);
					var CloseDate = Convert.ToDateTime((string)dr["Close Date"], culture);
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

				rec.Profit = Convert.ToDecimal(((string)dr["Profit"]).Replace(',','.'), CultureInfo.InvariantCulture);
				rec.Expense = Convert.ToDecimal(((string)dr["Amount"]).Replace(',', '.'), CultureInfo.InvariantCulture);
				rec.Commision = Convert.ToDecimal(((string)dr["Spread"]).Replace(',', '.'), CultureInfo.InvariantCulture);
		
				yield return rec;
			}
		}
	}
}