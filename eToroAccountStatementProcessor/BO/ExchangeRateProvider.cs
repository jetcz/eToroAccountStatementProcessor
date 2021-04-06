using HtmlAgilityPack;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace eToroAccountStatementProcessor.BO
{
	public class ExchangeRateProvider
	{
		private readonly int Year;
		private readonly string CurrencyCode;

		public ExchangeRateProvider(int Year, string CurrencyCode)
		{
			this.Year = Year;
			this.CurrencyCode = CurrencyCode;
		}

		public async Task<decimal> GetExchangeRate()
		{
			string s = await DownloadPage();

			return ParseHtml(s);
		}

		private async Task<string> DownloadPage()
		{
			string url = $"https://www.kurzy.cz/kurzy-men/jednotny-kurz/{Year}/";
			HttpClient client = new HttpClient();
			using (HttpResponseMessage response = await client.GetAsync(url))
			using (HttpContent content = response.Content)
			{
				return await content.ReadAsStringAsync();
			}
		}

		private decimal ParseHtml(string s)
		{
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(s);

			var row = doc.DocumentNode.SelectSingleNode("//table[@class='pd pdw rca rowcl']") //this is the table with exchange rates
				.Descendants("tr")
				.Where(tr => tr.Elements("td").Any(td => td.InnerText.Contains(CurrencyCode.ToUpper())))
				.SingleOrDefault();

			var cells = row.Elements("td").ToArray();

			string rate = cells[4].InnerText;

			decimal.TryParse(rate.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d);

			return d;
		}
	}
}
