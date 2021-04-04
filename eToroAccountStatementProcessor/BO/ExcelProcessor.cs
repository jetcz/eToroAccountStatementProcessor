using eToroAccountStatementProcessor.Models;
using ExcelDataReader;
using System;
using System.Data;
using System.IO;

namespace eToroAccountStatementProcessor.BO
{

	public class ExcelProcessor
	{
		public ProgressModel Progress { get; set; } = new ProgressModel() { Minimum = 0, Maximum = 100, Progress = 0 };

		public DataTable GetData(string filePath)
		{
			using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
			using (var reader = ExcelReaderFactory.CreateReader(stream))
			{
				var result = reader.AsDataSet(new ExcelDataSetConfiguration()
				{
					FilterSheet = (tableReader, sheetIndex) => { return sheetIndex == 1; }, //take the second sheet with the closed positions

					ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
					{
						UseHeaderRow = true,
						FilterRow = (rowReader) =>
						{
							Progress.Progress = (int)Math.Ceiling(rowReader.Depth / (decimal)rowReader.RowCount * 100);
							// progress is in the range 0..100			

							//System.Threading.Thread.Sleep(1);

							return true;
						}
					},
				});

				return result.Tables[0];

			}
		}
	}
}
