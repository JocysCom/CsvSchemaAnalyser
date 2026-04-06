using JocysCom.ClassLibrary.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.CsvSchemaAnalyser
{
	public class AppData : SettingsItem, INotifyPropertyChanged
	{
		public AppData()
		{
		}

		public bool Enabled { get; set; }

		public override bool IsEmpty =>
			string.IsNullOrEmpty(CsvFile) &&
			string.IsNullOrEmpty(CsvValue);

		public string CsvFile { get => _CsvFile ?? @"{CommonApplicationData}\{Company}\{Product}\File.csv"; set => SetProperty(ref _CsvFile, value); }
		private string _CsvFile;

		public string CsvValue { get => _CsvValue ?? @"2022-12-17 18:45:59"; set => SetProperty(ref _CsvValue, value); }
		private string _CsvValue;
	
	}
}
