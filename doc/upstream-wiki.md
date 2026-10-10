# Upstream wiki

The upstream wiki ([github.com/mparlak/Flee/wiki](https://github.com/mparlak/Flee/wiki), 12 pages,
read 2026-10-10) is the only user documentation upstream offers besides its README. Every code
example on it runs as a test against the fork:
`test/Flee.Test/DocumentationTests/UpstreamWikiTests.cs` (21 tests, all passing). Where the wiki's
C# does not compile or is wrong, the test makes the smallest correction and says so in a comment.

## Pages

| Wiki page | Where the fork documents it | Notes |
| --- | --- | --- |
| Home | `README.md`, `doc/limitations.md` | "Generated IL can be saved to an assembly" is no longer true: `EmitToAssembly` has no effect (R-015, D-06). |
| Getting Started | API guide: Quick start, Errors | The text says `ExpressionCompileExpression`; the type is `ExpressionCompileException` (upstream #96). |
| Examples | (index page) | |
| Extending | API guide: Imports, Expression owner, Variables | `int result = (int)e.Evaluate()` for `cos(a)` throws `InvalidCastException`: the result is a `double`. |
| Expression Variables | API guide: Variables, On-demand variables | Correct. "A cast exception" for a value of another type happens at evaluation, not when it is set (D-09). The resolve events fire once per occurrence of a name, as the page says. |
| Boolean Expression | Language reference: Operators | Correct. |
| Using an expression owner | API guide: Expression owner | The `Func1` example has an empty body and does not compile; the attribute works as described. |
| Using the Calculation Engine | API guide: Calculation engine | Correct, with plain atom names (the old XML documentation used `$a`, R-048). |
| Indexing arrays and collections | Language reference: Names; API guide: Variables | Correct. |
| Culture Sensitive Expressions | Language reference: Culture; API guide: Options | `context.ParseCulture` does not exist; it is `context.Options.ParseCulture`. |
| Customizing Parser | Language reference: Culture; API guide: Options | `CompileDynamic(text, context)` has no second parameter, and `context.RecreateParser()` is not public; use `context.ParserOptions.RecreateParser()`. |
| Calling functions with a variable number of arguments | API guide: Imports | Correct. |

## What came of it

- All wiki examples work on the fork, after the corrections above, so code written from the wiki
  keeps working.
- Two topics were missing from the API guide and are now mentioned there: instance members and
  indexers of variables, and `params` methods.
- The wiki itself belongs to upstream and is not changed. The corrections above are the fork's
  record of where it is out of date.
