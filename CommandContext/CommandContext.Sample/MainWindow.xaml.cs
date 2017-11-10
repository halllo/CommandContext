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
			this.CommandContext(new Controller((ViewModel)DataContext) { Name = "Controller" });
			this.TheInnerButton.CommandContext(new Controller(null) { Name = "Inner Controller" });
		}
	}


	public class Controller
	{
		private readonly ViewModel _vm;

		public Controller(ViewModel vm)
		{
			_vm = vm;
		}

		public string Name { get; set; }

		public void SayHi() => MessageBox.Show($"Hi {Name}");

		public void SayHiTo(string parameter, object tag) => MessageBox.Show($"Hi {parameter}! (Tag: {tag})");

		public void SayHiOutsideOrInside(bool t) => MessageBox.Show($"Hi {(t ? "inside" : "outside")}");

		public bool CanShowIfEnabled(int i) => _vm.IsEnabled;
		public void ShowIfEnabled(int i) => MessageBox.Show("i is " + i);
	}

	public class ViewModel
	{
		public string Input { get; set; }
		public bool IsEnabled { get; set; }
	}
}
