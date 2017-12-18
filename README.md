CommandContext
==============
DataContext and CommandContext allow you to separate data and commands into two dedicated view models.

[![Build status](https://ci.appveyor.com/api/projects/status/gj9tnljhh31arkbc?svg=true)](https://ci.appveyor.com/project/halllo/CommandContext)
[![Version](https://img.shields.io/nuget/v/CommandContext.svg)](https://www.nuget.org/packages/CommandContext)

To use regular WPF DataBinding you set a ViewModel as the DataContext of a FrameworkElement and bind to its properties with {Binding PropertyName} in XAML. In order to invoke methods you can have properties of type ICommand in the ViewModel and bind them to ICommand-Properties of FrameworkElements like Button. But you have to instantiate the ICommand and assign it the method it should invoke. This makes ViewModels ugly because they need to setup the command infrastructure. In other scenarios the command method is not even in the ViewModel but in a controller.

CommandContext is a second context next to the DataContext. While DataContext can be used for DataBinding, CommandContext allows you to bind methods to commands. It behaves just like DataContext and its methods can be bound with {CommandBinding MethodeName()}. This way interaction logic can be separated from view data.

```csharp
class Controller {
   public Controller() {
      var ui = new View { DataContext = new ViewModel() }.CommandContext( this );
      ...
   }

   public void Method() => ...
   public bool CanMethod() => ...
}

class ViewModel {
   public string MethodTitle { get; set; }
}
```

```xaml
<!--View.xaml-->
<Button Content="{Binding MethodTitle}" Command="{cmdctx:CommandBinding Method()}"/>
...
```

It also supports binding to events.
```xaml
<!--View.xaml-->
<Button Content="{Binding MethodTitle}" Click="{cmdctx:CommandBinding Method()}"/>
...
```

That includes binding to Binding.SourceUpdated in order to get notified when a regular data binding completes.
```xaml
<!--View.xaml-->
<TextBox 
   Text="{Binding MethodTitle,Mode=TwoWay,NotifyOnSourceUpdated=True}" 
   Binding.SourceUpdated="{cmdctx:CommandBinding Method()}"/>
...
```

And you can also pass in method arguments. Arguments can either be 'this' (the element it was bound to), properties of that element, event handler arguments or simple constants.
```xaml
<!--View.xaml-->
<Button Tag="1" Content="{Binding MethodTitle}" Click="{cmdctx:CommandBinding Method(this,Tag,e.RoutedEvent,true)}"/>
...
```