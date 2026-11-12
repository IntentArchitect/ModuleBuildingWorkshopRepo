# P2.5 — Opt-in Serilog logging decorator

## 1. Goal

Make WorkshopModule generate the logging decorator that ConsumerTestApp currently has written by hand (`CreateProductCommandLoggingDecorator`), but only for Commands that opt in. The WorkshopModule package becomes visible in the Services designer, and it gains a `Serilog Logging` stereotype for Commands and Queries, with three checkboxes: `Log Request`, `Log Response` and `Log Duration`. All three default to on. A new File Per Model C# File Builder template, `SerilogLoggingDecorator`, over the `Command` element generates a decorator only for Commands that carry the stereotype. The decorator implements the same `IAppRequestHandler<TCommand, TResponse>`. It wraps the inner handler with an `ILogger` and a `Stopwatch` in `try`/`finally`, and logs the request, the response and the duration according to the stereotype's flags. The template registers the decorator with Scrutor's `services.Decorate<…>()` through a `ServiceConfigurationRequest`, and declares the `Scrutor` and `Microsoft.Extensions.Logging.Abstractions` NuGet dependencies. The plan ends by updating WorkshopModule in ConsumerTestApp and applying the stereotype to `CreateProductCommand` (model only).

## 2. Starting point

Git tag: `p2-done`.

What exists at that tag:

- `intent/ModuleBuildingTraining.isln` is an Intent Architect solution with three applications: ModuleBuilderAiSkills, WorkshopModule and ConsumerTestApp.
- **WorkshopModule** is a Module Builder application.
  - Its Module Builder designer has one package, `WorkshopModule` (specialization `Intent Module`). Directly under the package are:
    - eight Single File C# Templates: `ICorrelationContext`, `CorrelationContext`, `CorrelationMiddleware`, `CorrelationDelegatingHandler`, `IAppRequest`, `IAppRequestHandler`, `IDispatcher` and `Dispatcher`;
    - two File Per Model C# Templates over the CQRS `Command` element: `CommandRequest` (Role `Application.Command`) and `CommandHandler` (Role `Application`), both with Default Location `Commands` and Model Name `CommandModel`;
    - the `CqrsControllerInstaller` Factory Extension;
    - the `Tracing Settings` Module Settings Configuration.
  - The package carries the `Module Settings` stereotype: `Version` = `1.0.0`, `Include in Module` = false, `Reference in` = `Selected Designers`, `Reference in Designer` empty. There are no stereotype definitions.
  - Intent.Modelers.Services 4.0.16 and Intent.Modelers.Services.CQRS 6.0.8 are installed in WorkshopModule as designer metadata only. The `Services` designer and the CQRS `Command` and `Query` element settings are visible to the Module Builder as referenced, read-only elements.
  - Its C# project is `WorkshopModule/WorkshopModule/WorkshopModule.csproj` (net8.0). It references `Intent.Modules.Common` 3.11.6, `Intent.Modules.Common.CSharp` 3.10.11, `Intent.SoftwareFactory.SDK` 3.14.0, `Intent.Modules.AspNetCore.Controllers` 7.1.15, `Intent.Modules.Modelers.Services` 4.0.16, `Intent.Modules.Modelers.Services.CQRS` 6.0.8, `Intent.RoslynWeaver.Attributes` 3.0.0 and `Intent.Packager` 3.6.2-pre.0. Its VS solution is `WorkshopModule/ModuleBuildingTraining.sln`.
  - `WorkshopModule/WorkshopModule/WorkshopModule.imodspec` is at version 1.0.0.
  - `Templates/CommandHandler/CommandHandlerTemplatePartial.cs` shows the house style for a File Per Model template over `CommandModel`:
    - the class is declared by name in the constructor;
    - everything else happens in `OnBuild(…, 0)`;
    - the response type is `GetTypeName(Model.TypeReference)`, or `Unit`;
    - a `BeforeTemplateExecution()` override publishes the DI request.
  - Building writes `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod`. That folder is the solution's `Workshop` module repository.
- **ConsumerTestApp** is a Clean Architecture .NET 10 app at `ConsumerTestApp/`. WorkshopModule 1.0.0 is installed from the `Workshop` repository.
  - The `ConsumerTestApp.Application` project has the Output Anchor role `Application`. All projects have `ImplicitUsings` and `Nullable` enabled.
  - Installed and relevant: Intent.Application.DependencyInjection 4.1.18, Intent.Modelers.Services 4.0.16, Intent.Modelers.Services.CQRS 6.0.8, Intent.Common.CSharp 3.10.11.
  - Its **C# Style Configuration → Parameter Placement** is `default`.
  - Its Services designer has the `ConsumerTestApp.Services` package. That package contains a `Products` folder with `CreateProductCommand` (HTTP POST, returns `guid`, fields `Title: string` and `Price: decimal`) and `GetProductsQuery` (HTTP GET, no generated action).
  - The hand-written decorator is the reference output:
    - `ConsumerTestApp/ConsumerTestApp.Application/Commands/Products/CreateProductCommandLoggingDecorator.cs`;
    - its registration line in `ConsumerTestApp/ConsumerTestApp.Application/DependencyInjection.cs`, `services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();`, which comes after the generated handler registration and before `return services;`;
    - the `Scrutor` 7.0.0 and `Microsoft.Extensions.Logging.Abstractions` 10.0.9 PackageReferences in `ConsumerTestApp/ConsumerTestApp.Application/ConsumerTestApp.Application.csproj`.
  - These pieces survived the consumer's Software Factory run at p2-done, and the decorator already runs and logs.

