using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace CommandContext
{
	public static class CommandContextDefinition
	{
		public static readonly DependencyProperty CommandContextProperty = DependencyProperty.RegisterAttached(
			"CommandContext",
			typeof(object),
			typeof(DependencyObject),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits)
		);
	}

	public static class CommandContextAccess
	{
		public static T CommandContext<T>(this T element, object commandContext) where T : DependencyObject
		{
			element.SetValue(CommandContextDefinition.CommandContextProperty, commandContext);
			return element;
		}

		public static object CommandContext(this DependencyObject element)
		{
			return element.GetValue(CommandContextDefinition.CommandContextProperty);
		}
	}

	public class CommandBinding : MarkupExtension
	{
		public CommandBinding() { }
		public CommandBinding(string path) { Path = path; }

		public string Path { get; set; }

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var targetPropertyType = GetPropertyType(serviceProvider);
			if (targetPropertyType == typeof(ICommand) && Path.EndsWith("()"))
			{
				var instance = GetPropertyInstance(serviceProvider);
				var methodName = string.Concat(Path.Reverse().Skip(2).Reverse());
				return CommandConstructor(() =>
				{
					var commandContext = CommandContextResolver(instance);
					if (commandContext == null)
					{
						throw new NotSupportedException($"\"{instance.GetType().Name}\" has no CommandContext.");
					}
					var dataContextType = commandContext.GetType();
					var method = dataContextType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
					if (method == null)
					{
						throw new NotSupportedException($"\"{methodName}\" not found on \"{dataContextType.Name}\"");
					}
					else
					{
						method.Invoke(commandContext, null);
					}
				},
				() =>
				{
					var commandContext = CommandContextResolver(instance);
					if (commandContext != null)
					{
						var dataContextType = commandContext.GetType();
						var method2 = dataContextType.GetMethod("Can" + methodName, BindingFlags.Public | BindingFlags.Instance);
						if (method2 != null && method2.ReturnType == typeof(bool))
						{
							return (bool)method2.Invoke(commandContext, null);
						}
					}
					return true;
				});
			}
			else
			{
				throw new NotSupportedException($"\"{Path}\" not suppoerted as {nameof(CommandBinding)}.");
			}
		}

		Type GetPropertyType(IServiceProvider serviceProvider)
		{
			var targetProvider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

			if (targetProvider.TargetProperty is DependencyProperty)
			{
				return ((DependencyProperty)targetProvider.TargetProperty).PropertyType;
			}

			return targetProvider.TargetProperty.GetType();
		}

		object GetPropertyInstance(IServiceProvider serviceProvider)
		{
			var targetProvider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

			if (targetProvider.TargetProperty is DependencyProperty)
			{
				return targetProvider.TargetObject;
			}

			return targetProvider.TargetObject;
		}

		private static Func<object, object> CommandContextResolver = element => (element as DependencyObject).CommandContext();
		public static Func<Action, Func<bool>, ICommand> CommandConstructor = (execute, canExecute) => new Command(execute, canExecute);
	}
}
