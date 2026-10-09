# Flee: project findings

As of 2026-10-09. Companion to the plan "Flee fork: plan to first release".

**Basis:** upstream `mparlak/Flee`, branch `master`, commit `f3b4fe2` (2022-03-22). All code figures come from reading and counting that commit. **Nothing was built or run**, because no .NET SDK was available in the analysis environment. Build behaviour, warning counts and test results are therefore unverified.

## 1. Summary

- Flee is a small library: about 22,600 lines of C# in 102 source files, half of which is a bundled parser runtime.
- The core design is sound: a classic compiler front end that parses an expression, builds an element tree and emits IL into a `DynamicMethod`.
- The code is a 2007 VB.NET design that was machine-converted to C# in 2017 and lightly patched since. It is structured and consistent, but full of conversion idioms.
- The test safety net is much weaker than it looks: 48 test methods run, while 1,756 expression test cases sit in script files that no test reads.
- Upstream has been inactive since March 2022. Project hygiene is missing: no CI, no license file, stale targets and package metadata.
- A one-person fork is realistic.

## 2. Upstream status

| Item | Value |
| --- | --- |
| Repository | `mparlak/Flee` on GitHub |
| Commits | 68, first on 2017-04-10, last on 2022-03-22 |
| Commits per year | 2017: 27, 2018: 22, 2019: 8, 2020: 2, 2021: 7, 2022: 2 |
| Stars / forks | 678 / 123 |
| Open issues / pull requests | 75 / 2 |
| GitHub releases | None listed |
| NuGet package | `Flee` 2.0.0, published 2022-03-21 |
| NuGet downloads | 5.0 million total, about 1,400 per day |
| Earlier NuGet versions | 1.2.2 (2019-01), 1.2.1 (2018-10), 1.2.0 (2018-05), 1.1.0 (2018-05) |
| License | LGPL, stated in the README and the grammar header (2.1 or later); no `LICENSE` file |

History in brief:

- **2007:** original Flee written in VB.NET by Eugene Ciloci (the grammar header carries his name and "May 2007").
- **2017:** imported to GitHub and converted to C# (README: "Convert this project vb.net to c#").
- **2019:** extension method support added from another fork.
- **2021:** the last substantial engineering work, by Garr Godfrey: long and nested long branches, a stack overflow check, recursion replaced by an explicit stack in the parser, an ambiguous-method fix, a fix for values in the 32-bit unsigned range.
- **2022:** retargeted to net6.0 / net5.0 / netstandard2.1 / netstandard2.0; released as 2.0.0. Nothing since.

## 3. Size

| Part | Files | Lines |
| --- | --- | --- |
| `Parsing` (bundled Grammatica runtime plus generated parser) | 38 | 11,331 |
| Flee logic (elements, IL emission, imports, variables, calc engine, resources) | 64 | 11,266 |
| **Source total** | **102** | **22,597** (18,353 without blank and comment lines) |
| Tests | 11 | 1,572 |

- The public API is small: about 25 public types outside `Parsing`.
- Largest non-parser files: `ImplicitConversions.cs` (573), `InternalTypes/Miscellaneous.cs` (562), `Cast.cs` (513), `Identifier.cs` (492), `ImportTypes.cs` (417), `FunctionCall.cs` (409), `VariableCollection.cs` (403).

## 4. Architecture

Folder layout under `src/Flee`:

| Folder | Content |
| --- | --- |
| `PublicTypes` | `ExpressionContext`, `ExpressionOptions`, `ExpressionParserOptions`, `ExpressionImports`, import types, `VariableCollection`, exceptions |
| `InternalTypes` | `Expression<T>`, `FleeILGenerator`, `BranchManager`, `ImplicitConverter`, binders, utilities |
| `ExpressionElements` | One class per language construct, grouped into `Base`, `Literals`, `LogicalBitwise`, `MemberElements` |
| `CalcEngine` | `CalculationEngine`, `SimpleCalcEngine`, `BatchLoader`, dependency manager |
| `Parsing` | Parser runtime, generated parser, Flee's analyzer |
| `Resources` | Error messages, element names, `DocComments.xml` |

Compile pipeline, as implemented in `InternalTypes/Expression.cs`:

