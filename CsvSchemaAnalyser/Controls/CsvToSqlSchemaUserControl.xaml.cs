using FastColoredTextBoxNS;
using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.CsvSchemaAnalyser.Controls
{
	/// <summary>
	/// Interaction logic for CsvToSqlSchemaUserControl.xaml
	/// </summary>
	public partial class CsvToSqlSchemaUserControl : UserControl
	{
		public CsvToSqlSchemaUserControl()
		{
			InitializeComponent();
			_OpenFileDialog = new OpenFileDialog();
			ControlsHelper.InitInvokeContext();
			_UpdateSkipper = new UpdateSkipper<ProgressEventArgs>();
			_UpdateSkipper.Control = this;
			_UpdateSkipper.Action = (e) => ProgressBarPanel.UpdateProgress(e);
			ProgressBarPanel.UpdateProgress();
		}

		UpdateSkipper<ProgressEventArgs> _UpdateSkipper;

		private void AnalyseValueButton_Click(object sender, RoutedEventArgs e)
		{
			MainTabControl.SelectedItem = LogTabItem;
			var name = "Value";
			var values = ValueTextBox.Text.Split(',');
			var s = "";
			try
			{
				var column = RuntimeHelper.DetectType(values);
				column.Name = name;
				s += "\r\n" + string.Join("\r\n", column.Log) + "\r\n\r\n";
				s += column.ToCSharpString();
			}
			catch (Exception ex)
			{
				AddResults(ResultsLogBox, true, "Failed: {0}\r\n", ex.Message);
				s += $"{ex}\r\n";
			}
			AddResults(ResultsLogBox, false, s);
		}

		private void AnalyseButton_Click(object sender, RoutedEventArgs e)
		{
			var path = GetNormalPath(FileTextBox.Text);
			var pea = new ProgressEventArgs
			{
				TopMessage = $"Analysing file: {path}",
				State = ProgressStatus.Started,
				TopIndex = 0,
				TopCount = 1,
				TopData = path,
				SubIndex = 0,
				SubCount = 0,
			};
			_UpdateSkipper.Update(pea);
			var task = Task.Factory.StartNew(() =>
			{
				var fi = new System.IO.FileInfo(path);
				var totalSize = fi.Length;
				var columns = new List<RuntimeHelper.DetectTypeItem>();
				var rows = 0;
				JocysCom.ClassLibrary.Files.CsvHelper.Read(path, (position, line, values) =>
				{
					if (line == 0)
					{
						pea.SubMessage = "Analysing columns...";
						pea.State = ProgressStatus.Updated;
						_UpdateSkipper.Update(pea);
						for (int i = 0; i < values.Length; i++)
						{
							var column = new RuntimeHelper.DetectTypeItem();
							column.Name = values[i];
							columns.Add(column);
						}
					}
					else
					{
						rows++;
						pea.SubMessage = $"Analysing row: {rows:#,##0}";
						pea.SubIndex = position;
						pea.SubCount = totalSize;
						pea.SubProgressText = $"{100m * position / totalSize:0}%";
						pea.State = ProgressStatus.Updated;
						_UpdateSkipper.Update(pea);
						for (int i = 0; i < columns.Count; i++)
						{
							var column = columns[i];
							RuntimeHelper.DetectType(ref column, values[i]);
						}
					}
					return true;
				});
				// Add C# declaration.
				var cs = string.Join("\r\n", columns.Select(x => x.ToCSharpString()));
				AddResults(ResultsCsBox, true, cs);
				// Add SQL Declaration.
				var sql = string.Join("\r\n", columns.Select(x => x.ToSqlString()));
				AddResults(ResultsSqlBox, true, sql);
				// Add logs.
				var log = "";
				var fileSize = JocysCom.ClassLibrary.IO.FileFinder.BytesToString(fi.Length);
				log += $"\r\nPath: {path}, Size: {fileSize}, Rows: {rows:#,##0}\r\n\r\n";
				for (int i = 0; i < columns.Count; i++)
				{
					var column = columns[i];
					log += "\r\n";
					log += string.Join("\r\n", column.Log);
					log += "\r\n\r\n";
					log += column.ToCSharpString() + "\r\n";
				}
				AddResults(ResultsLogBox, true, log);
				// Done.
				pea.TopMessage = $"File analysed: {path}";
				pea.TopIndex = 1;
				pea.SubMessage = $"Rows analysed: {rows:#,##0}";
				_UpdateSkipper.Update(pea);
			}, TaskCreationOptions.LongRunning)
			.ContinueWith((x) =>
			{
				AddResults(ResultsLogBox, true, $"{x.Exception}");
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		void AddResults(FastColoredTextBox control, bool clear, string format, params object[] args)
		{
			ControlsHelper.Invoke(() =>
			{
				if (clear)
					control.Clear();
				control.Text += args == null || args.Length == 0
						? format : string.Format(format, args);
			});
		}

		private OpenFileDialog _OpenFileDialog;

		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			BrowseCsvFile();
		}

		string GetNormalPath(string path)
		{
			path = AssemblyInfo.ExpandPath(path);
			path = PathHelper.ConvertFromSpecialFoldersPattern(path, "{", "}");
			return path;
		}

		string GetParametrizedPath(string path)
		{
			path = PathHelper.ConvertToSpecialFoldersPattern(path, "{", "}");
			path = AssemblyInfo.ParameterizePath(path);
			return path;
		}

		void BrowseCsvFile()
		{
			var path = GetNormalPath(FileTextBox.Text);
			var dialog = _OpenFileDialog;
			dialog.DefaultExt = "*.csv";
			dialog.Filter = "Data File (*.csv)|*.csv|All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			//if (string.IsNullOrEmpty(dialog.FileName))
			dialog.FileName = System.IO.Path.GetFileName(path);
			//if (string.IsNullOrEmpty(dialog.InitialDirectory))
			dialog.InitialDirectory = System.IO.Path.GetDirectoryName(path);
			dialog.Title = "Load CSV Data File";
			var result = dialog.ShowDialog();
			if (result == true)
				FileTextBox.Text = GetParametrizedPath(dialog.FileName);
		}

	}
}
