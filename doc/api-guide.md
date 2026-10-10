# Flee API guide

How to use Flee from C#: contexts, imports, variables, owners, on-demand values, options, errors
and the calculation engine. The expression language itself is described in
[language-reference.md](language-reference.md).

Every code block on this page is a test: it is copied from
`test/Flee.Test/DocumentationTests/ApiGuideExamples.cs`, where it runs with assertions, and a test
checks that the two copies are identical.

## Quick start

<!-- example: QuickStart -->
```csharp
var context = new ExpressionContext();
context.Imports.AddType(typeof(Math));
context.Variables["a"] = 3.0;
context.Variables["b"] = 4.0;

IGenericExpression<double> e = context.CompileGeneric<double>("sqrt(a^2 + b^2)");
double result = e.Evaluate();   // 5
```

An `ExpressionContext` holds everything an expression may use: imported types, variables, an
owner object and options. `CompileGeneric<T>` and `CompileDynamic` parse the text, check types,
generate IL and return an object you can evaluate as often as you like.

## Dynamic and generic expressions

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

- `CompileDynamic` returns an `IDynamicExpression`; `Evaluate()` returns `object?`.
- `CompileGeneric<T>` returns an `IGenericExpression<T>`; `Evaluate()` returns `T`, without boxing.
  The expression's result is converted to `T` if an implicit conversion exists; otherwise
  compiling fails (`"1.5"` cannot become an `int`).
- `ExpressionOptions.ResultType` does the same for dynamic expressions.

Compile once and evaluate many times: compiling costs tens of microseconds to milliseconds
(parsing dominates; see `benchmarks/results`), evaluating costs nanoseconds.

## Variables

<!-- example: Variables -->
```csharp
var context = new ExpressionContext();
context.Variables["price"] = 10.0;
context.Variables.DefineVariable("quantity", typeof(int));   // typed, value 0

IGenericExpression<double> total = context.CompileGeneric<double>("price * quantity");

context.Variables["quantity"] = 3;
double first = total.Evaluate();     // 30

context.Variables["price"] = 12.5;   // no recompiling needed
double second = total.Evaluate();    // 37.5

string[] used = total.Info.GetReferencedVariables();   // price, quantity
```

- `Variables[name] = value` defines a variable on first use, with the type of the value. Setting
  it again changes its value; compiled expressions read the current value on every evaluation.
