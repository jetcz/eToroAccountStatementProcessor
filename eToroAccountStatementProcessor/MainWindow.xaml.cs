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
				MessageBox.Show($"Failed to get last year exchange rate{Environment.NewLine}{Environment.NewLine}{ex}");
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
						StatementModel.RawData.AddRange(sp.Process(data));
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

			var statementModel = StatementModel.GetViewModel();
			var taxModel = new TaxModel().GetTaxReportModel(statementModel);

			Dispatcher.Invoke(() =>
			{
				pnlGrids.Visibility = Visibility.Visible;
				dgResult.ItemsSource = statementModel;
				dgTaxReport.ItemsSource = taxModel;
			});
		}

		private void mnuOpen_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Multiselect = true,
				Filter = "Excel (*.xlsx)|*.xlsx",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
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
			MessageBox.Show(@"eToro Account Statement Processor

This tools calculates tax data from eToro account statement.
It supports selecting of multiple files at once.  Make sure that the statement(s) cover single year only. It automatically skips trades opened earlier than 3 years ago (except CFD).

Jiří Macháček
jiri.machacek87@gmail.com
");
		}
	}
}