1. The expression clones its `ExpressionContext` (unless `NoClone` is set).
2. `ExpressionContext.Parse` runs the parser under a per-context lock. The analyzer callbacks build the element tree directly; there is no separate syntax tree.
3. The root element is wrapped in `RootExpressionElement`, which converts to the result type.
4. A `DynamicMethod` is created with the signature `(object owner, ExpressionContext context, VariableCollection variables)`.
5. The tree emits IL through `FleeILGenerator`. Branches are first emitted in short form; if any turns out to be long, the whole expression is emitted a second time.
6. The method becomes an `ExpressionEvaluator<T>` delegate. Evaluation is one delegate call.

Element model:

- `ExpressionElement` is the abstract base with two members: `Emit(ilg, services)` and `ResultType`.
- `BinaryExpressionElement`, `UnaryElement`, `MemberElement` and `LiteralElement` are the intermediate bases.
- Type checking, overload resolution and implicit conversion all happen at compile time. Type promotion for primitives is a lookup table in `ImplicitConverter`.
- Compile-time context reaches elements through a service container (`IServiceProvider`), 15 lookups in total.

## 5. Parser and grammar

### Origin

- The parser was generated with **Grammatica**, a parser generator by Per Cederberg.
- The grammar file is still in the repository: `src/Flee/Parsing/Expression.grammar`, 132 lines, grammar type LL, case-insensitive.
- Notation is Grammatica's own and close to EBNF: `{ }` repetition, `[ ]` and `?` optional, `|` alternatives, tokens as literal strings or regular expressions (`<<...>>`).
- It defines **40 tokens and 29 productions**. All 69 names match the checked-in `ExpressionConstants.cs`, so grammar and generated code have not drifted structurally.

### What is what in the `Parsing` folder

| Kind | Files |
| --- | --- |
| Generated from the grammar | `ExpressionParser.cs`, `ExpressionTokenizer.cs`, `ExpressionAnalyzer.cs` (1,395 lines of callback skeleton), `ExpressionConstants.cs` |
| Hand-written for Flee | `CustomExpressionAnalyzer.cs` (class `FleeExpressionAnalyzer`, builds elements in the callbacks), `CustomTokenPatterns.cs` (number and argument-separator tokens built at runtime from the parser options) |
| Grammatica runtime | About 30 files: tokenizer, regular expression engine (`TokenNFA.cs`, `RegExp.cs`), look-ahead sets, `RecursiveDescentParser.cs`, `StackParser.cs`, node and production classes |

### Maintenance state

- Upstream Grammatica looks dormant (README copyright range ends in 2015; exact last-commit date not confirmed).
- More important: Flee's copy is a private fork. It went through the VB.NET and back-to-C# conversions, lost its "generated" headers and was patched for Flee in 2021. No upstream fix would ever reach it.
- Regenerating with the official Java tool would produce different-looking code. Whether it would behave identically is unverified.

### The language, from the grammar

Precedence from lowest to highest:

| Level | Operators |
| --- | --- |
| 1 | `XOR` |
| 2 | `OR` |
| 3 | `AND` |
| 4 | `NOT` (prefix) |
| 5 | `in` (against a list or a collection member) |
| 6 | `=`, `<>`, `<`, `>`, `<=`, `>=` |
| 7 | `<<`, `>>` |
| 8 | `+`, `-` |
| 9 | `*`, `/`, `%` |
| 10 | `^` (power) |
| 11 | unary `-` |
| 12 | member access `.`, indexing `[ ]`, function call |

- **Literals:** integer (with `u`, `l`, `ul` suffixes), hex (`0x...`), real (optional exponent and `f` suffix), string, char, `True`/`False`, `null`, date-time (`#...#`), time span (`##...#`).
- **Special functions:** `if(condition, a, b)` and `cast(expression, type)`.

### What the grammar file does not capture

- The real number pattern depends on `DecimalSeparator` and `RequireDigitsBeforeDecimalPoint`.
- The argument separator is configurable (`FunctionArgumentSeparator`).
- Date-time parsing depends on `DateTimeFormat` (default `dd/MM/yyyy`).
- String escape handling and the interaction of unary minus with integer literals are in the analyzer code.

### The seam to the rest of the library

- Only three files outside `Parsing` reference it: `ExpressionContext.cs`, `Exceptions.cs` and `CalcEngine/InternalTypes/IdentifierAnalyzer.cs`.
- The effective contract is `ExpressionContext.Parse(string, services)` returning the top `ExpressionElement`.
- Six parser types are public and therefore part of the API: `ParseException` and five nested error and pattern enums.

