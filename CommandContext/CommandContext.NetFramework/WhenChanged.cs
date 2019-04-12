using System;
using System.Dynamic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CommandContext.Extensions
{
	public class WhenChanged : MarkupExtension
	{
		public Func<DependencyObject, EventHandler<DataTransferEventArgs>> Invoke { get; set; }

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var targetProvider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
			var element = (FrameworkElement)targetProvider.TargetObject;
			var property = (DependencyProperty)targetProvider.TargetProperty;
			element.SetBinding(property, new Binding("Ignored") { Source = new IgnoringViewModel(), NotifyOnSourceUpdated = true });
			Binding.AddSourceUpdatedHandler(element, Invoke(element));
			return null;
		}

		private class IgnoringViewModel : DynamicObject { }
	}
}
