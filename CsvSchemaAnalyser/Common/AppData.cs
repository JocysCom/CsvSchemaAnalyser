using JocysCom.ClassLibrary.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.CsvSchemaAnalyser
{
	public class AppData : ISettingsItem, INotifyPropertyChanged
	{
		public AppData()
		{
		}

		public bool Enabled { get; set; }

		public bool IsEmpty =>
			string.IsNullOrEmpty(CsvFile) &&
			string.IsNullOrEmpty(CsvValue);

		public string CsvFile { get => _CsvFile ?? @"{CommonApplicationData}\{Company}\{Product}\File.csv"; set => SetProperty(ref _CsvFile, value); }
		private string _CsvFile;

		public string CsvValue { get => _CsvValue ?? @"2022-12-17 18:45:59"; set => SetProperty(ref _CsvValue, value); }
		private string _CsvValue;

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
		{
			property = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
