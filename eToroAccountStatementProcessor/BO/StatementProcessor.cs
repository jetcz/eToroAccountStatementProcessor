using eToroAccountStatementProcessor.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;

namespace eToroAccountStatementProcessor.BO
{
	public class StatementProcessor
	{
		private const int iType = 15;
		private const int iAction = 1;
		private const int iOpenDate = 4;
		private const int iClosedDate = 5;
		private const int iProfit = 8;
		private const int iAmount = 2;
		private const int iSpread = 7;

		public ProgressModel Progress { get; set; } = new ProgressModel() { Minimum = 0, Maximum = 100, Progress = 0 };

		public static string[] Cryptos
		{
			get
			{
				return ConfigurationManager.AppSettings.Get("Cryptos")?.Split('|') ?? new string[0];
			}
		}


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

			string date = (string)Data.Rows[0][iOpenDate];

			CultureInfo[] cultures = { new CultureInfo("cs-CZ"), CultureInfo.InvariantCulture, new CultureInfo("en-US") };

			foreach (var c in cultures)
			{
				if (DateTime.TryParse(date, c, DateTimeStyles.None, out _))
				{
					culture = c;
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

				bool IsCFD = (string)dr[iType] == "CFD";
				if (IsCFD) //cfd must be taxed even if held over 3 yrs
				{
					rec.TradeType = PositionType.CFD;
					rec.IncludeToTaxReport = true;
				}
				else
				{
					bool IsCrypto = ((string)dr[iAction]).IsIn(CryptosCheckStrings);
					if (IsCrypto) //crypto must be taxed even if held over 3 yrs
					{
						rec.TradeType = PositionType.Crypto;
						rec.IncludeToTaxReport = true;
					}
					else //stock
					{
						var OpenDate = Convert.ToDateTime((string)dr[iOpenDate], culture);
						var CloseDate = Convert.ToDateTime((string)dr[iClosedDate], culture);
						TimeSpan span = CloseDate.Subtract(OpenDate);

						rec.TradeType = PositionType.Stock;
						rec.IncludeToTaxReport = span.TotalSeconds < 94608000; //3 years	
					}
				}

				//these are double internally
				rec.Profit = Convert.ToDecimal(dr[iProfit]);
				rec.Expense = Convert.ToDecimal(dr[iAmount]);
				rec.Commision = Convert.ToDecimal(dr[iSpread]);

				yield return rec;
			}
		}
	}
}