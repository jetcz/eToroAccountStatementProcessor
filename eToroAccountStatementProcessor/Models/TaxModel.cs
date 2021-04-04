using System;
using System.Collections.Generic;
using System.Linq;

namespace eToroAccountStatementProcessor.Models
{
	public class TaxModel 
	{
		public List<TaxReportRecord> GetTaxReportModel(List<ClosedPositionViewRecord> StatementViewModel)
		{
			List<TaxReportRecord> retval = new List<TaxReportRecord>();

			foreach (var item in StatementViewModel.Where(x => x.TradeType != PositionType.Sum))
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

				retval.Add(rec);
			}

			return retval;
		}
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