Read these files before Step A1. They are the specification, and you only read them. **Where the hand-written decorator and Section 8 differ, Section 8 wins.** Section 8 is the exact shape the module can generate. The facilitator may already have adjusted the hand-written file to match it. If not, Section 8 lists each difference.

## 3. Skills to load

Load these before any other step by reading `.claude/skills/<name>/SKILL.md` and the resources listed. Use them instead of decompiling NuGet packages.

- `file-builder-expert`: read `resources/api-cheatsheet.md`, `resources/builder-patterns.md`, `resources/troubleshooting.md` and `resources/patterns/*.cs` (especially `control-flow.cs` for try/finally and separators).
- `intent-metadata-consumer`: read `resources/metadata-cheatsheet.md`.
- `intent-module-orchestrator`: read `resources/orchestration-cheatsheet.md`, sections §DI Registration, §Host Wiring and §Two-Tier Module Dependency.
- `scripting-the-model`: required before `run_designer_query` / `run_designer_script`. Read `references/elements.md`, `references/stereotypes.md`, `references/types.md` and `references/packages.md`. The authoring API for stereotype **definitions** (`addStereotypeDefinition`, `setTargetMode`, `addProperty`, `setControlType`, …) is returned as an addendum inside the result of `getElementTypes()` in the Module Builder designer. Read it before Step A4.
- `exploring-the-model`
- `changing-the-model`
- `generating-and-applying-code`
- `working-with-modules` *(Phase C)*
- `resolve-destructive-changes`: load it only if `run_software_factory` reports a change as destructive "yes" or "unknown".

Every Intent Architect MCP tool call needs the `intention` parameter (10 words or fewer).

## 4. Scope

**In**

- WorkshopModule Module Builder metadata:
  - the `Module Settings` stereotype values on the `WorkshopModule` package (Step A3);
  - one Stereotype Definition, `Serilog Logging` (Step A4);
  - one File Per Model C# Template, `SerilogLoggingDecorator` (Step A5).
- Running the Software Factory on **WorkshopModule only**, and applying its changes.
- Bespoke code in WorkshopModule:
  - the constructor body of `Templates/SerilogLoggingDecorator/SerilogLoggingDecoratorTemplatePartial.cs`, plus an added `BeforeTemplateExecution()` override;
  - the body of `GetModels` in `Templates/SerilogLoggingDecorator/SerilogLoggingDecoratorTemplateRegistration.cs`, plus one `using`.
- Building `WorkshopModule/ModuleBuildingTraining.sln`.
- In **ConsumerTestApp** (Phase C, the only changes of any kind to ConsumerTestApp):
  - deleting the cached `intent/.cache/modules/WorkshopModule.1.0.0` folder;
  - updating WorkshopModule 1.0.0 from the `Workshop` repository with `install_or_update_modules`;
  - applying the `Serilog Logging` stereotype to `CreateProductCommand` in the Services designer, with all three flags on (model only).

**Out**

- Never run the Software Factory on ConsumerTestApp.
- Never hand-edit files owned by other modules. That includes:
  - every ConsumerTestApp code file, including `CreateProductCommandLoggingDecorator.cs`, `DependencyInjection.cs` and `ConsumerTestApp.Application.csproj`;
  - `modules.config` in any application;
  - every generated file in WorkshopModule outside the bodies listed under In;
  - the generated `Api/*StereotypeExtensions.cs` files.
- A decorator for Queries. The stereotype can be applied to Queries, but no Query handler exists yet (P2 decision 19), so nothing is generated for them.
- Modelling `NuGet Packages` elements in the Module Builder. The two dependencies are declared in code (decision 4).
- Changing WorkshopModule's version. It stays 1.0.0.
- Any change to `WorkshopModule.csproj` or a hand edit to `WorkshopModule.imodspec`. None is needed.
- Decompiling or reflecting over any NuGet package or module assembly.
- Icons, release notes, docs, migrations, and tests.
- Anything inside `.intent` or `Intent.Metadata` folders, and any `.application.config` file. Change models only through `run_designer_script`.
- Committing or tagging. The facilitator tags `p2.5-done`.

## 5. Decisions already made — do not ask

### Facilitator decisions (the user specified these)

1. **Designer visibility.** On the `WorkshopModule` package's `Module Settings` stereotype, set `Include in Module` = true and `Reference in Designer` = the `Services` designer. The package's stereotypes then become available in the Services designer.
2. **Stereotype.** A `Serilog Logging` stereotype applicable to Commands and Queries, with three checkbox properties: `Log Request`, `Log Response` and `Log Duration`. All three default to on.
3. **Template.** A File Per Model C# File Builder template named `SerilogLoggingDecorator`, over the CQRS `Command` element, with Role `Application`, Default Location `Commands` and Model Name `CommandModel`. Run the Software Factory on WorkshopModule and apply.
4. **Generated decorator:**
   - It is generated only for Commands that carry the `Serilog Logging` stereotype.
   - It implements the same `IAppRequestHandler<TCommand, TResponse>` as the handler.
   - It wraps the inner handler with an `ILogger` and a `Stopwatch` in `try`/`finally`.
   - It logs the request, the response and the duration according to the stereotype's flags.
   - It is registered with Scrutor's `services.Decorate`.
