# Examples

Common tasks, each with a complete example. Every code block on this page runs as a test
(`test/Flee.Test/DocumentationTests`), so it works as shown. The examples assume
`using Flee.PublicTypes;` and, for the calculation engine, `using Flee.CalcEngine.PublicTypes;`.

## Filters: boolean expressions

Compile a condition once as `IGenericExpression<bool>` and evaluate it for each item. Flee rejects
the expression at compile time if it does not produce a `bool`.

<!-- example: BooleanFilter -->
```csharp
var context = new ExpressionContext();
context.Variables["age"] = 0;
context.Variables["country"] = "";

IGenericExpression<bool> filter =
    context.CompileGeneric<bool>("age >= 18 and (country = \"DE\" or country = \"AT\")");

var people = new[] { (17, "DE"), (30, "AT"), (45, "FR") };
var accepted = new List<int>();
foreach (var (age, country) in people)
{
    context.Variables["age"] = age;
    context.Variables["country"] = country;
    if (filter.Evaluate()) accepted.Add(age);   // only 30
}
```

`and`, `or`, `xor` and `not` work on booleans (and bitwise on integers); `and` and `or` stop as
soon as the result is known. Variables are defined with a value of the right type first and then
updated for each item.

## Your own functions

Any public static method becomes a function once its type is imported. Reflection is only used
while compiling; the generated code calls the method directly.

<!-- example: CustomFunctionsClass -->
```csharp
public static class CustomFunctions
{
    public static int Product(int a, int b) => a * b;

    public static int Sum(params int[] values)
    {
        int sum = 0;
        foreach (int value in values) sum += value;
        return sum;
    }
}
```

<!-- example: CustomFunctions -->
```csharp
var context = new ExpressionContext();
context.Imports.AddType(typeof(CustomFunctions));                 // product(...), sum(...)
context.Imports.AddType(typeof(CustomFunctions), "fn");           // fn.product(...)
context.Variables["a"] = 100;
context.Variables["b"] = 200;

int plain = context.CompileGeneric<int>("product(a, b) + sum(a, b)").Evaluate();   // 20300
int prefixed = context.CompileGeneric<int>("fn.product(a, b) - a").Evaluate();    // 19900
int many = context.CompileGeneric<int>("sum(1, 2, 3, 4, 5, 6)").Evaluate();       // 21
```

- `AddType(type)` imports without a prefix, `AddType(type, "fn")` under a namespace prefix.
- `params` methods take any number of arguments.
- Built-in types work the same way: `AddType(typeof(Math))` gives `sqrt`, `cos`, `pi` and the rest.
- Overloads are chosen by the cheapest implicit conversion of the arguments.

## Members of values

A variable behaves like an instance of its type: its public properties, fields and methods are
available, and calls can be chained.

<!-- example: InstanceMembers -->
```csharp
var context = new ExpressionContext();
context.Variables["s"] = "this is a string";
context.Variables["start"] = new DateTime(2026, 10, 10);

int length = context.CompileGeneric<int>("s.Length + s.Remove(0, 1).Length").Evaluate();   // 31
string upper = context.CompileGeneric<string>("s.ToUpper().Substring(0, 4)").Evaluate();  // THIS
int month = context.CompileGeneric<int>("start.AddDays(30).Month").Evaluate();            // 11
```

## Arrays, lists and dictionaries

Square brackets index arrays (with dedicated IL) and anything with an indexer, such as lists and
dictionaries. The index can be any expression.

<!-- example: IndexingOwnerClass -->
```csharp
public class Inventory
{
    public int[] Stock = { 10, 20, 30 };
    public List<string> Names = new List<string> { "bolt", "nut", "washer" };
    public Dictionary<string, double> Prices = new Dictionary<string, double> { ["bolt"] = 0.25 };
}
```

<!-- example: Indexing -->
```csharp
var context = new ExpressionContext(new Inventory());
context.Variables["i"] = 1;
context.Variables["matrix"] = new[] { 1, 2, 3 };

int stock = context.CompileGeneric<int>("Stock[i + 1]").Evaluate();            // 30
string name = context.CompileGeneric<string>("Names[i]").Evaluate();           // nut
double price = context.CompileGeneric<double>("Prices[\"bolt\"]").Evaluate();  // 0.25
int element = context.CompileGeneric<int>("matrix[2]").Evaluate();            // 3
```

Membership tests use `in`: `country in ("DE", "AT")`, or `x in Names` against a collection.

## An object as the expression's owner

Passing an object to the context makes it the expression's owner: its members are usable by name,
as if the expression were a method of the class. Access is direct IL, which is faster than
variables, and private members can be allowed.

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

`[ExpressionOwnerMemberAccess(false)]` on a member hides it even if it is public;
`[ExpressionOwnerMemberAccess(true)]` shows a private one. Changing `Owner` lets one compiled
expression work on many objects, but not from several threads at once
([Limitations](Limitations)).

## Values looked up on demand

When the variables are not known in advance, let Flee ask for them through events.

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

The type events are raised while compiling, the value events at every evaluation, once for each
occurrence of the name.

## Expressions as variables

A compiled expression can be the value of a variable in another expression. It is evaluated each
time the outer expression reads it.

<!-- example: ExpressionsAsVariables -->
```csharp
var inner = new ExpressionContext();
inner.Imports.AddType(typeof(Math));
inner.Variables["x"] = 3.14;
IDynamicExpression cos2 = inner.CompileDynamic("cos(x) ^ 2");
IDynamicExpression sin2 = inner.CompileDynamic("sin(x) ^ 2");

var outer = new ExpressionContext();
outer.Variables["a"] = cos2;
outer.Variables["b"] = sin2;
IGenericExpression<double> sum = outer.CompileGeneric<double>("a + b");

double one = sum.Evaluate();      // 1, within rounding
inner.Variables["x"] = 1.0;
double stillOne = sum.Evaluate(); // 1 again: the inner expressions are evaluated each time
```

For a network of named formulas that depend on each other, the calculation engine below does the
bookkeeping.

## Formulas that depend on each other

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

Atoms refer to each other by plain name. To load many at once in any order, use a batch:

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

## Culture

By default numbers and argument lists follow the current culture. Set the culture explicitly, or
change single parser options and recreate the parser.

<!-- example: Culture -->
```csharp
var german = new ExpressionContext();
german.Imports.AddType(typeof(Math));
german.Options.ParseCulture = new CultureInfo("de-DE");          // ',' decimals, ';' between arguments
double a = german.CompileGeneric<double>("max(1,5; 2,25)").Evaluate();   // 2.25

var custom = new ExpressionContext();
custom.Imports.AddType(typeof(Math));
custom.Options.ParseCulture = CultureInfo.InvariantCulture;
custom.ParserOptions.FunctionArgumentSeparator = ';';
custom.ParserOptions.RecreateParser();                            // needed after changing ParserOptions
double b = custom.CompileGeneric<double>("max(1.5; 2.25)").Evaluate();   // 2.25
```

For expressions that are stored or move between machines, fix the culture: the same text can mean
different things, or not compile, under another culture.

## More

- All options (`Checked`, `IntegersAsDoubles`, `RealLiteralDataType`, `StringComparison`, ...):
  [API Guide](API-Guide#options).
- Everything the expression text can contain: [Language Reference](Language-Reference).
