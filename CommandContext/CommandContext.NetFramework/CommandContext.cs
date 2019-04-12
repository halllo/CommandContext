using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
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
					CreateCommand(instance, methodName, methodArguments, contextParameters: null));
			}
			else if (targetPropertyType.Name == "RuntimeMethodInfo")
			{
				var instance = GetPropertyInstance(serviceProvider);
				var propertyMemberInfo = GetPropertyMemberInfo(serviceProvider);
				var eventName = Regex.Match(propertyMemberInfo.Name, "Add(.*)Handler").Groups[1].Value;

				Type eventHandlerType;
				var @event = instance.GetType().GetEvent(eventName);
				if (@event != null)
				{
					eventHandlerType = @event.EventHandlerType;
				}
				else
				{
					var routedEvent = EventManager.GetRoutedEventsForOwner(propertyMemberInfo.DeclaringType).Single(e => e.Name == eventName);
					eventHandlerType = routedEvent.HandlerType;
				}
				return MatchMethodSignature(Path, (methodName, methodArguments) =>
					CreateEventHandler(eventHandlerType, (contextParameters) => MethodReflection.Invoke(instance, methodName, methodArguments, contextParameters)));
			}
			else if (targetPropertyType.Name == "RuntimeEventInfo")
			{
				var instance = GetPropertyInstance(serviceProvider);
				var eventName = GetPropertyMemberInfo(serviceProvider).Name;
				var @event = instance.GetType().GetEvent(eventName);
				return MatchMethodSignature(Path, (methodName, methodArguments) =>
					CreateEventHandler(@event.EventHandlerType, (contextParameters) => MethodReflection.Invoke(instance, methodName, methodArguments, contextParameters)));
			}
			else if (targetPropertyType.Name == "RuntimePropertyInfo")
			{
				var instance = GetPropertyInstance(serviceProvider);
				var propertyMemberInfo = GetPropertyMemberInfo(serviceProvider);
				var propertyType = propertyMemberInfo.GetType().GetProperty("PropertyType")?.GetValue(propertyMemberInfo) as Type;
				if (propertyType == null) return null;
				else if (typeof(ICommand).IsAssignableFrom(propertyType))
				{
					return MatchMethodSignature(Path, (methodName, methodArguments) =>
						CreateCommand(instance, methodName, methodArguments, contextParameters: null));
				}
				else if (typeof(Func<DependencyObject, object>).IsAssignableFrom(propertyType))
				{
					var commandType = propertyType.GenericTypeArguments[1];
					if (typeof(ICommand).IsAssignableFrom(commandType))
					{
						return ConvertFactory(returnType: commandType, factory: remoteInstance =>
							MatchMethodSignature(Path, (methodName, methodArguments) =>
								CreateCommand(remoteInstance, methodName, methodArguments, contextParameters: null)));
					}
					else
					{
						return ConvertFactory(returnType: commandType, factory: remoteInstance =>
							MatchMethodSignature(Path, (methodName, methodArguments) =>
								CreateEventHandler(commandType, (contextParameters) => MethodReflection.Invoke(remoteInstance, methodName, methodArguments, contextParameters))));
					}
				}
				else
				{
					return MatchMethodSignature(Path, (methodName, methodArguments) =>
						CreateEventHandler(propertyType, (contextParameters) => MethodReflection.Invoke(instance, methodName, methodArguments, contextParameters)));
				}
			}
			else
			{
				throw new NotSupportedException($"\"{Path}\" not supported as {nameof(CommandBinding)}.");
			}
		}

		public static ICommand CreateCommand(object instance, string path, Dictionary<string, object> contextParameters = null)
		{
			return MatchMethodSignature(path, (methodName, methodArguments) => CreateCommand(instance, methodName, methodArguments, contextParameters));
		}

		static ICommand CreateCommand(object instance, string methodName, string[] methodArguments, Dictionary<string, object> contextParameters)
		{
			return CommandConstructor(
				() => MethodReflection.Invoke(instance, methodName, methodArguments, contextParameters),
				() => MethodReflection.CanInvoke(instance, methodName, methodArguments, contextParameters)
			);
		}







		static class MethodReflection
		{
			public static void Invoke(object instance, string methodName, string[] methodArguments, Dictionary<string, object> contextParameters)
			{
				var commandContext = CommandContextResolver(instance);
				if (commandContext == null)
				{
					throw new NotSupportedException($"\"{instance.GetType().Name}\" has no CommandContext.");
				}
				var parameters = ResolveParameters(instance, methodArguments, contextParameters);
				var dataContextType = commandContext.GetType();
				var methods = dataContextType
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.Where(m => m.Name == methodName && m.GetParameters().Length == parameters.Length)
					.ToArray();
				var method = FindBestMethod(methods, parameters);
				if (method == null)
				{
					throw new NotSupportedException($"\"{methodName}\" not found on \"{dataContextType.Name}\"");
				}
				else
				{
					InvokeWithParameters(method, parameters, commandContext);
				}
			}

			public static bool CanInvoke(object instance, string methodName, string[] methodArguments, Dictionary<string, object> contextParameters)
			{
				var commandContext = CommandContextResolver(instance);
				if (commandContext != null)
				{
					var parameters = ResolveParameters(instance, methodArguments, contextParameters);
					var dataContextType = commandContext.GetType();
					var methods = dataContextType
						.GetMethods(BindingFlags.Public | BindingFlags.Instance)
						.Where(m => m.Name == "Can" + methodName && m.GetParameters().Length == parameters.Length && m.ReturnType == typeof(bool))
						.ToArray();
					var method = FindBestMethod(methods, parameters);
					if (method != null)
					{
						return (bool)InvokeWithParameters(method, parameters, commandContext);
					}
				}
				return true;
			}

			static ResolvedParameter[] ResolveParameters(object instance, string[] methodArguments, Dictionary<string, object> contextParameters)
			{
				var parameters = methodArguments.Select((a, index) =>
				{
					if (a == "this")
					{
						return ResolvedParameter.New(instance, instance.GetType(), a);
					}
					else
					{
						var splitted = a.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
						var result = instance;

						if (contextParameters != null)
						{
							var firstSplit = splitted.FirstOrDefault();
							if (firstSplit != null && contextParameters.ContainsKey(firstSplit))
							{
								result = contextParameters[firstSplit];
								splitted = splitted.Skip(1).ToArray();
							}
						}

						var resultType = result.GetType();

						foreach (var split in splitted)
						{
							if (result != null)
							{
								var property = resultType.GetProperty(split, BindingFlags.Public | BindingFlags.Instance);
								if (property != null)
								{
									result = property.GetValue(result);
									resultType = result?.GetType() ?? property.PropertyType;
								}
								else
								{
									return ResolvedParameter.Unresolvable(a);
								}
							}
							else
							{
								var property = resultType.GetProperty(split, BindingFlags.Public | BindingFlags.Instance);
								if (property != null)
								{
									resultType = property.PropertyType;
								}
								else
								{
									resultType = null;
									break;
								}
							}
						}
						return ResolvedParameter.New(result, resultType, a);
					}
				}).ToArray();
				return parameters;
			}
			class ResolvedParameter
			{
				public static ResolvedParameter Unresolvable(string descriptor) => new ResolvedParameter { Descriptor = descriptor, Resolvable = false };
				public static ResolvedParameter New(object value, Type type, string descriptor) => new ResolvedParameter { Descriptor = descriptor, Value = value, Type = type, Resolvable = true };
				private ResolvedParameter() { }
				public bool Resolvable { get; private set; }
				public object Value { get; private set; }
				public Type Type { get; private set; }
				public string Descriptor { get; private set; }
			}

			static MethodInfo FindBestMethod(MethodInfo[] methods, ResolvedParameter[] resolvedParameters)
			{
				if (methods.Length == 0) return null;
				else if (methods.Length == 1) return methods[0];
				else
				{
					foreach (var method in methods)
					{
						var parameterTypeMatches = Enumerable.Zip(
							first: method.GetParameters().Select(p => p.ParameterType),
							second: resolvedParameters,
							resultSelector: (methodParameterType, resolvedParameter) =>
							{
								if (!resolvedParameter.Resolvable)
								{
									try
									{
										var result = Convert.ChangeType(resolvedParameter.Descriptor, methodParameterType);
										return true;
									}
									catch (Exception)
									{
										return false;
									}
								}
								else if (resolvedParameter.Type == null)
								{
									return !methodParameterType.IsValueType;//try to cast it later
								}
								else if (resolvedParameter.Value == null)
								{
									return methodParameterType.IsAssignableFrom(resolvedParameter.Type);
								}
								else
								{
									return methodParameterType.IsInstanceOfType(resolvedParameter.Value);
								}
							});
						if (parameterTypeMatches.All(m => m))
						{
							return method;
						}
					}
					return null;
				}
			}

			static object InvokeWithParameters(MethodInfo method, ResolvedParameter[] resolvedParameters, object commandContext)
			{
				var parameters = Enumerable
					.Zip(
						first: resolvedParameters,
						second: method.GetParameters(),
						resultSelector: (resolvedParameter, actualParameter) => resolvedParameter.Resolvable ? resolvedParameter.Value : Convert.ChangeType(resolvedParameter.Descriptor, actualParameter.ParameterType))
					.ToArray();
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





		static readonly Regex MethodSignatureRegex = new Regex("^(?<methodName>\\w*)\\((?<p0>[\\.\\w]+)?(?:,\\s?(?<p1>[\\.\\w]+))?(?:,\\s?(?<p2>[\\.\\w]+))?(?:,\\s?(?<p3>[\\.\\w]+))?\\)$", RegexOptions.Compiled);
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




		static Func<DependencyObject, T> ConvertFactoryGeneric<T>(Func<DependencyObject, object> factory) => dpo => (T)factory(dpo);
		static readonly MethodInfo FactoryConverter = typeof(CommandBinding).GetMethod(nameof(ConvertFactoryGeneric), BindingFlags.NonPublic | BindingFlags.Static);
		static object ConvertFactory(Type returnType, Func<DependencyObject, object> factory) => FactoryConverter.MakeGenericMethod(returnType).Invoke(null, new object[] { factory });

		static Delegate CreateEventHandler(Type type, Action<Dictionary<string, object>> action)
		{
			if (type == typeof(MouseButtonEventHandler)) return new MouseButtonEventHandler((s, e) => action(new Dictionary<string, object> { { "sender", s }, { "e", e } }));
			if (type == typeof(KeyEventHandler)) return new KeyEventHandler((s, e) => action(new Dictionary<string, object> { { "sender", s }, { "e", e } }));
			if (type == typeof(RoutedEventHandler)) return new RoutedEventHandler((s, e) => action(new Dictionary<string, object> { { "sender", s }, { "e", e } }));
			if (type == typeof(EventHandler<DataTransferEventArgs>)) return new EventHandler<DataTransferEventArgs>((s, e) => action(new Dictionary<string, object> { { "sender", s }, { "e", e } }));
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

		MemberInfo GetPropertyMemberInfo(IServiceProvider serviceProvider)
		{
			var targetProvider = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
			return (MemberInfo)targetProvider.TargetProperty;
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




		internal static Func<object, object> CommandContextResolver = element => (element as DependencyObject).CommandContext();
		public static Func<Action, Func<bool>, ICommand> CommandConstructor = (execute, canExecute) => new Command(execute, canExecute);
	}
}