## 6. Code quality

**Overall:** straight and pattern-oriented, not hacky. Dated idioms on a sound structure.

### Strengths

- Predictable composite structure: one small class per language construct.
- Table-driven type promotion; custom reflection binders for overload resolution.
- About 150 XML doc summaries outside the parser.
- Very few self-declared hacks: one "ugly hack" comment, and it is in the Grammatica tokenizer.
- The hard code-generation bugs (long branches, deep expressions) were addressed in 2021.

### Conversion leftovers

| Finding | Count |
| --- | --- |
| `== true` / `== false` comparisons (outside parser) | 227 |
| Booleans combined with non-short-circuit `&` / `|` (approximate) | 29 |
| Uses of non-generic collections (`IList`, `ArrayList`, `Hashtable`) | 69 |
| `break; // TODO: might not be correct` markers left by the converter | 4 |
| `Debug.Assert` checks, which vanish in release builds | 32 |
| VB-style `_my` / `My` / `_our` / `Our` name prefixes | about 1,100 |

- The four converter markers are in `InvocationList.cs` (two), `CalculationEngine.cs` and `ExpressionImports.cs`. They were never reviewed.
- `ArithmeticElement` assigns static fields in its instance constructor, a typical conversion artifact.

### Dated or odd design spots

- Options are stored in a string-keyed `PropertyDictionary`. The class is marked `[Obsolete]` but still backs `ExpressionOptions`, `ExpressionParserOptions` and `ExpressionContext`.
- Elements are created through `Activator.CreateInstance` from a `Type`.
- Some internal helper methods are located by reflection on name strings (10 places).
- `FleeResourceManager` uses `lock (this)`.
- `<Nullable>enable</Nullable>` is set on code that was never annotated.

### Orientation

- **Evaluation speed was the design goal.** All analysis is done at compile time; evaluation is a single delegate call. There are deliberate micro-optimizations: short IL opcode forms, integer powers expanded inline, the left operand cached for `in` lists.
- **Compile speed was not a priority.** See section 9.
- **Maintainability comes from structure only.** Small classes and consistent naming help; missing annotations, debug-only assertions and untyped collections do not.

## 7. File organisation

| Types per file | Files |
| --- | --- |
| 1 | 75 |
| 2 to 5 | 20 |
| 8 or more | 4 |

- Four files are named `Miscellaneous.cs`. `InternalTypes/Miscellaneous.cs` holds 14 unrelated types, `PublicTypes/Miscellaneous.cs` holds 10; the other two are small.
- `ImportTypes.cs` (4 classes) and `VariableTypes.cs` (5) group related families.
- File names often omit the type suffix: `Arithmetic.cs` contains `ArithmeticElement`.
- No nested classes in Flee's own code. All 12 nested types are in the Grammatica runtime.
- No partial classes apart from the generated resource designers.

## 8. Tests

- Framework: NUnit 3.13.2 on net6.0, with the coverlet collector referenced.
- 48 `[Test]` methods in 11 files. Some are timing tests (`Benchmarks.cs`, 664 lines), and `SimpleCalcEngineTests.TestScripts` is empty.
- `LongScriptTests` contains commented-out code that used to load script files.

Script-driven test data that **no test reads**:

| File | Cases | Line format |
| --- | --- | --- |
| `ValidExpressions.txt` | 1,198 | result type; expression; expected result |
| `InvalidExpressions.txt` | 251 | result type; expression; expected compile error reason |
| `ValidCasts.txt` | 172 | result type; cast expression; expected result |
| `CheckedTests.txt` | 135 | expression; checked; should overflow |
| **Total** | **1,756** | |

- The harness that ran these files is not in the repository, and it is not in the git history either. Only the data files were carried along.
- The scripts refer to members of an expression owner (for example `bytea`, `sbytea`). That owner fixture is also missing and has to be recovered from the original VB.NET Flee source or inferred from the scripts.
- `Infrastructure/Core.cs` still contains the commented-out stub `ProcessScriptTests`.
- There is no CI configuration of any kind.

## 9. Performance characteristics

From reading the code, not from measurement:

