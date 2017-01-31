using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace CommandContext
{
	public class CommandContext
	{
	}

	public class CommandBinding : MarkupExtension
	{
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			throw new NotImplementedException();
		}
	}
}
