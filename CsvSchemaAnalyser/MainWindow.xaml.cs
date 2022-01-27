using JocysCom.ClassLibrary.Controls;
using System;
using System.Reflection;
using System.Windows;

namespace JocysCom.CsvSchemaAnalyser
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			ControlsHelper.InitInvokeContext();
			Global.AppData.Load();
			if (Global.AppData.Items.Count == 0)
			{
				Global.AppData.Items.Add(new AppData());
				Global.AppData.Save();
			}
			InitializeComponent();
			LoadHelpAndInfo();
		}

		void LoadHelpAndInfo()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var ai = new ClassLibrary.Configuration.AssemblyInfo();
			Title = ai.GetTitle(true, false, true, false, false);
		}

		public InfoControl HMan;

		public static bool IsClosing;

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			IsClosing = true;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Global.AppData.Save();
		}

	}

}