| Area | Observation |
| --- | --- |
| Evaluation | One delegate call into compiled IL. Fast by design. |
| Variable reads | Each read at evaluation time is a dictionary lookup by name plus an interface call (`GetVariableValueInternal<T>`). The most obvious evaluation cost. |
| Compile: emission | Expressions with long branches are emitted twice. |
| Compile: construction | Elements created by reflection; helper methods looked up by name. |
| Compile: context | Each expression clones its context; parsing is serialized by a lock per context. |
| Parsing | The tokenizer and parser allocate heavily. Share of total compile time unknown. |

A benchmark should measure parse, full compile and evaluate separately, because their costs and optimization options differ.

## 10. Extensibility

- **Cheap:** adding or changing functions, imports, variables, options and events. This is plain C# against a small public surface.
- **More expensive:** new syntax. It means editing the grammar and regenerating with the Java-based Grammatica tool, or hand-editing generated code.
- **Hooks that already exist:** imported types and namespaces, on-demand variables and functions through events, expression owners, custom operators through overloaded operators, extension methods.
- **API leak to be aware of:** parser exception types are public, so replacing the parser is a breaking change unless they are wrapped first.

## 11. Build, packaging and license

- Target frameworks: `net6.0;net5.0;netstandard2.1;netstandard2.0`. Both net5.0 and net6.0 are out of support.
- Package references: `System.Reflection.Emit` 4.7.0, `System.Reflection.Emit.ILGeneration` 4.7.0, `System.Reflection.Emit.Lightweight` 4.7.0, `System.Reflection` 4.3.0, `System.ComponentModel` 4.3.0.
- `GeneratePackageOnBuild` is on. `PackageLicenseUrl` and `PackageIconUrl` just point at the repository page.
- `EmitToAssembly`: the call that saved the assembly is commented out, so the README feature "Generated IL can be saved to an assembly" no longer holds.
- `Resources/DocComments.xml` holds about 1,200 lines of original API documentation with examples.
- No `LICENSE` file, no `.editorconfig`, no analyzers, no `InternalsVisibleTo`.
- License: the grammar header says LGPL 2.1 or later, so a fork stays under the LGPL. This is not legal advice; check the terms before commercial distribution.

## 12. Structural limits

- Runtime IL generation rules out NativeAOT and iOS, and is unfriendly to trimming. This is inherent in the design.
- Parsing is serialized per context by a lock.
- Thread safety of evaluation and of shared contexts was not analysed.

## 13. Conclusions on the parser question

Nothing is decided. The discussion reached these points:

- The current parser works, and no change to it improves stability or testing. Parser work belongs after the first release.
- For independence, the options differ:
  - **Keep Grammatica:** no external dependency, but 11,000 lines nobody would choose to maintain.
  - **ANTLR 4:** actively maintained with excellent tooling, but adds a runtime package for every consumer and a Java tool to the build. Its static lexer makes the runtime-configurable separators awkward.
  - **Hand-written recursive descent or Pratt parser:** no dependencies, probably under 1,000 lines for this grammar, full control over error messages and configurable tokens.
- The risk of "getting lost" in a hand-written parser is limited here: the grammar is a fixed precedence ladder of 29 productions, and the 1,756 script cases act as a specification.
- A reasonable split: ANTLR as a workbench for exploring and documenting the grammar, a hand-written parser as the shipped one. If ANTLR is what keeps the project enjoyable, shipping it costs one dependency and a build step.
- Whatever replaces it: keep the old parser behind a switch and compare both over all valid and invalid test expressions until they agree.

## 14. Known unknowns

- Does the solution build as is on a current SDK, and with how many warnings?
- How many of the 48 existing tests pass?
- How many of the 1,756 script cases pass once a harness exists?
- Can the original test owner fixture be recovered?
- Which of the 75 open upstream issues are real, reproducible bugs?
- Where does compile time actually go (parse, emit, delegate creation)?
- Would regenerating the parser from the grammar reproduce current behaviour?

## 15. Sources

- [mparlak/Flee on GitHub](https://github.com/mparlak/Flee) (source at commit `f3b4fe2`, repository statistics)
- [Flee on NuGet](https://www.nuget.org/packages/Flee) (versions, dates, downloads)
- [cederberg/grammatica on GitHub](https://github.com/cederberg/grammatica) and the [Grammatica website](https://grammatica.percederberg.net/) (parser generator status)
