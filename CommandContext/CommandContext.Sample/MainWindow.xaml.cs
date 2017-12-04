using System.Windows;
using CommandContext;

namespace CommandContext_Sample
{
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			this.DataContext = new ViewModel { Input = "World" };
			this.CommandContext(new Controller());
		}
	}


	public class Controller
	{
		public void SayHi() => MessageBox.Show($"Hi!");

		public void SayHiTo(object name) => MessageBox.Show($"Hi '{name}'!");
	}


	public class ViewModel
	{
		public string Input { get; set; }
	}
}
