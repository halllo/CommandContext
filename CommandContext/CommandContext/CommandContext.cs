using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
			if (targetPropertyType == typeof(ICommand))
			{
				var instance = GetPropertyInstance(serviceProvider);
				return MatchMethodSignature(Path, (methodName, methodArguments) =>
					CreateCommand(instance, methodName, methodArguments));
			}
			else if (targetPropertyType.Name == "RuntimeMethodInfo")
			{
				var instance = GetPropertyInstance(serviceProvider);
				var eventName = Regex.Match(GetPropertyName(serviceProvider), "Add(.*)Handler").Groups[1].Value;
				var @event = instance.GetType().GetEvent(eventName);
				return MatchMethodSignature(Path, (methodName, methodArguments) =>
					CreateEventHandler(@event.EventHandlerType, () => MethodReflection.Invoke(instance, methodName, methodArguments)));
			}
			else if (targetPropertyType.Name == "RuntimeEventInfo")
			{
				var instance = GetPropertyInstance(serviceProvider);
				var eventName = GetPropertyName(serviceProvider);
				var @event = instance.GetType().GetEvent(eventName);
				return MatchMethodSignature(Path, (methodName, methodArguments) =>
					CreateEventHandler(@event.EventHandlerType, () => MethodReflection.Invoke(instance, methodName, methodArguments)));
			}
			else
			{
				throw new NotSupportedException($"\"{Path}\" not suppoerted as {nameof(CommandBinding)}.");
			}
		}

		public static ICommand CreateCommand(object instance, string path)
		{
			return MatchMethodSignature(path, (methodName, methodArguments) => CreateCommand(instance, methodName, methodArguments));
		}

		static ICommand CreateCommand(object instance, string methodName, string[] methodArguments)
		{
			return CommandConstructor(
				() => MethodReflection.Invoke(instance, methodName, methodArguments),
				() => MethodReflection.CanInvoke(instance, methodName, methodArguments)
			);
		}







		static class MethodReflection
		{
			public static void Invoke(object instance, string methodName, string[] methodArguments)
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
					InvokeWithParameters(method, instance, methodArguments, commandContext);
				}
			}

			public static bool CanInvoke(object instance, string methodName, string[] methodArguments)
			{
				var commandContext = CommandContextResolver(instance);
				if (commandContext != null)
				{
					var dataContextType = commandContext.GetType();
					var method2 = dataContextType.GetMethod("Can" + methodName, BindingFlags.Public | BindingFlags.Instance);
					if (method2 != null && method2.ReturnType == typeof(bool))
					{
						return (bool)InvokeWithParameters(method2, instance, methodArguments, commandContext);
					}
				}
				return true;
			}

			static object InvokeWithParameters(MethodInfo method, object instance, string[] methodArguments, object commandContext)
			{
				var methodParameters = method.GetParameters();
				var parameters = methodArguments.Select((a, index) =>
				{
					if (a == "this")
						return instance;
					else
					{
						var property = instance.GetType().GetProperty(a, BindingFlags.Public | BindingFlags.Instance);
						if (property != null)
							return property.GetValue(instance);
						else
							return Convert.ChangeType(a, methodParameters[index].ParameterType);
					}
				}).ToArray();
				try
				{
					return method.Invoke(commandContext, parameters);
				}
				catch (TargetInvocationException e)
				{
					ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
				}
				return null;
			}
		}





		static readonly Regex MethodSignatureRegex = new Regex("^(?<methodName>\\w*)\\((?<p0>\\w+)?(?:,\\s?(?<p1>\\w+))?(?:,\\s?(?<p2>\\w+))?(?:,\\s?(?<p3>\\w+))?\\)$", RegexOptions.Compiled);
		static TResult MatchMethodSignature<TResult>(string path, Func<string, string[], TResult> continuation)
		{
			var methodSignatureMatch = MethodSignatureRegex.Match(path);
			if (methodSignatureMatch.Success)
			{
				var methodName = methodSignatureMatch.Groups["methodName"].Value;
				var methodArguments = new[]
				{
					methodSignatureMatch.Groups["p0"].Value,
					methodSignatureMatch.Groups["p1"].Value,
					methodSignatureMatch.Groups["p2"].Value,
					methodSignatureMatch.Groups["p3"].Value,
				}.Where(g => !string.IsNullOrWhiteSpace(g)).ToArray();

				return continuation(methodName, methodArguments);
			}
			else
			{
				throw new NotSupportedException($"\"{path}\" not supported as {nameof(CommandBinding)}.");
			}
		}




		static Delegate CreateEventHandler(Type type, Action action)
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




		static Func<object, object> CommandContextResolver = element => (element as DependencyObject).CommandContext();
		public static Func<Action, Func<bool>, ICommand> CommandConstructor = (execute, canExecute) => new Command(execute, canExecute);
	}
}