5. **ConsumerTestApp model.** In ConsumerTestApp's Services designer, apply `Serilog Logging` to `CreateProductCommand` with all three flags on. Model only; do not run the Software Factory on ConsumerTestApp.
6. **Typed API only.** Read the stereotype through the generated typed API (`HasSerilogLogging()`, `GetSerilogLogging().LogDuration()`, …), never through raw string lookups.
7. **NuGet.** `Scrutor` 7.0.0 (4.2.2 predates .NET 10), plus `Microsoft.Extensions.Logging.Abstractions`. Declare both NuGet dependencies in the template constructor, never inside `OnBuild`.
8. **Registration.** Publish the `Decorate` call as a `ServiceConfigurationRequest`. The Application DI template writes all container registrations before all service configurations, so the call lands after the handler registration. Scrutor throws at startup otherwise.
9. **try/finally**, so the duration is logged even when the handler throws.

### Plan-author decisions (derived from the hand-written pattern, the loaded skills, and a check of the referenced package versions; not facilitator decisions)

10. **Class name.** The constructor declares `AddClass($"{Model.Name}LoggingDecorator")`, which gives `CreateProductCommandLoggingDecorator`.
    - The File Per Model scaffold emits `AddClass($"{Model.Name}", …)`. Kept as is, that would write `Commands/Products/CreateProductCommand.cs`, which is `CommandRequest`'s output. Replace it.
    - The name also has to match the hand-written class exactly. The generated file then lands on the hand-written file's path and replaces it. The generated `Decorate` line is then textually identical to the hand-written one, so the `[IntentMerge]` `AddApplication` method keeps a single line instead of two.
11. **Opt-in filter in `GetModels`.** `SerilogLoggingDecoratorTemplateRegistration.GetModels` (generated `Body = Mode.Ignore`) returns `_metadataManager.Services(application).GetCommandModels().Where(x => x.HasSerilogLogging()).ToList()`, with `using WorkshopModule.Api;`.
    - Do not use `CanRunTemplate()`. With the filter in `GetModels`, a Command without the stereotype has no template instance, so no file is generated and no `Decorate` request is published.
    - Removing the stereotype later makes the Software Factory delete the decorator file.
12. **Reading the flags.** Inside `OnBuild`, use `Model.TryGetSerilogLogging(out var logging)` (`intent-metadata-consumer` Musts 1–3).
    - If it returns false, treat all three flags as off. This cannot happen after decision 11, but the guard is mandatory.
    - Otherwise use `logging.LogRequest()`, `logging.LogResponse()` and `logging.LogDuration()` as returned.
    - The accessors come from the generated `WorkshopModule.Api.CommandModelStereotypeExtensions` (Step A7). Never call `GetStereotype("Serilog Logging")` or `GetProperty("Log Request")`.
13. **Response type.** `Model.TypeReference.Element != null ? GetTypeName(Model.TypeReference) : "Unit"`. This is the same expression as `CommandHandlerTemplate` (P2 decision 31).
    - The handler interface is `$"{this.GetIAppRequestHandlerName()}<{this.GetCommandRequestName(Model)}, {response}>"`.
    - Never hard-code `IAppRequestHandler`, `Guid` or a namespace.
14. **Response capture works for any `TResponse`.** The hand-written `Guid? response = null; … return response.Value;` only compiles for non-nullable value types. `Unit` is a record class, and `string`, DTOs and collections are reference types, so none of them has `.Value`. A nullable return type would give `Guid??`. When `Log Response` is on, emit this instead:
    - `object? response = null;` before the `try`;
    - inside the `try`: `var result = await _inner.Handle(request, cancellationToken);`, then `response = result;`, then `return result;`.

    The logged value is the same: Serilog destructures by runtime type, and the value is `null` when the handler throws.
15. **What each flag owns.** Each flag owns exactly the parts of the hand-written method that serve it:
    - **`Log Request`:** the first statement, `_logger.LogInformation("Handling {RequestName} {@Request}", nameof(<Command>), request);`.
    - **`Log Duration`:**
      - `var stopwatch = Stopwatch.StartNew();`;
      - `stopwatch.Stop();` as the first statement of the `finally`;
      - the ` in {ElapsedMilliseconds}ms` part of the message and the `stopwatch.ElapsedMilliseconds` argument.
    - **`Log Response`:**
      - the decision 14 statements;
      - the ` with response {@Response}` part of the message and the `response` argument.
    - **`try`/`finally`** is emitted when `Log Response` **or** `Log Duration` is on.
      - The `finally` ends with one `_logger.LogInformation` call.
      - Its message is `"Handled {RequestName}"`, then ` with response {@Response}` if `Log Response` is on, then ` in {ElapsedMilliseconds}ms` if `Log Duration` is on.
      - Its arguments are, in order: the message, `nameof(<Command>)`, then `response` if `Log Response` is on, then `stopwatch.ElapsedMilliseconds` if `Log Duration` is on.
      - When `Log Response` is off, the `try` body is the single statement `return await _inner.Handle(request, cancellationToken);`.
    - **When `Log Response` and `Log Duration` are both off,** there is no `try`/`finally`. The method ends with `return await _inner.Handle(request, cancellationToken);`.
    - **Statement order:** request log, `stopwatch`, `response`, `try`, `finally`, each present or absent per its flag.
