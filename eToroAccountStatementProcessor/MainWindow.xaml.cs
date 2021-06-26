using eToroAccountStatementProcessor.BO;
using eToroAccountStatementProcessor.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace eToroAccountStatementProcessor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		StatementModel StatementModel;
		GlobalProgressModel Progress;

		public MainWindow()
		{
			InitializeComponent();
			ShowProgressBar(false);
			DownloadExchangeRate();
			Init();
		}

		private void Init()
		{
			StatementModel = new StatementModel();
			Progress = new GlobalProgressModel();
			prg.DataContext = Progress;
			SetCurrency();

			if (!StatementProcessor.Cryptos.Any())
			{
				MessageBox.Show($"Error: No config found or no crypto defined. Program will not be able to extract crypto trades.");
			}
		}

		private async void DownloadExchangeRate()
		{
			try
			{
				var ep = new ExchangeRateProvider(DateTime.Today.Year - 1, "USD");

				tbExchangeRate.Text = (await ep.GetExchangeRate()).ToString(CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to get last year exchange rate. Enter rate manually. {Environment.NewLine}{Environment.NewLine}{ex}");
			}
		}

		private void SetCurrency()
		{
			decimal.TryParse(tbExchangeRate.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ExchangeRate);

			StatementModel.ExchangeRate = ExchangeRate;
			StatementModel.UseLocalCurrency = rbLocalCurrency.IsChecked == true;
		}

		private void ProcessFileSelection(OpenFileDialog openFileDialog)
		{
			ShowProgressBar(true);

			List<Task> Tasks = new List<Task>();
			foreach (var filePath in openFileDialog.FileNames)
			{
				ExcelProcessor ep = new ExcelProcessor();
				StatementProcessor sp = new StatementProcessor();
				Progress.Add(ep.Progress);
				Progress.Add(sp.Progress);

				Task t = Task.Run(() =>
				{
					try
					{
						var data = ep.GetData(filePath);
						var statementData = sp.Process(data);

						Parallel.ForEach(statementData, item =>
						{
							StatementModel.RawData.Add(item);
						});					
					}
					catch (Exception ex)
					{
						Dispatcher.Invoke(() =>
						{
							MessageBox.Show(ex.ToString());
						});
					}
				});
				Tasks.Add(t);
			}

			Task.WhenAll(Tasks)
			   .ContinueWith(x =>
			   {
				   BindData();
				   ShowProgressBar(false);
			   });
		}

		private void ShowProgressBar(bool Visible)
		{
			Dispatcher.Invoke(() =>
			{
				prg.Visibility = Visible ? Visibility.Visible : Visibility.Hidden;
			});
		}

		private void BindData()
		{
			if (StatementModel.RawData.Count() == 0)
			{
				return;
			}

			Dispatcher.Invoke(() =>
			{
				pnlGrids.Visibility = Visibility.Visible;
				dgStatemetnSummary.ItemsSource = StatementModel.GetSummaryViewModel();
				dgTaxReport.ItemsSource = StatementModel.GetTaxReportModel();
			});
		}

		private void mnuOpen_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Multiselect = true,
				Filter = "Excel (*.xlsx)|*.xlsx"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				lblFileCount.Content = $"Files: {openFileDialog.FileNames.Count()}";

				Init();

				ProcessFileSelection(openFileDialog);
			}
		}

		private void mnuExit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Currency_Click(object sender, RoutedEventArgs e)
		{
			SetCurrency();
			BindData();
		}

		private void mnuAbout_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show($@"eToro Account Statement Processor

Tento nástroj automaticky vypočítá data pro vykázaní daně z příjmů fyzických osob na základě eToro account statementu. Jeho použití je na vlastní zodpovědnost, výstup má pouze informativní charakter a nelze jej použít při jednání s FÚ.

Funkce:
- Program si po spuštění automaticky stáhne loňský jednotný kurz USD z kurzy.cz. Tento je pak možné libovolně ručně přepsat.
- Je možné vybrat více souborů najednou, data budou automaticky agregována. Je nutné, aby všechny statementy byly zafiltrované na stejný rok.
- Automaticky jsou vyloučeny uzavřené pozice, které byly drženy déle než 3 roky (kromě CFD).
- Detekce kryptoměn: {string.Join(", ", StatementProcessor.Cryptos)}

Známé nedostatky:
- Dividendy nejsou nijak zohledňovány. Tyto jsou již zdaněny v zemi vzniku a podle platných smluv České republiky o zamezení dvojímu zdanění v oboru daní z příjmu, resp. z příjmu a z majetku, není třeba je danit znovu. Daňové přiznání by přesto mělo obsahovat úhrn dividend, ale z eToro account statementu není možné získat všechna potřebná data - země vzniku, částka před zdaněním v zemi v vzniku.
- Krypto staking rewards jsou vedeny jako běžný nákup kryptoměn. V případě uzavření této pozice je pak ve statementu veden náklad na tuto pozici ve výši, která se rovná hodnotě v USD při připsání této odměny. Tento náklad by měl být ale nulový.

Jiří Macháček
jiri.machacek87@gmail.com
");
		}
	}
}
