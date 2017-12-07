using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandContext.Tests
{
	[TestClass]
	public class InvokeWithParamters
	{
		[TestInitialize]
		public void Init()
		{
			CommandBinding.CommandContextResolver = e => e;
		}

		public class Control
		{
			public string DirectProperty1 { get; set; }
			public string DirectProperty2 { get; set; }
			public object DataContext { get; set; }
			public OverloadType1 AmbiguousProperty1 { get; set; }
			public OverloadType2 AmbiguousProperty2 { get; set; }

			public void Do() => Do_Argument = "<NULL>";
			public void Do(string s) => Do_Argument = s ?? "<NULL>";
			public void Do(string s1, string s2)
			{
				Do_Argument = s1;
				Do_Argument2 = s2;
			}
			public void DoAmbiguous(OverloadType1 t1) => Do_Argument = t1.ToString();
			public void DoAmbiguous(OverloadType2 t2) => Do_Argument = t2.ToString();

			public string Do_Argument;
			public string Do_Argument2;
		}

		public class OverloadType1 { }
		public class OverloadType2 { }
		public class ViewModel
		{
			public string Input { get; set; }
		}




		[TestMethod]
		public void NoParameter()
		{
			var control = new Control();

			var command = CommandBinding.CreateCommand(control, "Do()");
			command.Execute(null);

			Assert.AreEqual("<NULL>", control.Do_Argument);
		}

		[TestMethod]
		public void InvalidParameter()
		{
			var control = new Control();

			var command = CommandBinding.CreateCommand(control, "Do(Blabla)");
			command.Execute(null);

			Assert.AreEqual("Blabla", control.Do_Argument);
		}

		[TestMethod]
		public void DirectPropertyOfControlAsParameter()
		{
			var control = new Control
			{
				DirectProperty1 = "direct",
			};

			var command = CommandBinding.CreateCommand(control, "Do(DirectProperty1)");
			command.Execute(null);

			Assert.AreEqual("direct", control.Do_Argument);
		}

		[TestMethod]
		public void TwoDirectPropertiesOfControlAsParameters()
		{
			var control = new Control
			{
				DirectProperty1 = "direct1",
				DirectProperty2 = "direct2",
			};

			var command = CommandBinding.CreateCommand(control, "Do(DirectProperty2,DirectProperty1)");
			command.Execute(null);

			Assert.AreEqual("direct2", control.Do_Argument);
			Assert.AreEqual("direct1", control.Do_Argument2);
		}

		[TestMethod]
		public void IndirectPropertyOfControlAsParameter()
		{
			var control = new Control
			{
				DataContext = new ViewModel { Input = "nested" }
			};

			var command = CommandBinding.CreateCommand(control, "Do(DataContext.Input)");
			command.Execute(null);

			Assert.AreEqual("nested", control.Do_Argument);
		}

		[TestMethod]
		public void IndirectPropertyOfNullOfControlAsParameter()
		{
			var control = new Control
			{
				DataContext = null
			};

			var command = CommandBinding.CreateCommand(control, "Do(DataContext.Input)");
			command.Execute(null);

			Assert.AreEqual("<NULL>", control.Do_Argument);
		}

		[TestMethod]
		public void AmbiguousMethodnameOverloadsWithDifferentTypes()
		{
			var control = new Control
			{
				AmbiguousProperty1 = new OverloadType1(),
				AmbiguousProperty2 = new OverloadType2(),
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty2)");
			command.Execute(null);
			Assert.AreEqual(new OverloadType2().ToString(), control.Do_Argument);

			command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty1)");
			command.Execute(null);
			Assert.AreEqual(new OverloadType1().ToString(), control.Do_Argument);
		}
	}
}
