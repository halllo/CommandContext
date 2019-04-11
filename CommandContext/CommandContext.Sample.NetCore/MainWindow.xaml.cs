using System.Windows;

namespace CommandContext.Sample.NetCore
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var viewModel = this.DataContext = new ViewModel
			{
				Input = "World",
				SubViewModel1 = new ViewModel { Input = "item1" },
				SubViewModel2 = new ViewModel { Input = "item2" },
			};
			this.CommandContext(new Controller { ViewModel = (ViewModel)viewModel });
		}
	}


	public class Controller
	{
		public ViewModel ViewModel { get; set; }

		public bool CanSayHi() => ViewModel.Enabled;
		public void SayHi() => MessageBox.Show($"Hi!");

		public void SayHiTo(object name) => MessageBox.Show($"Hi '{name}'!");
	}


	public class ViewModel
	{
		public bool Enabled { get; set; }
		public string Input { get; set; }

		public ViewModel SubViewModel1 { get; set; }
		public ViewModel SubViewModel2 { get; set; }
	}
}
