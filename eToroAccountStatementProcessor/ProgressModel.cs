using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace eToroAccountStatementProcessor
{
	public class ProgressModel : INotifyPropertyChanged
	{
		public double Minimum { get; set; }
		public double Maximum { get; set; }


		private double _progress;
		public double Progress
		{
			get { return _progress; }
			set
			{
				if (_progress != value)
				{
					_progress = value;
					OnPropertyChanged(nameof(Progress)); 
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}

	public class GlobalProgress : INotifyPropertyChanged
	{
		public double Minimum { get { return 0; } }
		public double Maximum { get { return Progresses.Sum(x => x.Maximum); } }
		public double Progress { get { return Progresses.Sum(x => x.Progress); } }

		private List<ProgressModel> Progresses { get; set; } = new List<ProgressModel>();

		public void Add(ProgressModel ProgressModel)
		{
			ProgressModel.PropertyChanged += ProgressModel_PropertyChanged;

			Progresses.Add(ProgressModel);
	
			OnPropertyChanged(nameof(Maximum));
		}

		private void ProgressModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(Progress));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string info)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}
	}
}