16. **Decorator shape** (`file.Classes[0]`, inside `OnBuild`):
    - `ImplementsInterface(handlerInterface)`.
    - One `AddConstructor` with two parameters, both with `IntroduceReadonlyField()`:
      - `handlerInterface` named `inner`, which gives `_inner`;
      - `UseType($"Microsoft.Extensions.Logging.ILogger<{ClassName}>")` named `logger`, which gives `_logger`. This is the P1 `CorrelationMiddleware` pattern.
    - `AddMethod($"{UseType("System.Threading.Tasks.Task")}<{response}>", "Handle", m => …)`:
      - `m.Async()`;
      - parameters `request` (`this.GetCommandRequestName(Model)`) and `cancellationToken` (`UseType("System.Threading.CancellationToken")`);
      - no `[IntentManaged]` attribute. The decorator is fully generated, and there is no bespoke body.
    - Resolve `Stopwatch` as `UseType("System.Diagnostics.Stopwatch")`, interpolated into the statement string.
    - Do not `AddUsing` anything. `LogInformation` is an extension method in `Microsoft.Extensions.Logging`, the same namespace as `ILogger<>`, so `UseType` already brings it in.
17. **Builder calls for the body.**
    - **Request log:** `m.AddInvocationStatement("_logger.LogInformation", s => s.AddArgument("\"Handling {RequestName} {@Request}\"").AddArgument($"nameof({command})").AddArgument("request"))`. It renders on one line.
    - **Finally log:** the same call plus `.WithArgumentsOnNewLines()`. It renders with every argument, the first included, on its own line, as in the hand-written file.
    - **Message strings** contain literal `{…}` Serilog placeholders. Build them with string concatenation, or escape them as `{{…}}` inside an interpolated string. Never let C# interpolate them. These are message-template braces inside a string literal, not object-initializer braces, so `file-builder-expert` Must-Not 4 does not apply.
    - **try/finally:** `m.AddTryBlock(t => …)` then `m.AddFinallyBlock(f => …)`. They are siblings on the method (`file-builder-expert` Must-Not 3).
    - Plain statements use `AddStatement(string, s => …)`. Never pass a base-typed `CSharpStatement` (`file-builder-expert` Must-Not 2).
18. **Blank lines.** Match Section 8 exactly with the separator API. Never use raw text.
    - The statement that follows the request log (whichever it is) gets `SeparatedFromPrevious()`. On a method's first statement this adds nothing, so it is safe when `Log Request` is off.
    - A `CSharpTryBlock` defaults to a blank line before it. Call `t.SeparatedFromPrevious(false)` so that the `try` sits directly under `object? response = null;` / `var stopwatch …`.
    - Everything else uses the defaults:
      - `finally` follows `}` with no blank line;
      - statements inside blocks have no blank lines between them;
      - fields render before the constructor, with one blank line before the constructor and one before `Handle`.
19. **Signature wrapping.** This is builder behaviour in Intent.Modules.Common.CSharp 3.10.11 under ConsumerTestApp's Parameter Placement `default`. There is no API to change it. Do not try to.
    - A constructor whose estimated length exceeds 120 characters wraps with the **first parameter on the `(` line** and each further parameter on its own line, indented four spaces. The decorator's constructor (about 156 characters) wraps this way.
    - A method wraps with **every** parameter on its own line. `Handle` (about 102 characters) stays on one line.
20. **NuGet calls.** In the constructor, after `base(...)` and before `CSharpFile` is assigned:
    - `AddNugetDependency("Scrutor", "7.0.0");`
    - `AddNugetDependency("Microsoft.Extensions.Logging.Abstractions", "10.0.9");`

    The `(string packageName, string packageVersion)` overload on `CSharpTemplateBase` is public and not obsolete in 3.10.11. Version 10.0.9 matches ConsumerTestApp's existing PackageReference, so the csproj does not change. The packages attach to the project the decorator file is in, which is the Application project, where `Decorate` is also called.
21. **The `Decorate` request.**
    - Published from `public override void BeforeTemplateExecution()`. The override calls `base.BeforeTemplateExecution()`, then:

      `ExecutionContext.EventDispatcher.Publish(ServiceConfigurationRequest.ToRegister($"Decorate<{handlerInterface}, {ClassName}>").ForConcern("Application").RequiresUsingNamespaces(GetTemplate<IClassProvider>(IAppRequestHandlerTemplate.TemplateId).Namespace).HasDependency(this).HasDependency(GetTemplate<ITemplate>(IAppRequestHandlerTemplate.TemplateId)).WithPriority(20));`

      `handlerInterface` is computed exactly as in decision 13.
    - These facts were checked against Intent.Application.DependencyInjection 4.1.18:
      - Its DI template ignores any `ServiceConfigurationRequest` whose `Concern` is not exactly `"Application"`, so `ForConcern("Application")` is mandatory.
      - It renders the request as `services.{ExtensionMethodName}();`, with no further processing. So the generic arguments go into the name string, and the line comes out as `services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();`.
      - It throws `ArgumentOutOfRangeException` for **any** entry in the parameter list, so pass no `ParameterType`.
      - It adds a `using` for each `HasDependency` template's namespace (which brings in the decorator and command namespace) and for each `RequiresUsingNamespaces` entry (`IAppRequestHandler` and `Unit`). It always adds `Microsoft.Extensions.DependencyInjection`, which is where Scrutor's `Decorate` lives.
      - It writes, in order: its decorators' lines, then container registrations ordered by priority, then service configurations ordered by priority, then `return services;`.
    - The template needs `using Intent.Modules.Common.CSharp.DependencyInjection;` and `using WorkshopModule.Templates.IAppRequestHandler;`.
