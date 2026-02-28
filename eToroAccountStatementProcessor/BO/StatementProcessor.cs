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
		private const int iType = 19;
	
		private const int iOpenDate = 5;
		private const int iClosedDate = 6;
		private const int iProfit = 10;
		private const int iAmount = 3;
		private const int iSpread = 9;

		public ProgressModel Progress { get; set; } = new ProgressModel() { Minimum = 0, Maximum = 100, Progress = 0 };
		public CultureInfo Culture { get; private set; }

		public IEnumerable<ClosedPositionRecord> Process(DataTable Data)
		{
			//resolve culture of the data
			string date = (string)Data.Rows[0][iOpenDate];

			CultureInfo[] cultures = { new CultureInfo("cs-CZ"), CultureInfo.InvariantCulture, new CultureInfo("en-US") };

			foreach (var c in cultures)
			{
				if (DateTime.TryParse(date, c, DateTimeStyles.None, out _))
				{
					Culture = c;
					break;
				}
			}

			if (Culture is null)
			{
				throw new Exception("Could not determine culture of the excel file. Contact support and attach the excel file.");
			}

			//process the data
			for (int i = 0; i < Data.Rows.Count; i++)
			{
				Progress.Progress = (int)Math.Ceiling(((i + 1) / (decimal)Data.Rows.Count) * 100);

				ClosedPositionRecord rec = new ClosedPositionRecord();

				DataRow dr = Data.Rows[i];

				string type = (string)dr[iType];

				var OpenDate = Convert.ToDateTime((string)dr[iOpenDate], Culture);
				var CloseDate = Convert.ToDateTime((string)dr[iClosedDate], Culture);
				TimeSpan span = CloseDate.Subtract(OpenDate);

				switch (type)
				{
					case "CFD":
						rec.TradeType = PositionType.CFD;
						rec.IncludeToTaxReport = true;
						break;
					case "Crypto":
						rec.TradeType = PositionType.Crypto;
						rec.IncludeToTaxReport = true;
						rec.IncludeToTaxReport = span.TotalSeconds < 94608000; //3 years	
						break;
					case "Stocks":
                    case "ETF":
                        rec.TradeType = PositionType.StockAndETF;
						rec.IncludeToTaxReport = span.TotalSeconds < 94608000; //3 years	
						break;

                    default:
						break;
				}

				//these are double internally
				rec.Profit = Convert.ToDecimal(dr[iProfit]);
				rec.Expense = Convert.ToDecimal(dr[iAmount]);
				rec.Commision = Convert.ToDecimal(((string)dr[iSpread]).Replace('.',',')); //idk why spread is stored as string with dot as decimal separator

				yield return rec;
			}
		}
	}
}