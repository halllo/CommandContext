using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandContext.Tests
{
	[TestClass]
	public class InvokeWithParameters
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
			public Control Nested { get; set; }
			public object DirectObject { get; set; }

			public void Do() => Do_Argument = "<NULL>";
			public void Do(string s) => Do_Argument = s ?? "<NULL>";
			public void Do(string s1, string s2)
			{
				Do_Argument = s1;
				Do_Argument2 = s2;
			}
			public void DoObject(object o) => Do_Argument = o?.ToString() ?? "<NULL>";
			public void DoObjects(object o1, object o2)
			{
				Do_Argument = o1?.ToString() ?? "<NULL>";
				Do_Argument2 = o2?.ToString() ?? "<NULL>";
			}

			public void DoAmbiguous(OverloadType1 t1) => Do_Argument = t1?.ToString() ?? "<NULL1>";
			public void DoAmbiguous(OverloadType2 t2) => Do_Argument = t2?.ToString() ?? "<NULL2>";
			public void DoAmbiguous(OverloadType1 t1, int i1) => Do_Argument = (t1?.ToString() ?? "<NULL1>") + "_" + i1;
			public void DoAmbiguous(OverloadType2 t2, int i2) => Do_Argument = (t2?.ToString() ?? "<NULL2>") + "_" + i2;

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
		public void DirectPropertyOfControlAsObjectParameter()
		{
			var control = new Control
			{
				AmbiguousProperty2 = new OverloadType2(),
			};

			var command = CommandBinding.CreateCommand(control, "DoObject(AmbiguousProperty2)");
			command.Execute(null);

			Assert.AreEqual(new OverloadType2().ToString(), control.Do_Argument);
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
		public void IndirectPropertySecondDegreeOfNullOfControlAsParameter()
		{
			var control = new Control
			{
				DataContext = null
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(DataContext.Nested.AmbiguousProperty1)");
			command.Execute(null);

			Assert.AreEqual("<NULL1>", control.Do_Argument);
		}

		[TestMethod]
		public void OverloadsWithDifferentTypesAndSameTypeArgument()
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

		[TestMethod]
		public void OverloadsWithDifferentTypesButObjectArgument()
		{
			var control = new Control
			{
				DirectObject = new OverloadType2(),
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(DirectObject)");
			command.Execute(null);
			Assert.AreEqual(new OverloadType2().ToString(), control.Do_Argument);
		}

		[TestMethod]
		public void OverloadsWithDifferentTypesButNull()
		{
			var control = new Control
			{
				AmbiguousProperty1 = null,
				AmbiguousProperty2 = null,
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty2)");
			command.Execute(null);
			Assert.AreEqual("<NULL2>", control.Do_Argument);

			command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty1)");
			command.Execute(null);
			Assert.AreEqual("<NULL1>".ToString(), control.Do_Argument);
		}

		[TestMethod]
		public void OverloadsWithIndirectParameterOfDifferentTypesButNull()
		{
			var control = new Control
			{
				Nested = null
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(Nested.AmbiguousProperty2)");
			command.Execute(null);
			Assert.AreEqual("<NULL2>", control.Do_Argument);

			command = CommandBinding.CreateCommand(control, "DoAmbiguous(Nested.AmbiguousProperty1)");
			command.Execute(null);
			Assert.AreEqual("<NULL1>".ToString(), control.Do_Argument);
		}

		[TestMethod]
		public void OverloadsWithDifferentTypesAndInteger()
		{
			var control = new Control
			{
				AmbiguousProperty1 = new OverloadType1(),
				AmbiguousProperty2 = new OverloadType2(),
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty2, 123)");
			command.Execute(null);
			Assert.AreEqual(new OverloadType2().ToString() + "_123", control.Do_Argument);

			command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty1, 321)");
			command.Execute(null);
			Assert.AreEqual(new OverloadType1().ToString() + "_321", control.Do_Argument);
		}

		[TestMethod]
		public void OverloadsWithDifferentTypesButNullAndInteger()
		{
			var control = new Control
			{
				AmbiguousProperty1 = null,
				AmbiguousProperty2 = null,
			};

			var command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty2, 123)");
			command.Execute(null);
			Assert.AreEqual("<NULL2>_123", control.Do_Argument);

			command = CommandBinding.CreateCommand(control, "DoAmbiguous(AmbiguousProperty1, 321)");
			command.Execute(null);
			Assert.AreEqual("<NULL1>_321", control.Do_Argument);
		}

		[TestMethod]
		public void ContextParameter()
		{
			var control = new Control();

			var command = CommandBinding.CreateCommand(control, "DoObjects(sender,sender.Input)", new Dictionary<string, object> { { "sender", new ViewModel { Input = "blabla" } } });
			command.Execute(null);

			Assert.AreEqual(new ViewModel().ToString(), control.Do_Argument);
			Assert.AreEqual("blabla", control.Do_Argument2);
		}
	}
}