22. **Constructor vs OnBuild** (P2 decision 23, which implements `file-builder-expert` Must 7). The constructor body, in this order:
    1. the two `AddNugetDependency` calls;
    2. `CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())`;
    3. `.AddClass($"{Model.Name}LoggingDecorator")`, declared by name only;
    4. `.OnBuild(file => { … }, 0)`.

    Everything else happens inside that `OnBuild`: the stereotype read, members, base interface, and `UseType`/`GetTypeName`/`Get…Name()` resolution. `this.GetFolderPath()` already follows the model's folders, so the file lands in `Commands/Products`.
23. **Stereotype definition details.**
    - **Placement:** directly under the `WorkshopModule` package. Its C# identifier is `SerilogLogging`.
    - **Targeting:** `setTargetMode("elements-of-type")`, then `setTargetTypes([...])` with the ids of the CQRS `Command` and `Query` elements of specialization `Element Settings`. By name alone they are ambiguous with same-named `Mappable Element Settings`. Resolve them with `findElements({ name: "Command", includeReferences: true })` filtered on specialization `Element Settings`, and the same for `Query`.
    - **Apply mode:** `setApplyMode("manually")`.
    - **Properties:** three, in this order: `Log Request`, `Log Response`, `Log Duration`. Each has `setControlType("checkbox")` and `setDefaultValue("true")`.
    - **No icon,** no display text, no scripts.
24. **Template Settings for `SerilogLoggingDecorator`.** As P2 decision 33:
    - `Source` = `Lookup Type`;
    - `Designer` = the `Services` designer reference;
    - `Model Type` = the CQRS `Command` element settings;
    - `Model Name` = `CommandModel`, set explicitly;
    - `Role` = `Application`;
    - `Default Location` = `Commands`.

    The Software Factory must then generate `FilePerModelTemplateRegistration<CommandModel>`. If it generates `object`, fix the model; never fix the generated registration.
25. **Layout.** The stereotype definition and the template sit directly under the `WorkshopModule` package, not in a folder.
26. **Updating the module in ConsumerTestApp is part of this plan (Phase C).** This is an assumption, stated because the facilitator did not say who updates the module. Facilitator decision 5 needs the `Serilog Logging` definition inside ConsumerTestApp, and only a module update delivers it.
    - The update must use the **final** `.imod`, so Phase C runs only after Checkpoint B.
    - The version stays 1.0.0, and Intent Architect keeps a stale copy in `intent/.cache/modules/WorkshopModule.1.0.0`. So delete that folder first.
    - `install_or_update_modules` enables every omitted flag. Pass all five explicitly as `true`, which is a normal consumer install:
      - `installDesignerMetadata: true`
      - `installDesigners: true`
      - `enableFactoryExtensions: true`
      - `installApplicationSettings: true`
      - `installTemplateOutputs: true`
    - Updating a module does not run the Software Factory.

## 6. Steps

### Phase A — Module Builder metadata

**A1. Read the reference.**
- Read the three ConsumerTestApp files listed in Section 2.
- Also read these for the house style:
  - `WorkshopModule/WorkshopModule/Templates/CommandHandler/CommandHandlerTemplatePartial.cs`
  - `.../CommandHandler/CommandHandlerTemplateRegistration.cs`
  - `.../CorrelationMiddleware/CorrelationMiddlewareTemplatePartial.cs`
- Do not modify any of them.

**A2. Explore the designer.**
- Call `get_applications` to get the ids of:
  - the WorkshopModule application and its `Module Builder` designer;
  - the ConsumerTestApp application and its `Services` designer.
- With `run_designer_query` on the Module Builder, call `getDesignerSchema()`, `getDesignerModelStructure()` and `getElementTypes()`. Read the stereotype-definition addendum in the `getElementTypes()` result.
- Confirm:
  - The `WorkshopModule` package has the elements listed in Section 2, no stereotype definitions, and nothing named `SerilogLoggingDecorator` or `Serilog Logging`.
  - The package's `Module Settings` stereotype has the properties `Include in Module` (true/false), `Reference in` and `Reference in Designer` (ref to Designer, multiple).
  - `findElements({ name: "Services", includeReferences: true })` returns the `Services` element of specialization `Designer`.
  - `Command` and `Query` each have exactly one referenced element of specialization `Element Settings`, from `Intent.Modelers.Services.CQRS`.
- Check: the query results show all of the above. If `SerilogLoggingDecorator` or `Serilog Logging` already exists, stop and report.

**A3. Make the package visible in the Services designer.** Use one `run_designer_script`:
- On the `WorkshopModule` package, `ensureStereotype("Module Settings")`.
- Set `Include in Module` = `true`.
- Leave `Reference in` = `Selected Designers`.
- Set `Reference in Designer` = `[<id of the Services Designer element from A2>]`.
- Do not change `Version`.

Check: read the stereotype back with `run_designer_query`. It shows `Include in Module` = true and `Reference in Designer` = `Services`, and `Version` is still `1.0.0`.

