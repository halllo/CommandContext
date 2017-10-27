using System.Windows;
using CommandContext;

namespace CommandContext_Sample
{
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			this.DataContext = new ViewModel { Input = "beispielhafte Eingabe" };
			this.CommandContext(new Controller { Name = "Controller" });
			this.TheInnerButton.CommandContext(new Controller { Name = "Inner Controller" });
		}
	}


	public class Controller
	{
		public string Name { get; set; }

		public void SayHi() => MessageBox.Show($"Hi {Name}");

		public void SayHiTo(string parameter, object tag) => MessageBox.Show($"Hi {parameter}! (Tag: {tag})");

		public void SayHiOutsideOrInside(bool t) => MessageBox.Show($"Hi {(t ? "inside" : "outside")}");
	}

	public class ViewModel
	{
		public string Input { get; set; }
	}
}