- `DefineVariable(name, type)` defines a typed variable with the type's default value.
- A variable's type is fixed once it exists. Assigning a value of another type is not checked and
  fails later (upstream issue #26, `doc/deferred.md` D-09); remove and re-add the variable instead.
- `Info.GetReferencedVariables()` lists the variables an expression uses.
- A variable acts as an instance of its type: expressions can use its public instance members
  (`s.Length`, `s.Remove(0, 1)`, `rand.NextDouble()`) and index it if it is an array or has an
  indexer (`list[1]`).
- A compiled expression can itself be a variable value: other expressions then use its result.

## Imports

<!-- example: Imports -->
```csharp
var context = new ExpressionContext();
context.Imports.AddType(typeof(Math));                     // cos(0), pi
context.Imports.AddType(typeof(DateTime), "DateTime");     // DateTime.Today
// A single method; for overloaded methods pass the MethodInfo instead of its name.
MethodInfo join = typeof(string).GetMethod("Join", new[] { typeof(string), typeof(string[]) })!;
context.Imports.AddMethod(join, "text");                  // text.join(", ", ...)
context.Imports.ImportBuiltinTypes();                      // int.MaxValue, cast(x, long)

object? pi = context.CompileDynamic("pi").Evaluate();
object? joined = context.CompileDynamic("text.join(\", \", \"a\", \"b\")").Evaluate();
object? max = context.CompileDynamic("cast(int.MaxValue, long) + 1").Evaluate();
```

- `AddType(type)` makes a type's public static members usable without prefix; `AddType(type, ns)`
  puts them under a namespace prefix (`DateTime.Today`). Only imported types, and types reached
  through values, are visible to expressions.
- `AddMethod(name, type, ns)` imports a single static method by name; for an overloaded method it
  throws `AmbiguousMatchException`, so pass the `MethodInfo` (`AddMethod(methodInfo, ns)`).
- Methods with a `params` array take any number of arguments (`text.join` above).
- `ImportBuiltinTypes()` adds the C# type names (`int`, `long`, `string`, ...) for `cast(...)` and
  for static members such as `int.MaxValue`.
- Nested namespaces can be built with `NamespaceImport` and `TypeImport` and added to
  `Imports.RootImport`.

## Expression owner

<!-- example: OrderClass -->
```csharp
public class Order
{
    public double Price = 12.5;
    public int Quantity = 4;
    private string _customer = "ACME";
    public string Customer => _customer;
}
```

<!-- example: Owner -->
```csharp
var order = new Order { Price = 12.5, Quantity = 4 };
var context = new ExpressionContext(order);

IGenericExpression<double> total = context.CompileGeneric<double>("Price * Quantity");
double first = total.Evaluate();                // 50

total.Owner = new Order { Price = 2, Quantity = 3 };
double second = total.Evaluate();               // 6

context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;
string? customer = context.CompileGeneric<string>("_customer").Evaluate();   // ACME
```

- The object passed to `new ExpressionContext(owner)` is the expression's owner: its fields,
  properties and methods are usable by name, as if the expression were code inside the class.
- `OwnerMemberAccess` (default `Public`) controls whether non-public members are visible.
  `[ExpressionOwnerMemberAccess(true/false)]` on a member overrides it for that member.
- `Owner` on a compiled expression can be changed to another object of the same type.
  **Not thread-safe:** the owner is stored in the expression, so two threads must not set
  different owners on one expression (upstream issue #99, `doc/deferred.md` D-11). Compile one
  expression per thread, or give each thread its own context.

## On-demand variables and functions

<!-- example: OnDemand -->
```csharp
var context = new ExpressionContext();
var values = new Dictionary<string, double> { ["width"] = 3, ["height"] = 4 };

context.Variables.ResolveVariableType += (sender, e) =>
    e.VariableType = values.ContainsKey(e.VariableName) ? typeof(double) : null;
context.Variables.ResolveVariableValue += (sender, e) =>
    e.VariableValue = values[e.VariableName];

context.Variables.ResolveFunction += (sender, e) =>
{
    if (e.FunctionName == "area") e.ReturnType = typeof(double);
};
context.Variables.InvokeFunction += (sender, e) =>
    e.Result = (double)e.Arguments[0]! * (double)e.Arguments[1]!;

IGenericExpression<double> area = context.CompileGeneric<double>("area(width, height)");
double result = area.Evaluate();   // 12

values["width"] = 5;
double updated = area.Evaluate();  // 20: values are fetched on every evaluation
```

When a name is neither a member nor a defined variable, Flee asks:

- `ResolveVariableType` at compile time for the variable's type (leave `VariableType` null to say
  "not mine"), and `ResolveVariableValue` at every evaluation for its value;
- `ResolveFunction` at compile time for a function's return type (`ArgumentTypes` holds the
  argument types), and `InvokeFunction` at every evaluation for its result.

Use this to connect expressions to data you look up on the fly (a dictionary, a blackboard, a
database row) without defining every variable in advance.

## Options

<!-- example: Options -->
```csharp
var context = new ExpressionContext();

context.Options.Checked = true;                                  // overflow throws
context.Options.IntegersAsDoubles = true;                        // 7 / 2 = 3.5
context.Options.RealLiteralDataType = RealLiteralDataType.Decimal; // 0.1 is a decimal
context.Options.StringComparison = StringComparison.OrdinalIgnoreCase; // "a" = "A"
context.Options.ParseCulture = CultureInfo.InvariantCulture;     // '.' and ',' everywhere
```

`ExpressionOptions` (via `context.Options`):

| Option | Default | Effect |
| --- | --- | --- |
| `Checked` | false | Integer overflow and narrowing casts throw `OverflowException` |
| `CaseSensitive` | false | Names are matched case-sensitively |
| `IntegersAsDoubles` | false | Integer literals are `Double` |
| `RealLiteralDataType` | `Double` | Type of real literals without suffix |
| `StringComparison` | `Ordinal` | How `=` and `<>` compare strings |
| `OwnerMemberAccess` | `Public` | Which owner members are visible |
| `ParseCulture` | current culture | Decimal separator, argument separator, date format |
| `ResultType` | none | Result type for dynamic expressions |
| `EmitToAssembly` | false | Obsolete; has no effect |

`ExpressionParserOptions` (via `context.ParserOptions`) sets the separators and the date format
individually; call `RecreateParser()` after changing them. Options are copied into each expression
when it is compiled; changing them later does not affect compiled expressions.