**A4. Define the `Serilog Logging` stereotype.** Use one `run_designer_script` that follows the addendum's idempotent idiom (`pkg.getStereotypeDefinition("Serilog Logging") ?? pkg.addStereotypeDefinition("Serilog Logging")`) and decision 23.

Check: `getStereotypeDefinitions()` returns `Serilog Logging`, with these properties:

| Property | Control type | Default |
|---|---|---|
| `Log Request` | checkbox | `true` |
| `Log Response` | checkbox | `true` |
| `Log Duration` | checkbox | `true` |

It targets `Command` and `Query`, and its apply mode is manual.

**A5. Model the template.** Use one `run_designer_script` that creates a `C# Template` named `SerilogLoggingDecorator` directly under the `WorkshopModule` package:
- type reference `File Per Model`;
- `C# Template Settings` → `Templating Method` = `C# File Builder`;
- the `Template Settings` from decision 24.

Check: `getElementDetails("SerilogLoggingDecorator")` shows these values, with `Model Name` = `CommandModel`.

**A6. Validate.** Run `getDesignerValidationErrors()` with `run_designer_query`. It must return zero errors. If it does not, fix the model with a follow-up `run_designer_script` and check again.

**A7. Generate WorkshopModule.**
- Call `run_software_factory` for the **WorkshopModule** application. Review the staged changes with `get_file_diffs`, then apply them with `apply_staged_file_changes`, passing exactly the paths reviewed. Follow `generating-and-applying-code`. If a change is reported destructive "yes" or "unknown", follow `resolve-destructive-changes`.
- Expected changes:
  - `Templates/SerilogLoggingDecorator/SerilogLoggingDecoratorTemplatePartial.cs` and `…TemplateRegistration.cs`.
    - The partial derives from `CSharpTemplateBase<CommandModel>`.
    - The registration derives from `FilePerModelTemplateRegistration<CommandModel>`, and its `GetModels` returns `_metadataManager.Services(application).GetCommandModels()`.
  - `Templates/TemplateExtensions.cs` gains `GetSerilogLoggingDecoratorName(…)` overloads.
  - New stereotype extension files in namespace `WorkshopModule.Api` (expected under `WorkshopModule/WorkshopModule/Api/`):
    - `CommandModelStereotypeExtensions` with `HasSerilogLogging(this CommandModel)`, `GetSerilogLogging(this CommandModel)` and `TryGetSerilogLogging(this CommandModel, out SerilogLogging)`, plus a nested `SerilogLogging` class with `bool LogRequest()`, `bool LogResponse()` and `bool LogDuration()`;
    - the same for `QueryModel` in `QueryModelStereotypeExtensions`.
  - `WorkshopModule.imodspec` gains:
    - a `<template id="WorkshopModule.SerilogLoggingDecorator">` entry with role `Application` and location `Commands`;
    - a `<metadata>` entry that installs the `WorkshopModule` package into the `Services` designer. Its exact shape is the Module Builder's.
- Check: all of these files and entries exist. Record the `TemplateId` constant and the exact path of the two `Api` files.

