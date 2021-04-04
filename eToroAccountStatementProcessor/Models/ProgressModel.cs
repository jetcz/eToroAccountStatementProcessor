using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace eToroAccountStatementProcessor.Models
{
	public class ProgressModel : INotifyPropertyChanged
	{
		public int Minimum { get; set; }
		public int Maximum { get; set; }

		private int _progress;
		public int Progress
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

	public class GlobalProgressModel : INotifyPropertyChanged
	{
		public int Minimum { get { return Progresses.Sum(x => x.Minimum); } }
		public int Maximum { get { return Progresses.Sum(x => x.Maximum); } }
		public int Progress { get { return Progresses.Sum(x => x.Progress); } }

		private List<ProgressModel> Progresses { get; set; } = new List<ProgressModel>();

		public void Add(ProgressModel ProgressModel)
		{
			ProgressModel.PropertyChanged += ProgressModel_PropertyChanged;

			Progresses.Add(ProgressModel);

			OnPropertyChanged(nameof(Minimum));
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
