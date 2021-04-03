using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
		}

		private void Init()
		{
			DataModel = new StatementModel();
			Progress = new GlobalProgress();
			prg.DataContext = Progress;
			SetExchangeRate();
		}

		public void SetExchangeRate()
		{
			decimal.TryParse(tbExchangeRate.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d);
			DataModel.ExchangeRate = d;
		}

		private void ProcessFileSelection(OpenFileDialog openFileDialog)
		{
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
			Dispatcher.Invoke(() =>
			{
				dgResult.ItemsSource = DataModel.GetViewData();
			});
		}

		private void mnuOpen_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Multiselect = true;
			openFileDialog.Filter = "Excel (*.xlsx)|*.xlsx";
			openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (openFileDialog.ShowDialog() == true)
			{
				Init();

				lblFileCount.Content = $"Files: {openFileDialog.FileNames.Count()}";

				ShowProgressBar(true);

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

This tools calculates tax data from eToro account statement. Make sure that the statement(s) cover single year only.
It supports selecting of multiple files at once. It automatically skips trades opened earlier than 3 years ago (except CFD).

Author:
Jiří Macháček
jiri.machacek87@gmail.com
");
		}
	}
}
