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
		StatementModel DataModel;
		GlobalProgress Progress;

		public MainWindow()
		{
			InitializeComponent();
			ShowProgressBar(false);
			DownloadExchangeRate();
			Init();
		}

		private void Init()
		{
			DataModel = new StatementModel();
			Progress = new GlobalProgress();
			prg.DataContext = Progress;
			SetExchangeRate();
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
				MessageBox.Show($@"Failed to get last year exchange rate{Environment.NewLine}{Environment.NewLine}{ex}");
			}
		}

		private void SetExchangeRate()
		{
			decimal.TryParse(tbExchangeRate.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d);
			DataModel.ExchangeRate = d;
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
						DataModel.RawData.AddRange(sp.Process(data));
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
				   ProcessResult();
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

		private void ProcessResult()
		{
			if (DataModel.RawData.Count() == 0)
			{
				return;
			}

			Dispatcher.Invoke(() =>
			{
				dgResult.ItemsSource = DataModel.GetViewData();
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

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			SetExchangeRate();
			ProcessResult();
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
