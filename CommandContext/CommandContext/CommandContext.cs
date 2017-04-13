using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
			typeof(CommandContextDefinition),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits)
		);

		public static void SetCommandContext(DependencyObject element, object commandContext)
		{
			element.SetValue(CommandContextProperty, commandContext);
		}

		public static object GetCommandContext(DependencyObject element)
		{
			return element.GetValue(CommandContextProperty);
		}

		public static T CommandContext<T>(this T element, object commandContext) where T : DependencyObject
		{
			element.SetValue(CommandContextProperty, commandContext);
			return element;
		}

		public static object CommandContext(this DependencyObject element)
		{
			return element.GetValue(CommandContextProperty);
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
			else if (targetPropertyType.Name == "RuntimeMethodInfo" && Path.EndsWith("()"))
			{
				var name = GetPropertyName(serviceProvider);
				var eventName = Regex.Match(name, "Add(.*)Handler").Groups[1].Value;

				var instance = GetPropertyInstance(serviceProvider);
				var methodName = string.Concat(Path.Reverse().Skip(2).Reverse());

				var @event = instance.GetType().GetEvent(eventName);
				return EventHandler(@event.EventHandlerType, () =>
				{
					var commandContext = CommandContextResolver(instance);
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
				});
			}
			else if (targetPropertyType.Name == "RuntimeEventInfo" && Path.EndsWith("()"))
			{
				var name = GetPropertyName(serviceProvider);
				var eventName = name;

				var instance = GetPropertyInstance(serviceProvider);
				var methodName = string.Concat(Path.Reverse().Skip(2).Reverse());

				var @event = instance.GetType().GetEvent(eventName);
				return EventHandler(@event.EventHandlerType, () =>
				{
					var commandContext = CommandContextResolver(instance);
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
				});
			}
			else
			{
				throw new NotSupportedException($"\"{Path}\" not suppoerted as {nameof(CommandBinding)}.");
			}
		}

		static Delegate EventHandler(Type type, Action action)
		{
			if (type == typeof(MouseButtonEventHandler)) return new MouseButtonEventHandler((s, e) => action());
			if (type == typeof(KeyEventHandler)) return new KeyEventHandler((s, e) => action());
			if (type == typeof(RoutedEventHandler)) return new RoutedEventHandler((s, e) => action());
			else throw new ArgumentException($"CommandBinding events is not yet supported for \"{type.Name}\"");
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

		string GetPropertyName(IServiceProvider serviceProvider)
		{
			var targetProvider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

			return ((MemberInfo)targetProvider.TargetProperty).Name;
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
