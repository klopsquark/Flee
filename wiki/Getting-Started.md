# Getting Started

## Install

The fork's package keeps the ID `Flee` and lives on a private feed (a local folder works as well).
Add the feed once, then reference the package:

```
dotnet nuget add source C:\dev\nuget --name local
dotnet add package Flee --version 2.6.0-*
```

The package targets netstandard2.0, netstandard2.1, net8.0 and net10.0, so it runs on .NET
Framework 4.6.1 and later and on .NET Core 2.0 and later. It needs a runtime with a JIT: NativeAOT
and iOS are not supported ([Limitations](Limitations)).

Everything public lives in the namespaces `Flee.PublicTypes` and `Flee.CalcEngine.PublicTypes`.

## Your first expression

<!-- example: QuickStart -->
```csharp
var context = new ExpressionContext();
context.Imports.AddType(typeof(Math));
context.Variables["a"] = 3.0;
context.Variables["b"] = 4.0;

IGenericExpression<double> e = context.CompileGeneric<double>("sqrt(a^2 + b^2)");
double result = e.Evaluate();   // 5
```

Three things happen here:

1. An `ExpressionContext` collects everything an expression may use: imported types (here all
   static members of `System.Math`, so `sqrt` and `pi` work), variables, an optional owner object
   and options.
2. `CompileGeneric<double>` parses the text, checks the types and generates IL. A mistake in the
   text is reported here, not later.
3. `Evaluate()` runs the compiled code. Change a variable and evaluate again; there is no need to
   compile again.

## Dynamic or generic

<!-- example: DynamicAndGeneric -->
```csharp
var context = new ExpressionContext();

IDynamicExpression dynamic = context.CompileDynamic("1 + 2");
object? boxed = dynamic.Evaluate();             // 3, an int

IGenericExpression<int> typed = context.CompileGeneric<int>("1 + 2");
int value = typed.Evaluate();                   // 3, no boxing

IGenericExpression<double> widened = context.CompileGeneric<double>("1 + 2");
double converted = widened.Evaluate();          // 3.0: int converts implicitly to double
```

Use `CompileGeneric<T>` when you know the result type (for example `bool` for filters): it is
faster and rejects expressions of the wrong type at compile time. Use `CompileDynamic` when the
expression decides its own type.

## When an expression is wrong

<!-- example: Errors -->
```csharp
var context = new ExpressionContext();
try
{
    context.CompileDynamic("1 + unknown");
}
catch (ExpressionCompileException ex)
{
    reason = ex.Reason;     // CompileExceptionReason.UndefinedName
    message = ex.Message;   // names the problem and the element that found it
}
```

Every compile problem is an `ExpressionCompileException`; its `Reason` says what kind
(`SyntaxError`, `UndefinedName`, `TypeMismatch` and others, listed in the
[API Guide](API-Guide#errors)). Evaluating can throw whatever the code itself throws, such as
`DivideByZeroException`.

## Good to know from the start

- **Compile once, evaluate often.** Compiling takes microseconds to milliseconds; evaluating takes
  nanoseconds.
- **Names ignore case** by default: `SQRT(A)` is the same as `sqrt(a)`.
- **Numbers follow the current culture** by default. Under a German culture, `1,5` is a number and
  arguments are separated by `;`. Set `context.Options.ParseCulture = CultureInfo.InvariantCulture`
  if expressions are stored or shared between machines ([Examples](Examples#culture)).
- **Variables keep their type.** A variable created with an `int` stays an `int`.

## Next

- [Examples](Examples) for common tasks: filters, custom functions, owners, indexing, the
  calculation engine.
- [Language Reference](Language-Reference) for everything the expression text can contain.
- [API Guide](API-Guide) for the full C# side.