## Errors

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

Compiling throws `ExpressionCompileException`; `Reason` says what kind of problem it is:

| Reason | Meaning |
| --- | --- |
| `SyntaxError` | The text does not parse |
| `UndefinedName` | A name or function is unknown, or no overload fits the arguments |
| `TypeMismatch` | An operator or conversion does not apply to the operand types |
| `InvalidExplicitCast` | `cast(...)` between types that cannot be converted |
| `ConstantOverflow` | A literal does not fit its type |
| `InvalidFormat` | A date or time-span literal cannot be read |
| `AmbiguousMatch` | Several overloads fit equally well |
| `AccessDenied` | A member exists but is not accessible |
| `FunctionHasNoReturnValue` | A `void` method is used as a value |

Evaluating can throw whatever the generated code throws: `DivideByZeroException`,
`OverflowException` (with `Checked`), `NullReferenceException` for a member access on null,
exceptions from called methods.

## Calculation engine

<!-- example: CalculationEngine -->
```csharp
var engine = new CalculationEngine();
var context = new ExpressionContext();
context.Variables["netPrice"] = 100.0;

engine.Add("tax", "netPrice * 0.19", context);
engine.Add("gross", "netPrice + tax", context);

double gross = engine.GetResult<double>("gross");          // 119

context.Variables["netPrice"] = 200.0;
engine.Recalculate("tax");                                 // tax and everything after it
double updated = engine.GetResult<double>("gross");        // 238

string[] dependents = engine.GetDependents("tax");         // gross
string[] precedents = engine.GetPrecedents("gross");       // tax
```

A `CalculationEngine` holds named expressions ("atoms") that can refer to each other by name. It
tracks the dependencies, evaluates in the right order and recalculates only what depends on a
change:

- `Add(name, expression, context)` compiles and evaluates an atom; names of other atoms in the
  expression become dependencies. An unknown name is a compile error; a circular reference throws
  `CircularReferenceException`. A failed `Add` leaves the engine unchanged.
- `GetResult<T>(name)` returns the current value; `Recalculate(names)` re-evaluates those atoms and
  everything that depends on them, after you changed variables they read.
- `GetDependents`, `GetPrecedents`, `HasDependents`, `HasPrecedents`, `DependencyGraph` describe the
  dependencies; `Remove(name)` removes an atom and its dependents; `Clear()` removes all.
- `NodeRecalculated` is raised for every atom that was recalculated.

### Loading many atoms

<!-- example: BatchLoad -->
```csharp
var engine = new CalculationEngine();
var context = new ExpressionContext();

BatchLoader loader = engine.CreateBatchLoader();
loader.Add("c", "a + b", context);   // order does not matter in a batch
loader.Add("a", "1", context);
loader.Add("b", "a * 2", context);
engine.BatchLoad(loader);

int c = engine.GetResult<int>("c");  // 3
```

A `BatchLoader` accepts atoms in any order and adds them in dependency order. If one fails to
compile, the engine is cleared and `BatchLoadCompileException` names the atom.

### SimpleCalcEngine

<!-- example: SimpleCalcEngine -->
```csharp
var engine = new SimpleCalcEngine();
engine.Context = new ExpressionContext();
engine.AddGeneric<int>("a", "10");
engine.AddDynamic("b", "a * 2");

object? b = ((IDynamicExpression)engine["b"]!).Evaluate();   // 20
```

`SimpleCalcEngine` is a lighter variant without dependency tracking or recalculation: expressions
refer to earlier ones by name, and the indexer returns the compiled expression to evaluate.
Variables set on `Context` apply to the next expression only: a successful add copies them into
that expression's context and clears them; a failed add keeps them.

## Threads, contexts and cloning

- Each compile works on a copy of the context, with copied options and imports and shared
  variables. `ExpressionContext.Clone` makes an explicit copy that also copies the variables;
  changing the copy's options, parser options or imports leaves the original alone.
- Parsing takes a lock on the context. Compiling from several threads with one shared context has
  not been verified in this fork; to be safe, give each thread its own context (a `Clone`).
- A compiled expression can be evaluated from several threads as long as its owner does not
  change (see above). Variables are shared objects: if one thread changes a variable while another
  evaluates, the evaluating thread sees the old or the new value.
- See [limitations.md](limitations.md) for what Flee cannot do.