**A8. Build.** Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` from the solution root. It must succeed.

**Checkpoint A:** the model has the package visibility, the `Serilog Logging` stereotype and the `SerilogLoggingDecorator` template, with zero validation errors. The generated scaffolding is applied, and the build is green. Phase B starts from here.

### Phase B — Template implementation

These rules apply to B1 and B2:
- Edit only the bodies listed under Scope → In. Keep the generated `CSharpFile`, `DefineFileConfig()` and `TransformText()` members exactly as generated.
- Follow decisions 10–22.
- Build with warnings visible after each step. Treat any `CS0618` "obsolete" warning as a failure.
- Section 8 has the exact expected output.

**B1. Registration filter** (`Templates/SerilogLoggingDecorator/SerilogLoggingDecoratorTemplateRegistration.cs`)
- Replace the body of `GetModels` as decision 11 says, and add `using WorkshopModule.Api;`.
- Check: build is green.

**B2. SerilogLoggingDecoratorTemplate** (`Templates/SerilogLoggingDecorator/SerilogLoggingDecoratorTemplatePartial.cs`)
- Constructor: decisions 20 and 22. Remove the scaffold's `exampleParam` constructor.
- In `OnBuild`, apply decisions 12–18:
  1. Read the three flags (decision 12).
  2. Compute `response`, `command` and `handlerInterface` (decision 13).
  3. Add the base interface and the constructor (decision 16).
  4. Add the `Handle` method. Its body follows decisions 14, 15, 17 and 18.
- Add the `BeforeTemplateExecution()` override from decision 21.
- Check: build is green, and both of these return nothing:
  - `grep -n "GetStereotype(\|GetProperty(" WorkshopModule/WorkshopModule/Templates/SerilogLoggingDecorator/*.cs`
  - `grep -n "\.Value" WorkshopModule/WorkshopModule/Templates/SerilogLoggingDecorator/SerilogLoggingDecoratorTemplatePartial.cs`

**B3. Verify Phase B.**
- Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` from the solution root. It must finish with 0 errors and no `CS0618`. Check that `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod` has a fresh timestamp.
- Run `run_software_factory` on **WorkshopModule** again, then `get_file_diffs`. It must propose zero changes.
  - If it proposes removing bespoke code (for example the `BeforeTemplateExecution()` override, the `GetModels` body or the added `using`), do not apply it.
  - Protect those members with `[IntentManaged(Mode.Ignore)]`, then rebuild and re-run until zero changes are proposed.
- Confirm all of the following:
  - `grep -rn "ConsumerTestApp" WorkshopModule/WorkshopModule` returns nothing.
  - `AddNugetDependency` appears only in the constructor, before `CSharpFile` is assigned, and never inside `OnBuild`.
  - Every cross-template type comes from a `Get…Name()` extension or a `TemplateId` constant.
  - Every type resolution and member addition happens inside `OnBuild`, except the bare class declaration.
  - `WorkshopModule.csproj` and the `<dependencies>` of `WorkshopModule.imodspec` are unchanged from `p2-done`. Check with `git diff p2-done -- WorkshopModule/WorkshopModule/WorkshopModule.csproj`.

**Checkpoint B:** WorkshopModule has the new stereotype and template with zero validation errors. The build is green, and a fresh Software Factory run on WorkshopModule proposes zero changes. The `.imod` is final. Phase C starts from here.

### Phase C — ConsumerTestApp model (model only)

**C1. Clear the cached module.** Run `rm -rf intent/.cache/modules/WorkshopModule.1.0.0` from the solution root (decision 26). Touch nothing else under `intent/.cache`.

**C2. Update WorkshopModule in ConsumerTestApp.** Follow `working-with-modules`. Make one `install_or_update_modules` call on the **ConsumerTestApp** application with `WorkshopModule` version `1.0.0` and all five flags `true` (decision 26).
- Check: `run_solution_query` → `listInstalledModules(<ConsumerTestApp id>, "WorkshopModule")` lists WorkshopModule 1.0.0.
- Never open or edit `modules.config`.

**C3. Confirm the stereotype is available.** With `run_designer_query` on ConsumerTestApp's `Services` designer:
- `getPackageReferences()` lists `WorkshopModule` for the `ConsumerTestApp.Services` package.
- `getStereotypeDefinitions()` (or `getDesignerSchema()`) lists `Serilog Logging`, applicable to `Command` and `Query`, with `Log Request`, `Log Response` and `Log Duration`.

If the stereotype is missing:
- If `intent/.cache/modules/WorkshopModule.1.0.0` was re-created from a stale source, repeat C1–C2 once.
- If the package reference alone is missing, add it with `run_designer_script` (`scripting-the-model` `references/packages.md`) and note it in the final report.
- If it is still missing, stop and report. Do not edit any metadata file.

**C4. Apply the stereotype.** With one `run_designer_script` on ConsumerTestApp's `Services` designer:
- Look up `CreateProductCommand` by path (`ConsumerTestApp.Services/Products/CreateProductCommand`, or `lookupByName`).
- `ensureStereotype("Serilog Logging")`.
- Set `Log Request`, `Log Response` and `Log Duration` to `true` explicitly. Do not rely on the defaults. `GetProperty<bool>` reads a value that was never persisted as `false`.
- Change nothing else: not the fields, not `Http Settings`, and not `GetProductsQuery`.

Check:
- `getElementDetails("CreateProductCommand")` shows `Serilog Logging(Log Request=true, Log Response=true, Log Duration=true)` next to the unchanged `Http Settings`.
- `getDesignerValidationErrors()` on the Services designer returns zero errors.

**Do not** call `run_software_factory` for ConsumerTestApp.

**C5. Final report.**
- The files changed.
- Every decision taken that this plan does not cover (choose the option closest to the hand-written pattern).
- The `TemplateId` and the paths of the generated `Api` files.
- Whether C3 needed a repeat or a manual package reference.

**Checkpoint C:** ConsumerTestApp has WorkshopModule 1.0.0 updated from the final `.imod`. `CreateProductCommand` carries `Serilog Logging` with all flags on, and the Services designer has zero validation errors. ConsumerTestApp's Software Factory has not been run. The facilitator verifies Section 8 and tags `p2.5-done`.

## 7. Done when

- `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental`, run from the solution root, succeeds with no `CS0618` warnings.
- A fresh Software Factory run on WorkshopModule proposes zero changes.
- The WorkshopModule Module Builder has zero validation errors.
- ConsumerTestApp's Services designer has zero validation errors, and `CreateProductCommand` carries `Serilog Logging` with all three flags on. ConsumerTestApp's Software Factory was not run.

## 8. Expected consumer result

These are the facilitator's steps, not the executor's.

1. The executor already updated WorkshopModule in ConsumerTestApp from the final `.imod` (Phase C). If WorkshopModule has been rebuilt since then:
   - delete `intent/.cache/modules/WorkshopModule.1.0.0`;
   - update the module again from the `Workshop` repository.
2. Make sure the hand-written decorator, its `Decorate` line and the two PackageReferences are committed, so every change shows up in `git diff`.
3. Run ConsumerTestApp's Software Factory and review the staged changes before applying them.

**`ConsumerTestApp.Application/Commands/Products/CreateProductCommandLoggingDecorator.cs`**

The template now outputs to this path, so the hand-written file is replaced. With all three flags on, the result is:

```csharp
using System.Diagnostics;
using ConsumerTestApp.Application.Common.Dispatching;
using Intent.RoslynWeaver.Attributes;
using Microsoft.Extensions.Logging;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.SerilogLoggingDecorator", Version = "1.0")]

namespace ConsumerTestApp.Application.Commands.Products
{
    public class CreateProductCommandLoggingDecorator : IAppRequestHandler<CreateProductCommand, Guid>
    {
        private readonly IAppRequestHandler<CreateProductCommand, Guid> _inner;
        private readonly ILogger<CreateProductCommandLoggingDecorator> _logger;

        public CreateProductCommandLoggingDecorator(IAppRequestHandler<CreateProductCommand, Guid> inner,
            ILogger<CreateProductCommandLoggingDecorator> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling {RequestName} {@Request}", nameof(CreateProductCommand), request);

            var stopwatch = Stopwatch.StartNew();
            object? response = null;
            try
            {
                var result = await _inner.Handle(request, cancellationToken);
                response = result;
                return result;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Handled {RequestName} with response {@Response} in {ElapsedMilliseconds}ms",
                    nameof(CreateProductCommand),
                    response,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

**`ConsumerTestApp.Application/DependencyInjection.cs`**: no diff against the current file.
- The module now generates the `Decorate` line that was added by hand.
- Its text is identical to the hand-written line, so the `[IntentMerge]` method keeps exactly one copy, in the same place: after the handler registration and before `return services;`.

```csharp
            services.AddScoped<IDispatcher, Dispatcher>();
            services.AddScoped<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandHandler>();
            services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();
            return services;
```

**`ConsumerTestApp.Application/ConsumerTestApp.Application.csproj`**: no diff. The template asks for `Scrutor` 7.0.0 and `Microsoft.Extensions.Logging.Abstractions` 10.0.9, which are the versions already referenced.

**Hand-written vs generated decorator: differences the module cannot avoid.** Fix the hand-written file to these shapes before running the plan, or accept them as the diff:

| # | Hand-written | Generated | Why |
|---|---|---|---|
| 1 | `Guid? response = null;`, `response = await _inner.Handle(...)`, `return response.Value;` | `object? response = null;`, `var result = await _inner.Handle(...)`, `response = result;`, `return result;` | `.Value` exists only on nullable value types. `Unit` (a record class), `string`, DTOs and collections have none, and a nullable return type would give `Guid??` (decision 14). |
| 2 | Constructor parameters each on their own line, the first one included | `IAppRequestHandler<CreateProductCommand, Guid> inner,` stays on the `public CreateProductCommandLoggingDecorator(` line, and `logger` goes on the next line, indented four spaces | Builder behaviour under Parameter Placement `default` (decision 19). Changing ConsumerTestApp's setting to `depends-on-length` would also reflow every other generated constructor. |
| 3 | No Intent header | `using Intent.RoslynWeaver.Attributes;` and the `[assembly: …]` attribute(s). As with `CreateProductCommand.cs` at p2-done, `[assembly: DefaultIntentManaged(Mode.Fully)]` may be absent when the weaver merges into an existing hand-written file. | Every generated file has it. Like every Intent-generated file, it ends without a trailing newline. |

Everything else matches the hand-written file exactly:
- the class name and path;
- the `ILogger<…>`/`IAppRequestHandler<…>` fields;
- the using set and order;
- both message templates and their argument order;
- `nameof(CreateProductCommand)`;
- `try`/`finally` with `stopwatch.Stop()` first;
- the one-line `Handle` signature;
- the blank lines.

**Flag variations to spot-check** (change the flags on `CreateProductCommand`, re-run, and revert):

- **Log Request off:** the `Handling` line and the blank line after it disappear. `var stopwatch` becomes the first statement.
- **Log Response off:**
  - `object? response = null;` disappears;
  - the `try` body becomes `return await _inner.Handle(request, cancellationToken);`;
  - the message becomes `"Handled {RequestName} in {ElapsedMilliseconds}ms"`, with arguments `nameof(CreateProductCommand)` and `stopwatch.ElapsedMilliseconds`.
- **Log Duration off:**
  - both `stopwatch` lines disappear, and `object? response = null;` follows the blank line;
  - the message becomes `"Handled {RequestName} with response {@Response}"`, with arguments `nameof(CreateProductCommand)` and `response`.
- **Log Response and Log Duration off:** no `try`/`finally`. The body is the `Handling` line, a blank line, then `return await _inner.Handle(request, cancellationToken);`.
- **Stereotype removed:** the decorator file is deleted. The `Decorate` line may stay, because `AddApplication` is `[IntentMerge]` and keeps statements it no longer generates. If it stays, ConsumerTestApp no longer builds; delete the line by hand. This is IntentMerge behaviour, not a module bug.
- **Stereotype on `GetProductsQuery`:** nothing is generated, because there is no Query handler to decorate (Scope → Out).

Things to check while verifying:

- ConsumerTestApp builds with `dotnet build ConsumerTestApp/ConsumerTestApp.sln`.
- At startup Scrutor does not throw. If it does, the `Decorate` line was written before the handler registration; report it.
- `POST api/products` still returns **500**, because the preserved hand-written handler body throws `NotImplementedException`.
  - The log shows `Handling CreateProductCommand {…}`, then `Handled CreateProductCommand with response null in <n>ms`. This proves the `finally` runs on a throw.
  - To see a 201 with the `Guid` logged, implement `Handle` in `CreateProductCommandHandler`. `Body = Mode.Ignore` keeps the implementation.
- `DependencyInjection.cs` has exactly one `services.Decorate<…>` line. If there are two, the generated text differs from the hand-written text. Compare them character by character and report it.
