# P2 — CQRS dispatcher module

## 1. Goal

Make WorkshopModule generate the in-memory CQRS slice that ConsumerTestApp currently has written by hand, driven by the Services designer's metadata. Phase A delivers the dispatcher contracts: four Single File C# File Builder templates (`IAppRequest` with `Unit`, `IAppRequestHandler`, `IDispatcher`, `Dispatcher`), plus a scoped `IDispatcher` registration in the Application DI. Phase B makes commands metadata-driven. It installs the Services and CQRS designers into WorkshopModule as metadata only and adds two File Per Model templates over the `Command` element. `CommandRequest` produces the command class and `CommandHandler` produces a handler stub that is registered in DI against `IAppRequestHandler<TCommand, TResponse>`. A `CqrsControllerInstaller` factory extension, together with a hand-written controller model and template registration, makes the ASP.NET Core Controllers module generate one controller per folder that dispatches through `IDispatcher`. A Query gets a controller action only when some template fills the `Application.Query` role for it. At p2-done no template does, so the GET is not generated and ConsumerTestApp builds. When Query templates are added later, the GET appears with no installer change. Phase B ends by modelling a `Products` folder in ConsumerTestApp's Services designer (model only), with one command and one query. The query is there to show the gap left by having no Query templates.

## 2. Starting point

There are two phases, run one after the other in the same session. Phase B starts from Checkpoint A.

**Phase A** starts from the Practical 1 result (WorkshopModule generates the correlation pattern) plus the hand-written CQRS slice that Practical 2's Prompt A just wrote (uncommitted).

What exists at the start:

- `intent/ModuleBuildingTraining.isln` is an Intent Architect solution with three applications: ModuleBuilderAiSkills, WorkshopModule and ConsumerTestApp.
- **WorkshopModule** is a Module Builder application.
  - Its Module Builder designer has one package, `WorkshopModule` (specialization `Intent Module`). The package contains four Single File C# Templates (`ICorrelationContext`, `CorrelationContext`, `CorrelationMiddleware`, `CorrelationDelegatingHandler`) and the `Tracing Settings` Module Settings Configuration, all directly under the package.
  - Its C# project is `WorkshopModule/WorkshopModule/WorkshopModule.csproj` (net8.0). It references `Intent.Modules.Common` 3.7.2, `Intent.Modules.Common.CSharp` 3.8.1, `Intent.SoftwareFactory.SDK` 3.7.0, `Intent.RoslynWeaver.Attributes` 3.0.0 and `Intent.Packager` 3.6.2-pre.0. Its VS solution is `WorkshopModule/ModuleBuildingTraining.sln`.
  - `WorkshopModule/WorkshopModule/WorkshopModule.imodspec` is at version 1.0.0.
  - The P1 template implementations in `WorkshopModule/WorkshopModule/Templates/*/…TemplatePartial.cs` show the house style: `new CSharpFile(this.GetNamespace(), this.GetFolderPath())`, types declared by name in the constructor, everything else in `OnBuild(…, 0)`, and registration requests published from a `BeforeTemplateExecution()` override.
  - Installed modules: Intent.Common, Intent.Common.CSharp, Intent.Common.Types, Intent.Modelers.CodebaseStructure, Intent.ModuleBuilder, Intent.ModuleBuilder.CSharp, Intent.OutputManager.RoslynWeaver, Intent.VisualStudio.Projects. The Services designer modules are **not** installed in WorkshopModule.
  - Building writes `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod`. That folder is the solution's `Workshop` module repository.
- **ConsumerTestApp** is a Clean Architecture .NET 10 app at `ConsumerTestApp/`. WorkshopModule 1.0.0 is installed from the `Workshop` repository.
  - Its projects have these Output Anchor roles: `ConsumerTestApp.Api` has `Distribution` and `Startup`, `ConsumerTestApp.Application` has `Application`, `ConsumerTestApp.Infrastructure` has `Infrastructure`.
  - All projects have `ImplicitUsings` and `Nullable` enabled.
  - Installed and relevant: Intent.Modelers.Services 4.0.16, Intent.Modelers.Services.CQRS 6.0.8, Intent.Metadata.WebApi 4.7.21, Intent.AspNetCore.Controllers 7.1.15, Intent.AspNetCore.Controllers.Dispatch.ServiceContract 5.2.20, Intent.Application.DependencyInjection 4.1.18. Its **API Settings → Default API Security** is `unsecured`.
  - Its Services designer has one package, `ConsumerTestApp.Services`, which contains only a `Security` Security Configuration and the `Default Diagram`. There are no folders, commands or queries.
  - The hand-written slice is the reference output. It has already been adjusted to the shapes the C# File Builder can emit:
    - `Unit.Value` is a static get-only property;
    - long signatures are wrapped one parameter per line;
    - the Dispatcher uses a `CreateInvoker<TResponse>` helper instead of a lambda;
    - the controller route is on the action.
    The files are:
    - `ConsumerTestApp/ConsumerTestApp.Application/Common/Dispatching/IAppRequest.cs` (contains `IAppRequest<out TResponse>` **and** the `Unit` record)
    - `ConsumerTestApp/ConsumerTestApp.Application/Common/Dispatching/IAppRequestHandler.cs`
    - `ConsumerTestApp/ConsumerTestApp.Application/Common/Dispatching/IDispatcher.cs`
    - `ConsumerTestApp/ConsumerTestApp.Application/Common/Dispatching/Dispatcher.cs`
    - `ConsumerTestApp/ConsumerTestApp.Application/Commands/Products/CreateProductCommand.cs`
    - `ConsumerTestApp/ConsumerTestApp.Application/Commands/Products/CreateProductCommandHandler.cs`. It carries no Intent code-management attributes; the user's pattern prompt forbids them in hand-written files.
    - `ConsumerTestApp/ConsumerTestApp.Api/Controllers/ProductsController.cs`
    - The registration lines in `ConsumerTestApp/ConsumerTestApp.Application/DependencyInjection.cs`: `services.AddScoped<IDispatcher, Dispatcher>();` and `services.AddScoped<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandHandler>();`.
  - P2.5 pieces are already in the slice. Read them, but do not reproduce them:
    - `ConsumerTestApp/ConsumerTestApp.Application/Commands/Products/CreateProductCommandLoggingDecorator.cs`;
    - the `services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();` line in `DependencyInjection.cs`;
    - the `Scrutor` 7.0.0 and `Microsoft.Extensions.Logging.Abstractions` 10.0.9 PackageReferences in `ConsumerTestApp.Application.csproj`.

Read these files before Step A1 (and before Step B1 when starting at Phase B). They are the specification, and you only read them. **Where a hand-written file and Section 8 differ, Section 8 wins.** Section 8 is the exact shape the module can generate. Section 8 also lists each difference.

What Checkpoint A adds:

- Four more Single File C# Templates directly under the `WorkshopModule` package: `IAppRequest`, `IAppRequestHandler`, `IDispatcher` and `Dispatcher` (Role `Application`, Default Location `Common/Dispatching`, Templating Method `C# File Builder`).
- Their implemented `Templates/<Name>/<Name>TemplatePartial.cs` files, and the generated `GetIAppRequestName()`, `GetIAppRequestHandlerName()`, `GetIDispatcherName()` and `GetDispatcherName()` extensions in `Templates/TemplateExtensions.cs`.
- `DispatcherTemplate` publishes the scoped `IDispatcher` registration.

## 3. Skills to load

Load these before any other step by reading `.claude/skills/<name>/SKILL.md` and the resources listed. Use them instead of decompiling NuGet packages.

- `file-builder-expert`: read `resources/api-cheatsheet.md`, `resources/builder-patterns.md`, `resources/troubleshooting.md` and `resources/patterns/*.cs`.
- `intent-module-orchestrator`: read `resources/orchestration-cheatsheet.md`, sections §DI Registration, §Resolution, §The Model Bridge, §Two-Tier Module Dependency and §Factory Extension.
- `intent-modelers-integration`: read `resources/integration-recipe.md`, `resources/services.md` (§2, §3, §9) and `resources/services-extensions.md` (§1, §2.1, §3). *(Phase B)*
- `intent-dotnet-module-integration`: read `resources/aspnetcore-controllers.md` in full, especially §2, §5, §6, §7 and §9 (Traps). *(Phase B)*
- `working-with-modules` *(Phase B, Step B1)*
- `scripting-the-model`: required before `run_designer_query` / `run_designer_script`. Read `references/elements.md`, `references/stereotypes.md` and `references/types.md`.
- `exploring-the-model`
- `changing-the-model`
- `generating-and-applying-code`
- `resolve-destructive-changes`: load it only if `run_software_factory` reports a change as destructive "yes" or "unknown".

Every Intent Architect MCP tool call needs the `intention` parameter (10 words or fewer).

## 4. Scope

**In**

- WorkshopModule Module Builder metadata: four Single File C# Templates (Phase A); two File Per Model C# Templates and one Factory Extension (Phase B). Each is listed in Step A3 and Step B3.
- Installing Intent.Modelers.Services 4.0.16 and Intent.Modelers.Services.CQRS 6.0.8 into **WorkshopModule**, metadata only (Step B1).
- Running the Software Factory on **WorkshopModule only**, and applying its changes.
- Bespoke code in WorkshopModule:
  - the constructor body of each of the six generated `*TemplatePartial.cs` files, plus an added `BeforeTemplateExecution()` override in `DispatcherTemplatePartial.cs` and `CommandHandlerTemplatePartial.cs`;
  - the body of `FactoryExtensions/CqrsControllerInstaller.cs` (an added `OnAfterTemplateRegistrations` override);
  - two new hand-written files: `Templates/CqrsController/CqrsControllerModel.cs` and `Templates/CqrsController/CqrsControllerTemplateRegistration.cs`;
  - hand edits to `WorkshopModule.csproj` (one PackageReference plus three version pins) and `WorkshopModule.imodspec` (one `<dependency>`).
- Modelling, **model only**, a `Products` folder with `CreateProductCommand` and `GetProductsQuery` in ConsumerTestApp's Services designer (Step B11). This is the only change of any kind to ConsumerTestApp.
- Building `WorkshopModule/ModuleBuildingTraining.sln`.

**Out**

- Never run the Software Factory on ConsumerTestApp.
- Never hand-edit files owned by other modules. That includes ConsumerTestApp's `DependencyInjection.cs`, every ConsumerTestApp code file, `modules.config` in any application, and every generated file in WorkshopModule outside the bodies and files listed under In.
- Any change to ConsumerTestApp other than the Step B11 model. The hand-written slice stays exactly as it is.
- Query templates (no `QueryRequest`/`QueryHandler`, nothing in the `Application.Query` role). `GetProductsQuery` is modelled only to show the gap. Decision 40 keeps it off the controller until such a template exists.
- The logging decorator, `services.Decorate<…>`, Scrutor and `Microsoft.Extensions.Logging.Abstractions` (P2.5).
- Validation, unit of work, `[Authorize]`/security handling, API versioning, file transfer, and Commands or Queries that sit directly in a package rather than in a folder.
- Changing WorkshopModule's version. It stays 1.0.0.
- Decompiling or reflecting over any NuGet package or module assembly.
- Release notes, docs, icons, migrations, and tests.
- Anything inside `.intent` or `Intent.Metadata` folders, and any `.application.config` file. Change models only through `run_designer_script`.
- Committing or tagging. The facilitator tags `p2-start` (after Phase A) and `p2-done` (after Phase B).

## 5. Decisions already made — do not ask

### Facilitator decisions (the user specified these)

1. **Phase A templates:** four Single File C# File Builder templates, `IAppRequest`, `IAppRequestHandler`, `IDispatcher` and `Dispatcher`, all with Role `Application` and Default Location `Common/Dispatching`. They reproduce the hand-written files.
2. **Dispatcher registration:** `Dispatcher` is registered **scoped** against `IDispatcher` in the Application DI, via a registration request.
3. **Unit is a record**, because the C# File Builder can't declare structs.
4. **Generic variance** uses the builder's `Covariant()` / `Contravariant()`. Never pass a parameter handle to `AddGenericTypeConstraint` (it emits `where in TRequest`, CS1960).
5. **Phase B designer install:** Intent.Modelers.Services 4.0.16 and Intent.Modelers.Services.CQRS 6.0.8 go into WorkshopModule as metadata only: designer metadata on; designers, factory extensions, template outputs and settings off.
6. **Phase B templates:** File Per Model C# File Builder templates over the Services designer's `Command` element. `CommandRequest` has Role `Application.Command` and `CommandHandler` has Role `Application`. Both have Default Location `Commands` and Model Name `CommandModel`. There is also a Factory Extension named `CqrsControllerInstaller`.
7. **Set Model Name explicitly to `CommandModel`.** Setting the Model Type from a script leaves it as `object`.
8. **Do all type resolution and member generation inside `OnBuild`**, never before `CSharpFile` is assigned.
9. **CommandRequest:** a class per Command with a property per field, implementing `IAppRequest<TResponse>`. `TResponse` is the command's return type, or `Unit` when it has none.
10. **CommandHandler:** a handler per Command implementing `IAppRequestHandler<TCommand, TResponse>`. Its `Handle` throws `NotImplementedException` and is left for the developer to fill in. The handler is registered in the Application DI against that interface.
11. **Register handlers against `IAppRequestHandler<TCommand, TResponse>`** (`ForInterface`), never the concrete class, so the Dispatcher can resolve them and a decorator can wrap them later.
12. **`Handle` is `[IntentManaged(Mode.Fully, Body = Mode.Ignore)]`.** The Software Factory owns the signature, the developer owns the body.
13. **Controller integration:**
    - Add the `Intent.Modules.AspNetCore.Controllers` 7.1.15 NuGet package to `WorkshopModule.csproj`, and a matching `<dependency id="Intent.AspNetCore.Controllers" version="7.1.15" />` to `WorkshopModule.imodspec`.
    - Register the Controllers module's `ControllerTemplate` with our own `IControllerModel` (one controller per folder) for Commands and Queries that have HTTP settings.
    - `CqrsControllerInstaller` injects `IDispatcher` into those controllers. Each action does `var result = await _dispatcher.Send(command, cancellationToken);` and returns the template's `GetReturnStatement(operation)`.
14. **Use `GetReturnStatement`**, never a hard-coded `Ok(result)`.
15. **The template registration and `IControllerModel` are hand-written classes.** The Module Builder's Template Registration element can't register another module's template.
16. **Use `intent-dotnet-module-integration` for the controller integration**: the typed dependency, the `result` variable `GetReturnStatement` expects, and multi-host duplication. Do not decompile packages.
17. **The `.imodspec` dependency on Controllers is added by hand.** Only designer package references become module dependencies, and a hand-added one survives regeneration.
18. **If NuGet reports NU1605**, pin `Intent.SoftwareFactory.SDK` 3.14.0, `Intent.Modules.Common` 3.11.6 and `Intent.Modules.Common.CSharp` 3.10.11 in `WorkshopModule.csproj`.
19. **No Query templates.** `GetProductsQuery` is modelled only to show the gap.
20. **ConsumerTestApp model (Step B11):** a `Products` folder containing `CreateProductCommand` (Title: string, Price: decimal) returning `guid`, exposed as HTTP POST `api/products`, and `GetProductsQuery`, returning a list of strings, exposed as HTTP GET `api/products`. Model only; do not run the Software Factory on ConsumerTestApp.

### Plan-author decisions (derived from the hand-written pattern, the loaded skills, and a check of the referenced package versions; not facilitator decisions)

21. **One type per template, except `IAppRequest`.** The hand-written `IAppRequest.cs` declares two types, `IAppRequest<out TResponse>` and `Unit`, and the scope has only four Phase A templates. So the IAppRequest template emits both into its one file. `IAppRequest` is declared **first**, so `CSharpFile.GetConfig()` reports `ClassName = IAppRequest` and `GetIAppRequestName()` resolves to it. (This mirrors P1's CorrelationMiddleware.)
22. **`Unit.Value` is a static get-only property**, `public static Unit Value { get; } = new();`, as in the hand-written file. A `static readonly` field is not an option: in `Intent.Modules.Common.CSharp` 3.8.1, `CSharpField` has no public read-only modifier. Build it as `AddProperty("Unit", "Value", p => p.Static().WithoutSetter().WithInitialValue("new()"))` on a `Sealed()` record.
23. **Constructor vs OnBuild.** This is the P1 rule, and it implements decision 8. The constructor assigns `CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())` exactly as scaffolded, and declares each type **by name only**:
    - `AddInterface("…")` / `AddClass("…")` / `AddRecord("Unit")`;
    - for the File Per Model templates, `AddClass(Model.Name)` and `AddClass($"{Model.Name}Handler")`.
    It then registers `.OnBuild(file => { … }, 0)`. Everything else happens inside that `OnBuild`: members, generic parameters and constraints, base types and interfaces, `UseType`/`GetTypeName`/`Get…Name()` resolution. `this.GetFolderPath()` already respects the model's designer folders, so commands land in `Commands/Products`. Do not add `additionalFolders`.
24. **Type references.**
    - Reference other WorkshopModule templates only through the generated extension methods (`this.GetIAppRequestName()`, `this.GetIAppRequestHandlerName()`, `this.GetIDispatcherName()`, `this.GetCommandRequestName(Model)`) or `TemplateId` constants. Never use a hard-coded namespace or `ConsumerTestApp`.
    - Close generics by interpolation, for example `$"{this.GetIAppRequestName()}<TResponse>"`.
    - `Unit` is written as the bare name `Unit`. Every file that uses it already has the `Common.Dispatching` using through `GetIAppRequestName()`/`GetIAppRequestHandlerName()`.
    - Resolve model types with `GetTypeName(field.TypeReference)` / `GetTypeName(Model.TypeReference)`.
    - Resolve framework types in type positions (field, parameter and return types) through `UseType("Namespace.Type")`, for example `UseType("System.Threading.Tasks.Task")`, `UseType("System.Threading.CancellationToken")`, `UseType("System.IServiceProvider")` and `UseType("System.Collections.Concurrent.ConcurrentDictionary<System.Type, object>")`.
    - The only `AddUsing` is `file.AddUsing("Microsoft.Extensions.DependencyInjection")` in the Dispatcher. `GetRequiredService` is an extension method, and `file-builder-expert` Must-Not 7 allows `AddUsing` for that.
25. **Generic parameters.**
    - Interfaces: `@interface.AddGenericParameter("TResponse", p => p.Covariant())` and `AddGenericParameter("TRequest", p => p.Contravariant())`. In 3.8.1, `Covariant()`/`Contravariant()` exist only on `CSharpInterfaceGenericParameter`.
    - Classes and methods: `AddGenericParameter("TResponse")`.
    - Constraints always use the **name string**: `AddGenericTypeConstraint("TRequest", c => c.AddType($"{this.GetIAppRequestName()}<TResponse>"))` (decision 4).
26. **Long signatures wrap.** The builder puts each parameter on its own line once a declaration exceeds its line-length limit. This applies to `Dispatcher.Send` and both `HandlerInvoker.Invoke` declarations; the hand-written file is already wrapped the same way. There is no API to change it. `IDispatcher.Send` and `IAppRequestHandler.Handle` stay on one line.
27. **Dispatcher shape: no lambda.**
    - `Send` calls `Invokers.GetOrAdd(request.GetType(), CreateInvoker<TResponse>)`.
    - The `private static object CreateInvoker<TResponse>(Type requestType)` helper sits between `Send` and the nested classes, as in the hand-written file.
    - Never emit a lambda here: `file-builder-expert` forbids raw `=>` strings.
28. **Blank lines.** Match Section 8 exactly, using the separator API (`SeparatedFromPrevious()`, `IntroduceReadonlyField(f => f.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines)`, `nested.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines`) rather than raw text. Defaults that already match need nothing:
    - fields render before properties;
    - the members of a class are separated by one blank line;
    - `IntroduceProperty()` properties follow the constructor after one blank line and have no blank line between them.
    Two places need a separator:
    - the second nested `HandlerInvoker` class needs `BeforeSeparator = EmptyLines`;
    - `Dispatcher._serviceProvider` needs `BeforeSeparator = EmptyLines` so it sits apart from the static field.
29. **Registrations are published from the templates**, in a `public override void BeforeTemplateExecution()` that calls `base.BeforeTemplateExecution()` and then `ExecutionContext.EventDispatcher.Publish(...)`. This is the P1 pattern.
    - `DispatcherTemplate`: `ContainerRegistrationRequest.ToRegister(this).ForInterface(GetTemplate<IClassProvider>(IDispatcherTemplate.TemplateId)).ForConcern("Application").WithPerServiceCallLifeTime().WithPriority(10)`
    - `CommandHandlerTemplate`: `ContainerRegistrationRequest.ToRegister(this).ForInterface($"{this.GetIAppRequestHandlerName()}<{this.GetCommandRequestName(Model)}, {response}>").ForConcern("Application").WithPerServiceCallLifeTime().RequiresUsingNamespaces(GetTemplate<IClassProvider>(IAppRequestHandlerTemplate.TemplateId).Namespace).HasDependency(GetTemplate<ITemplate>(IAppRequestHandlerTemplate.TemplateId)).WithPriority(20)`. `{response}` is computed exactly as in decision 31.
    - Why these values:
      - `ForConcern("Application")` targets the Application `DependencyInjection.cs`.
      - `WithPerServiceCallLifeTime()` gives `AddScoped`, as hand-written.
      - Priorities 10 and 20 keep the hand-written order: `IValidatorProvider`, then `IDispatcher`, then handlers.
      - The handler's interface is a closed generic string, so its namespace is supplied explicitly. `ToRegister(this)` supplies the handler's own namespace, which is also the command's.
30. **CommandRequest shape** (`file.Classes[0]`, inside `OnBuild`):
    - `ImplementsInterface($"{this.GetIAppRequestName()}<{response}>")`.
    - One `AddConstructor(ctor => …)`. For each `field` in `Model.Properties`, in model order, call `ctor.AddParameter(GetTypeName(field.TypeReference), field.Name.ToParameterName(), p => p.IntroduceProperty())`. This yields `public string Title { get; set; }` plus the `Title = title;` assignment.
    - A command with no fields gets an empty constructor.
31. **Response type.**
    - `Model.TypeReference.Element != null` → `GetTypeName(Model.TypeReference)` (for `guid` this gives `Guid`);
    - otherwise → `Unit`.
    The CommandRequest and CommandHandler templates use the same expression.
32. **CommandHandler shape** (`file.Classes[0]`, inside `OnBuild`):
    - `ImplementsInterface($"{this.GetIAppRequestHandlerName()}<{this.GetCommandRequestName(Model)}, {response}>")`.
    - An empty `AddConstructor()`.
    - `AddMethod($"{UseType("System.Threading.Tasks.Task")}<{response}>", "Handle", m => …)`:
      - `m.AddAttribute("IntentManaged", a => a.AddArgument("Mode.Fully").AddArgument("Body = Mode.Ignore"))`.
      - `m.Async()`. Without `async`, a developer who adds `await` would have the managed signature put back without it.
      - Parameters `request` (`this.GetCommandRequestName(Model)`) and `cancellationToken` (`UseType("System.Threading.CancellationToken")`).
      - The body is exactly the three statements `file-builder-expert` Must 11 prescribes:
        - `// IntentInitialGen`
        - `// TODO: Implement Handle ({ClassName}) functionality`
        - `throw new NotImplementedException("Your implementation here...");`
    - Assumption, stated because the facilitator did not specify the message: the skill's three-line body outranks the hand-written exception message. Only a **new** handler file gets this body. `Body = Mode.Ignore` keeps the body already on disk, so the existing `CreateProductCommandHandler` keeps its hand-written `throw` (Section 8).
    - The `async` method with no `await` produces a CS1998 *warning* in the consumer. This is accepted; Intent's own handler stubs do the same, and ConsumerTestApp does not treat warnings as errors.
    - The hand-written handler carries no `[IntentManaged]` attribute and is not `async`; the user's pattern prompt forbids code-management attributes in hand-written files. The generated file adds both, and keeps the hand-written body.
33. **Template Settings for the File Per Model templates** (decisions 6 and 7):
    - `Source` = `Lookup Type`;
    - `Designer` = the `Services` designer reference;
    - `Model Type` = the CQRS `Command` element settings;
    - `Model Name` = `CommandModel`, set explicitly.
    The Software Factory must then generate `FilePerModelTemplateRegistration<CommandModel>` with `using Intent.Modelers.Services.CQRS.Api;` and a `GetModels` that reads the Services designer's Command models. If it generates `object`, fix the model, never the generated registration.
34. **Controller model** (`Templates/CqrsController/CqrsControllerModel.cs`, hand-written, namespace `WorkshopModule.Templates.CqrsController`):
    - `public class CqrsControllerModel : IControllerModel`, with constructor `(IElement folderElement, IEnumerable<IHttpEndpointModel> endpoints)`.
    - Member values:
      - `Id` = `folderElement.Id`
      - `Name` = `folderElement.Name`. Controllers names the class `ProductsController`.
      - `Folder` = `folderElement.ParentElement?.AsFolderModel()`. This is the *parent* of the grouping folder, as with the reference `CqrsControllerModel` (aspnetcore-controllers §6.2/§9), so the controller lands in `Controllers/`, not `Controllers/Products/`.
      - `Route` = `null`
      - `Comment` = `null`
      - `RequiresAuthorization` = `false`
      - `AllowAnonymous` = `false`
      - `SecurityModels` = an empty `ISecurityModel[]`
      - `ApplicableVersions` = an empty list
      - `InternalElement` = `folderElement`
      - `Operations` = `endpoints.Select(e => (IControllerOperationModel)new ControllerOperationModel(e, new List<IApiVersionModel>(), this)).ToList()`. This holds every Command **and** Query endpoint in the folder. It is a mutable `List<IControllerOperationModel>`, which the installer prunes (decision 40).
    - `ControllerOperationModel` is the public Controllers class (`Intent.Modules.AspNetCore.Controllers.Templates.Controller.Models`). Its public constructor `(IHttpEndpointModel httpEndpoint, IList<IApiVersionModel> applicableVersions, IControllerModel controller)` builds the operation's parameters (including the synthetic `command`/`query` body parameter, §6.4) from the endpoint. Do not write your own operation or parameter model classes.
35. **Template registration** (`Templates/CqrsController/CqrsControllerTemplateRegistration.cs`, hand-written):
    - `public class CqrsControllerTemplateRegistration : FilePerModelTemplateRegistration<IControllerModel>`, with a constructor `(IMetadataManager metadataManager)`.
    - `TemplateId => ControllerTemplate.TemplateId`.
    - `CreateTemplateInstance(outputTarget, model) => new ControllerTemplate(outputTarget, model)`.
    - `GetModels(IApplication application)`:
      - Take the `InternalElement` of every `_metadataManager.Services(application).GetCommandModels()` and `.GetQueryModels()`.
      - Keep those for which `application.TryGetHttpEndpoint(element, null, out var endpoint)` is true. The extension is `Intent.Modules.Metadata.WebApi.Models.HttpEndpointExecutionContextExtensions`, and it honours the app's Default API Security; ConsumerTestApp is unsecured, so no `[Authorize]` is emitted.
      - Keep those whose `InternalElement.ParentElement?.SpecializationTypeId == FolderModel.SpecializationTypeId`. Elements not in a folder are skipped.
      - Group by parent folder id, order each group by `InternalElement.Order`, and return one `CqrsControllerModel` per folder.
      - Do **not** filter Queries here. `GetModels` runs during template registration, when another module's `Application.Query` templates may not be registered yet. Decision 40 does that filtering later.
    - The `.imodspec` gets no `<template>` entry for this (aspnetcore-controllers §7).
36. **Installer** (`FactoryExtensions/CqrsControllerInstaller.cs`): override `protected override void OnAfterTemplateRegistrations(IApplication application)`.
    - Find the controllers with `application.FindTemplateInstances<IControllerTemplate<IControllerModel>>(ControllerTemplate.TemplateId)`. Use the string overload: the `ITemplateDependency` overloads are ambiguous with an obsolete twin. Keep only templates with `template.Model is CqrsControllerModel`. Loop over **all** instances (multi-host, §9). Never call `FindTemplateInstance` for a single one.
    - For each, **first** prune the model's Query operations as decision 40 says. Do this once per distinct `CqrsControllerModel`, because multi-host instances may share one model.
    - **Then** register `template.CSharpFile.AfterBuild(file => { … }, 500)`:
      - `var @class = file.Classes.First();`.
      - On `@class.Constructors.First()`, add `template.GetIDispatcherName()` named `dispatcher` with `IntroduceReadonlyField()`, guarded so it is added only if no parameter is already named `dispatcher`.
      - For each `method` in `@class.Methods` with `method.TryGetMetadata<IControllerOperationModel>("model", out var operation)`:
        - **Argument.** Find the payload parameter: the `method.Parameters` entry whose `TryGetMetadata<IControllerParameterModel>("model", out var p)` gives `p.Id == operation.Id`. For a POST/PUT/PATCH, Controllers folds the fields into this body parameter, named `command` for a Command. Use its name as the argument, which gives decision 13's `Send(command, cancellationToken)`.
        - If there is **no** payload parameter, construct the request instead. This happens for a GET/DELETE, where each field is its own action parameter. It never happens at p2-done, because the only such operation, `GetProductsQuery`, is pruned. It is needed so a gated-in Query compiles with no installer change.
          - Resolve the type with `template.TryGetTypeName(new[] { "Application.Command", "Application.Query" }, operation.InternalElement.Id, out var requestType)`. If that fails, throw `InvalidOperationException` naming the element.
          - The argument is `new {requestType}({args})`. `{args}` is the names of the method's parameters that carry `IControllerParameterModel` metadata, in order, comma-separated (the cancellation token is excluded).
          - This assumes the request class takes its fields as constructor parameters in field order, as `CommandRequest` does. A Query template must do the same.
          - Plan-author assumption: decision 13's literal `command` does not exist in such an action, so it cannot be used there.
        - `method.AddStatement($"var result = await _dispatcher.Send({argument}, cancellationToken);");`. The variable must be named `result` (§9).
        - `method.AddStatement(template.GetReturnStatement(operation));`.
        - If the second `AddStatement` raises CS0618, use the non-obsolete overload `file-builder-expert` Must-Not 2 names.
    - Correlate only through metadata, never by name.
37. **NuGet and module identities.**
    - Restore will report NU1605: Controllers 7.1.15 requires Common ≥ 3.11.0, Common.CSharp ≥ 3.10.10 and SDK ≥ 3.14.0. So decision 18 applies. Change the three existing PackageReference versions in place to 3.11.6 / 3.10.11 / 3.14.0. P1 code builds cleanly with these pins.
    - The Controllers PackageReference id is `Intent.Modules.AspNetCore.Controllers`. Its `.imodspec` id is `Intent.AspNetCore.Controllers`, with no `Modules.`.
    - `Intent.Modules.Metadata.WebApi`, `Intent.Modules.Metadata.Security` and `Intent.Modules.Common.Types` arrive transitively. Do not add them.
38. **Module install flags.** `install_or_update_modules` enables every omitted flag, so pass all five explicitly on each module:
    - `installDesignerMetadata: true`
    - `installDesigners: false`
    - `enableFactoryExtensions: false`
    - `installApplicationSettings: false`
    - `installTemplateOutputs: false`
39. **Layout.** All six templates and the factory extension sit directly under the `WorkshopModule` package, not in a folder.

### Orchestrator decisions (made by the orchestrator under the user's delegation to approve plans; not user decisions)

40. **Query operations are gated on an `Application.Query` template.** Controllers are still registered for Commands *and* Queries that have HTTP settings (decision 13). `GetProductsQuery` stays modelled with its HTTP settings (decision 20). But a controller keeps a Query operation only when some template in the execution context fills the `Application.Query` role for that query.
    - **Where.** The check runs in `CqrsControllerInstaller.OnAfterTemplateRegistrations`, before the `AfterBuild` callback is registered (decision 36). This is the first point at which every module's templates are registered. `ControllerTemplate` builds its action methods from `Model.Operations` when its file is built, which is later.
    - **How.**
      - For each distinct `CqrsControllerModel`, remove from `Operations` every operation where:
        - `operation.InternalElement.IsQueryModel()` (`Intent.Modelers.Services.CQRS.Api`), and
        - `!application.TemplateExists("Application.Query", operation.InternalElement.Id)`.
      - `TemplateExists(string templateIdOrRole, string metadataModelId)` is a member of `ISoftwareFactoryExecutionContext` in `Intent.SoftwareFactory.SDK` 3.14.0, verified against the package's public API. `IApplication` implements that interface. It is the closest API to `FindTemplateInstance(s)` by role, and it is scoped to the query, because the action needs that query's own type.
      - Commands are never pruned.
    - **Effect.**
      - At p2-done no template fills `Application.Query`. So `ProductsController` has only `CreateProduct`, and ConsumerTestApp builds.
      - When an attendee adds Query templates with Role `Application.Query` for the Stretch Exercise, `GetProducts` appears with no installer change. Its request is constructed as decision 36 describes.
      - A folder whose only HTTP operations are gated-out Queries gets a controller with no actions. This is accepted.

## 6. Steps

### Phase A — Dispatcher contracts

**A1. Read the reference.** Read the eight ConsumerTestApp files listed in Section 2, plus `ConsumerTestApp/ConsumerTestApp.Application/DependencyInjection.cs`. Also read `WorkshopModule/WorkshopModule/Templates/CorrelationContext/CorrelationContextTemplatePartial.cs` and `.../CorrelationMiddleware/CorrelationMiddlewareTemplatePartial.cs` for the house style. Do not modify any of them.

**A2. Explore the designer.**
- Call `get_applications` to get the ids of the WorkshopModule application and its `Module Builder` designer.
- With `run_designer_query`, call `getDesignerSchema()` and `getDesignerModelStructure()`.
- Confirm:
  - The `WorkshopModule` package contains the four P1 templates and `Tracing Settings`, and nothing named `IAppRequest`, `IAppRequestHandler`, `IDispatcher` or `Dispatcher`.
  - The `C# Template` element type, the `Single File` type reference, and the stereotypes `C# Template Settings` (`Templating Method`) and `Template Settings` (`Role`, `Default Location`) are available.
- Check: the query result shows all of the above. If any Phase A template already exists, stop and report.

**A3. Model the four templates.** Use one `run_designer_script` that creates four `C# Template` elements directly under the `WorkshopModule` package. For each, set the type reference to `Single File`, set `C# Template Settings` → `Templating Method` = `C# File Builder`, and set these `Template Settings` values:

| Template name | Role | Default Location |
|---|---|---|
| `IAppRequest` | `Application` | `Common/Dispatching` |
| `IAppRequestHandler` | `Application` | `Common/Dispatching` |
| `IDispatcher` | `Application` | `Common/Dispatching` |
| `Dispatcher` | `Application` | `Common/Dispatching` |

Check: run `getElementDetails` on each of the four with `run_designer_query`. Each must show the type `Single File`, Templating Method `C# File Builder`, and the Role and Default Location above.

**A4. Validate.** Run `getDesignerValidationErrors()` with `run_designer_query`. It must return zero errors. If it does not, fix the model with a follow-up `run_designer_script` and check again.

**A5. Generate WorkshopModule.**
- Call `run_software_factory` for the **WorkshopModule** application. Review the staged changes with `get_file_diffs`, then apply them with `apply_staged_file_changes`, passing exactly the paths reviewed. Follow `generating-and-applying-code`. If a change is reported destructive "yes" or "unknown", follow `resolve-destructive-changes`.
- Expected changes:
  - New files in `WorkshopModule/WorkshopModule/Templates/<Name>/`: `<Name>TemplatePartial.cs` and `<Name>TemplateRegistration.cs` for each of the four templates.
  - `Templates/TemplateExtensions.cs` gains `GetIAppRequestName()`, `GetIAppRequestHandlerName()`, `GetIDispatcherName()` and `GetDispatcherName()`.
  - `WorkshopModule.imodspec` gains four `<template>` entries.
- Check: all of these files and entries exist. Record each template's `TemplateId` constant (`WorkshopModule.IAppRequest`, …). Use the constants, never the literal strings.

**A6. Build.** Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` from the solution root. It must succeed.

These rules apply to every template in A7–A10 and B7–B8:
- Edit only the constructor body of `<Name>TemplatePartial.cs`, which is generated `[IntentManaged(Mode.Fully, Body = Mode.Ignore)]`, plus the `BeforeTemplateExecution()` override where a step says so.
- Keep the generated `CSharpFile`, `DefineFileConfig()` and `TransformText()` members exactly as generated.
- Follow decisions 21–40.
- Build with warnings visible after each template. Treat any `CS0618` "obsolete" warning as a failure.
- Section 8 has the exact expected output for each template.

**A7. IAppRequestTemplate** (`Templates/IAppRequest/IAppRequestTemplatePartial.cs`)
- Constructor: `AddInterface("IAppRequest")`, then `AddRecord("Unit")`, then `OnBuild(…, 0)`. The order matters (decision 21).
- In `OnBuild`:
  - `file.Interfaces[0].AddGenericParameter("TResponse", p => p.Covariant())`.
  - On `file.Records[0]`: `Sealed()` and the `Value` property from decision 22.
- Check: build is green.

**A8. IAppRequestHandlerTemplate** (`Templates/IAppRequestHandler/IAppRequestHandlerTemplatePartial.cs`)
- Constructor: `AddInterface("IAppRequestHandler")`, then `OnBuild(…, 0)`.
- In `OnBuild`:
  - Generic parameters `TRequest` (`Contravariant()`) and `TResponse`.
  - Constraint `AddGenericTypeConstraint("TRequest", c => c.AddType($"{this.GetIAppRequestName()}<TResponse>"))`.
  - Method `Handle`, returning `$"{UseType("System.Threading.Tasks.Task")}<TResponse>"`, with parameters `TRequest request` and `CancellationToken cancellationToken`.
- Check: build is green, and `grep -n "where in" -r WorkshopModule/WorkshopModule/Templates` returns nothing.

**A9. IDispatcherTemplate** (`Templates/IDispatcher/IDispatcherTemplatePartial.cs`)
- Constructor: `AddInterface("IDispatcher")`, then `OnBuild(…, 0)`.
- In `OnBuild`, add the method `Send`:
  - return type `Task<TResponse>`;
  - method generic parameter `TResponse`;
  - parameter `$"{this.GetIAppRequestName()}<TResponse>" request`;
  - parameter `CancellationToken cancellationToken` with `WithDefaultValue("default")`.
- Check: build is green.

**A10. DispatcherTemplate** (`Templates/Dispatcher/DispatcherTemplatePartial.cs`)
- Constructor: `AddClass("Dispatcher")`, then `OnBuild(…, 0)`.
- In `OnBuild`, add the using, base interface and members in this order. Section 8 has the exact text.
  1. `file.AddUsing("Microsoft.Extensions.DependencyInjection")` and `ImplementsInterface(this.GetIDispatcherName())`.
  2. Field `Invokers`: `PrivateReadOnly().Static()`, `WithAssignment(new CSharpStatement("new()"))`.
  3. A constructor with `IServiceProvider serviceProvider` → `IntroduceReadonlyField(f => f.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines)`.
  4. The `Send` method. Its body:
     - `ArgumentNullException.ThrowIfNull(request);`
     - the `var invoker = …GetOrAdd(request.GetType(), CreateInvoker<TResponse>);` statement, `SeparatedFromPrevious()`;
     - `return invoker.Invoke(request, _serviceProvider, cancellationToken);`, `SeparatedFromPrevious()`.
  5. The `CreateInvoker<TResponse>` method, `Private().Static()` (decision 27).
  6. The nested class `HandlerInvoker` with `TResponse`: `Private().Abstract()`, with an `Abstract()` `Invoke` method.
  7. The nested class `HandlerInvoker` with `TRequest, TResponse`: `Private().Sealed()`, `BeforeSeparator = EmptyLines`. It has base type `HandlerInvoker<TResponse>`, a name-string constraint on `TRequest`, and an `Override()` `Invoke` whose `GetRequiredService` uses `this.GetIAppRequestHandlerName()`.
- Add the `BeforeTemplateExecution()` override that publishes the `IDispatcher` registration (decision 29).
- Check: build is green.

**A11. Verify Phase A.**
- Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental`. It must finish with 0 errors and no `CS0618`.
- Run `run_software_factory` on **WorkshopModule** again, then `get_file_diffs`. It must propose zero changes. If it proposes removing a `BeforeTemplateExecution()` override, do not apply it. Protect the override with `[IntentManaged(Mode.Ignore)]`, then rebuild and re-run until zero changes are proposed.
- `grep -rn "ConsumerTestApp" WorkshopModule/WorkshopModule` returns nothing.

**Checkpoint A:** the model has the four Phase A templates with zero validation errors. Their implementations are applied and the build is green. A fresh Software Factory run on WorkshopModule proposes zero changes. The facilitator tags `p2-start` here, and Phase B starts from it.

### Phase B — Metadata-driven commands and controller integration

If you start here from `p2-start`, first do A1 and confirm Checkpoint A: build green, Software Factory zero changes.

**B1. Install the Services designers into WorkshopModule.** Follow `working-with-modules`. Make one `install_or_update_modules` call on the **WorkshopModule** application with:
- `Intent.Modelers.Services` 4.0.16
- `Intent.Modelers.Services.CQRS` 6.0.8

Pass all five flags on both modules, as decision 38 requires.

Check: `run_solution_query` → `listInstalledModules(<WorkshopModule id>)` lists both modules at those versions. Never open or edit `modules.config`.

**B2. Explore the designer.** With `run_designer_query`, call `getDesignerSchema()` and look up the `Template Settings` stereotype (the one applicable to `Single File` / `File Per Model` / `Custom`). Confirm:
- Its `Designer` property can reference a `Services` designer.
- Its `Model Type` property can reference the CQRS `Command` element settings.
- The `File Per Model` type reference and the `Factory Extension` element type exist.

If the `Command` element settings are not visible with designer metadata alone, re-run B1 with `installDesigners: true` for the two modules only (`intent-modelers-integration` Must 7), and note it in the final report.

**B3. Model the Phase B elements.** Use one `run_designer_script`, directly under the `WorkshopModule` package:
- Two `C# Template` elements with the type reference `File Per Model`, `C# Template Settings` → `Templating Method` = `C# File Builder`, and these `Template Settings` values:

| Template name | Source | Designer | Model Type | Model Name | Role | Default Location |
|---|---|---|---|---|---|---|
| `CommandRequest` | `Lookup Type` | `Services` | `Command` | `CommandModel` | `Application.Command` | `Commands` |
| `CommandHandler` | `Lookup Type` | `Services` | `Command` | `CommandModel` | `Application` | `Commands` |

- One `Factory Extension` element named `CqrsControllerInstaller`.

Check: `getElementDetails` shows these values on both templates, with `Model Name` = `CommandModel` (decision 7). Then run `getDesignerValidationErrors()`. It must return zero errors.

**B4. Generate WorkshopModule.**
- Run `run_software_factory` → `get_file_diffs` → `apply_staged_file_changes`, as in A5.
- Expected:
  - `Templates/CommandRequest/CommandRequestTemplatePartial.cs` + `…Registration.cs`, and the same for `CommandHandler`. The partial classes derive from `CSharpTemplateBase<CommandModel>`. The registrations derive from `FilePerModelTemplateRegistration<CommandModel>` and read `GetCommandModels()` from the Services designer.
  - `FactoryExtensions/CqrsControllerInstaller.cs`.
  - `TemplateExtensions.cs` gains `GetCommandRequestName(…)` and `GetCommandHandlerName(…)` overloads that take a `CommandModel`.
  - `WorkshopModule.imodspec` gains two `<template>` entries and a `<factoryExtension>` entry. It also gains dependencies on `Intent.Modelers.Services` and `Intent.Modelers.Services.CQRS`.
  - The Software Factory may also add the matching `Intent.Modules.Modelers.Services` / `Intent.Modules.Modelers.Services.CQRS` PackageReferences to `WorkshopModule.csproj`.
- Check: the registrations are typed on `CommandModel`, not `object`. If they use `object`, fix `Model Name`/`Model Type` in the model (B3) and regenerate. Never edit the generated registration.

**B5. NuGet and imodspec by hand.**
- In `WorkshopModule/WorkshopModule/WorkshopModule.csproj`:
  - Add `<PackageReference Include="Intent.Modules.AspNetCore.Controllers" Version="7.1.15" />`.
  - If B4 did not add them, also add `Intent.Modules.Modelers.Services` 4.0.16 and `Intent.Modules.Modelers.Services.CQRS` 6.0.8.
  - Run `dotnet restore WorkshopModule/ModuleBuildingTraining.sln`. It reports NU1605 (decision 37), so set `Intent.Modules.Common` to 3.11.6, `Intent.Modules.Common.CSharp` to 3.10.11 and `Intent.SoftwareFactory.SDK` to 3.14.0, editing the existing three PackageReferences in place.
- In `WorkshopModule/WorkshopModule/WorkshopModule.imodspec`, inside `<dependencies>`, add `<dependency id="Intent.AspNetCore.Controllers" version="7.1.15" />`.
- Check: `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` succeeds with no `CS0618`. The P1 and Phase A code is unchanged.

**B6. Re-run the Software Factory once** on WorkshopModule and run `get_file_diffs`. The hand-added csproj lines and the imodspec dependency must not be proposed for removal. If they are, do not apply; report it.

**B7. CommandRequestTemplate** (`Templates/CommandRequest/CommandRequestTemplatePartial.cs`)
- Constructor: `AddClass(Model.Name)`, then `OnBuild(…, 0)`.
- In `OnBuild`, apply decisions 30 and 31.
- Check: build is green.

**B8. CommandHandlerTemplate** (`Templates/CommandHandler/CommandHandlerTemplatePartial.cs`)
- Constructor: `AddClass($"{Model.Name}Handler")`, then `OnBuild(…, 0)`.
- In `OnBuild`, apply decisions 31 and 32.
- Add the `BeforeTemplateExecution()` override that publishes the handler registration (decision 29).
- Check: build is green.

**B9. Controller model and registration.** Create the two hand-written files from decisions 34 and 35 in `WorkshopModule/WorkshopModule/Templates/CqrsController/`. They have no Intent header and no `[IntentManaged]` attributes.
- Usings come from these namespaces:
  - `Intent.Engine`
  - `Intent.Metadata.Models`
  - `Intent.Modelers.Services.Api`
  - `Intent.Modelers.Services.CQRS.Api`
  - `Intent.Modules.AspNetCore.Controllers.Templates.Controller`
  - `Intent.Modules.AspNetCore.Controllers.Templates.Controller.Models`
  - `Intent.Modules.Common.Registrations`
  - `Intent.Modules.Common.Types.Api`
  - `Intent.Modules.Metadata.Security.Models`
  - `Intent.Modules.Metadata.WebApi.Models`
  - `Intent.Templates`
- Check: build is green.

**B10. CqrsControllerInstaller** (`FactoryExtensions/CqrsControllerInstaller.cs`)
- Add the `OnAfterTemplateRegistrations` override from decisions 36 and 40. Query pruning comes first, then the `AfterBuild` registration. Keep the generated `Id` and `Order` members as generated.
- Add `using Intent.Modules.Common;` (orchestrator §Factory Extension, CS0308).
- Check: build is green. `grep -n "TemplateExists(\"Application.Query\"" WorkshopModule/WorkshopModule/FactoryExtensions/CqrsControllerInstaller.cs` finds the gate, and `CqrsControllerTemplateRegistration.cs` contains no `TemplateExists`. `grep -n "FindTemplateInstance<" WorkshopModule/WorkshopModule/FactoryExtensions/CqrsControllerInstaller.cs` returns nothing; only the plural `FindTemplateInstances` is allowed. `grep -rn "Ok(result)" WorkshopModule/WorkshopModule` returns nothing (decision 14).

**B11. Model the ConsumerTestApp Products folder (model only).**
- Call `get_applications` for the ConsumerTestApp `Services` designer id.
- With one `run_designer_script`, under the `ConsumerTestApp.Services` package, create a `Folder` named `Products` containing:
  - `Command` `CreateProductCommand`:
    - type `guid`;
    - `DTO-Field` children `Title` (`string`) and `Price` (`decimal`), in that order;
    - stereotype `Http Settings` with `Verb` = `POST`, `Route` = `api/products`, `Return Type Mediatype` = `Default` and `Success Response Code` = `201 (Created)`.
  - `Query` `GetProductsQuery`:
    - type `string` as a collection;
    - no fields;
    - stereotype `Http Settings` with `Verb` = `GET`, `Route` = `api/products`, `Return Type Mediatype` = `Default` and `Success Response Code` = `200 (Ok)`.
- Add both to `Default Diagram`, in a single vertical column on the left, 100px apart. This is required by the Services designer's own modelling rules.
- Check: `getElementDetails` on both shows the types, fields and stereotype values above. `getDesignerValidationErrors()` on the Services designer returns zero errors.
- **Do not** call `run_software_factory` for ConsumerTestApp.

**B12. Verify Phase B.**
- Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` from the solution root. It must finish with 0 errors and no `CS0618`. Check that `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod` has a fresh timestamp.
- Run `run_software_factory` on **WorkshopModule** again, then `get_file_diffs`. It must propose zero changes. If it proposes removing bespoke code (for example the `BeforeTemplateExecution()` overrides or the installer override), do not apply it. Protect those members with `[IntentManaged(Mode.Ignore)]`, then rebuild and re-run until zero changes are proposed.
- Confirm all of the following:
  - `grep -rn "ConsumerTestApp" WorkshopModule/WorkshopModule` returns nothing.
  - Every cross-template type comes from a `Get…Name()` extension or a `TemplateId` constant.
  - Every type resolution and member addition happens inside `OnBuild`, except the bare type declarations.
  - `WorkshopModule.csproj` has no `ProjectReference`.
- Write the final report:
  - the files changed;
  - every decision taken that this plan does not cover (choose the option closest to the hand-written pattern);
  - each template's `TemplateId`;
  - whether B2 needed `installDesigners: true`.

**Checkpoint B:** WorkshopModule has six templates and one factory extension with zero validation errors. The build is green with the pinned packages. A fresh Software Factory run on WorkshopModule proposes zero changes. ConsumerTestApp's Services designer has the `Products` model with zero validation errors, and its Software Factory has not been run. The facilitator verifies Section 8 and tags `p2-done`.

## 7. Done when

- `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental`, run from the solution root, succeeds with no `CS0618` warnings.
- A fresh Software Factory run on WorkshopModule proposes zero changes.
- The WorkshopModule Module Builder has zero validation errors.
- The ConsumerTestApp Services designer has zero validation errors and contains the B11 model. Its Software Factory was not run.

## 8. Expected consumer result

These are the facilitator's steps, not the executor's.

1. Delete `intent/.cache/modules/WorkshopModule.1.0.0`. The version is still 1.0.0, and without this Intent Architect silently keeps the stale cached module.
2. Update WorkshopModule in ConsumerTestApp from the `Workshop` repository. Its new dependency `Intent.AspNetCore.Controllers` 7.1.15 is already installed.
3. Make sure the hand-written slice, including the P2.5 pieces, is committed, so any unexpected change shows up in `git diff`.
4. Run ConsumerTestApp's Software Factory and review the staged changes before applying them. Do not apply any change that removes a P2.5 piece; see "P2.5 pieces" below. After Phase A only, expect just the four `Common/Dispatching` files and no change to `DependencyInjection.cs`. After Phase B, expect everything below.

For the four `Common/Dispatching` files, `CreateProductCommand.cs` and the two DI lines, the module reproduces the hand-written code exactly. The only changes are the Intent header lines and the missing trailing newline.

**`ConsumerTestApp.Application/Common/Dispatching/IAppRequest.cs`** (an overwrite; adds the Intent header)

```csharp
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.IAppRequest", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public interface IAppRequest<out TResponse>
    {
    }

    public sealed record Unit
    {
        public static Unit Value { get; } = new();
    }
}
```

**`ConsumerTestApp.Application/Common/Dispatching/IAppRequestHandler.cs`**

```csharp
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.IAppRequestHandler", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public interface IAppRequestHandler<in TRequest, TResponse>
        where TRequest : IAppRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
```

**`ConsumerTestApp.Application/Common/Dispatching/IDispatcher.cs`**

```csharp
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.IDispatcher", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public interface IDispatcher
    {
        Task<TResponse> Send<TResponse>(IAppRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
```

**`ConsumerTestApp.Application/Common/Dispatching/Dispatcher.cs`**

```csharp
using System.Collections.Concurrent;
using Intent.RoslynWeaver.Attributes;
using Microsoft.Extensions.DependencyInjection;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.Dispatcher", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public class Dispatcher : IDispatcher
    {
        private static readonly ConcurrentDictionary<Type, object> Invokers = new();

        private readonly IServiceProvider _serviceProvider;

        public Dispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TResponse> Send<TResponse>(
            IAppRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var invoker = (HandlerInvoker<TResponse>)Invokers.GetOrAdd(request.GetType(), CreateInvoker<TResponse>);

            return invoker.Invoke(request, _serviceProvider, cancellationToken);
        }

        private static object CreateInvoker<TResponse>(Type requestType)
        {
            var invokerType = typeof(HandlerInvoker<,>).MakeGenericType(requestType, typeof(TResponse));
            return Activator.CreateInstance(invokerType)!;
        }

        private abstract class HandlerInvoker<TResponse>
        {
            public abstract Task<TResponse> Invoke(
                IAppRequest<TResponse> request,
                IServiceProvider serviceProvider,
                CancellationToken cancellationToken);
        }

        private sealed class HandlerInvoker<TRequest, TResponse> : HandlerInvoker<TResponse>
            where TRequest : IAppRequest<TResponse>
        {
            public override Task<TResponse> Invoke(
                IAppRequest<TResponse> request,
                IServiceProvider serviceProvider,
                CancellationToken cancellationToken)
            {
                var handler = serviceProvider.GetRequiredService<IAppRequestHandler<TRequest, TResponse>>();
                return handler.Handle((TRequest)request, cancellationToken);
            }
        }
    }
}
```

**`ConsumerTestApp.Application/Commands/Products/CreateProductCommand.cs`**

```csharp
using ConsumerTestApp.Application.Common.Dispatching;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.CommandRequest", Version = "1.0")]

namespace ConsumerTestApp.Application.Commands.Products
{
    public class CreateProductCommand : IAppRequest<Guid>
    {
        public CreateProductCommand(string title, decimal price)
        {
            Title = title;
            Price = price;
        }

        public string Title { get; set; }
        public decimal Price { get; set; }
    }
}
```

**`ConsumerTestApp.Application/Commands/Products/CreateProductCommandHandler.cs`**

The file exists, so this is a merge, not a fresh generation:
- The Software Factory owns the method's signature and attributes (`Mode.Fully`). So `Handle` gains `[IntentManaged(Mode.Fully, Body = Mode.Ignore)]` and becomes `async`.
- `Body = Mode.Ignore` keeps the body already on disk, so the hand-written `throw` survives.
- The file gains the `Intent.RoslynWeaver.Attributes` using and the two `[assembly: …]` attributes.
- `async` with no `await` gives a CS1998 warning, not an error.

```csharp
using ConsumerTestApp.Application.Common.Dispatching;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.CommandHandler", Version = "1.0")]

namespace ConsumerTestApp.Application.Commands.Products
{
    public class CreateProductCommandHandler : IAppRequestHandler<CreateProductCommand, Guid>
    {
        public CreateProductCommandHandler()
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Implement CreateProductCommandHandler.Handle");
        }
    }
}
```

A handler for a *new* command, with no file on disk, gets the template's own body instead:
- `// IntentInitialGen`
- `// TODO: Implement Handle (<Command>Handler) functionality`
- `throw new NotImplementedException("Your implementation here...");`

**`ConsumerTestApp.Application/DependencyInjection.cs`**: no diff against the current file.
- The module now generates the `IDispatcher` and handler lines that were added by hand, plus the `ConsumerTestApp.Application.Commands.Products` and `ConsumerTestApp.Application.Common.Dispatching` usings.
- The `services.Decorate<…>` line is kept; see "P2.5 pieces".

```csharp
            services.AddScoped<IValidatorProvider, ValidatorProvider>();
            services.AddScoped<IDispatcher, Dispatcher>();
            services.AddScoped<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandHandler>();
            services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();
            return services;
```

**`ConsumerTestApp.Api/Controllers/ProductsController.cs`**

Lines marked `// module` are WorkshopModule's, and they match the hand-written file exactly. The rest belongs to Controllers 7.1.15:
- `[ApiController]`, `[HttpPost("api/products")]` and the action signature match the hand-written file.
- The XML doc comments, `typeof(ProblemDetails)` on the 400 and the extra 500 `[ProducesResponseType]` are Controllers' own additions; the module does not control their exact text.

There is **no `GetProducts` action**, because no template fills `Application.Query` (decision 40).

```csharp
using ConsumerTestApp.Application.Commands.Products;
using ConsumerTestApp.Application.Common.Dispatching;
using Intent.RoslynWeaver.Attributes;
using Microsoft.AspNetCore.Mvc;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.AspNetCore.Controllers.Controller", Version = "1.0")]

namespace ConsumerTestApp.Api.Controllers
{
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IDispatcher _dispatcher;                          // module

        public ProductsController(IDispatcher dispatcher)                  // module
        {
            _dispatcher = dispatcher;                                      // module
        }

        /// <summary>
        /// </summary>
        /// <response code="201">Successfully created.</response>
        /// <response code="400">One or more validation errors have occurred.</response>
        [HttpPost("api/products")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Guid>> CreateProduct(
            [FromBody] CreateProductCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _dispatcher.Send(command, cancellationToken); // module
            return Created(string.Empty, result);                           // module (GetReturnStatement)
        }
    }
}
```

**P2.5 pieces (no template produces them yet).** The expectation for each, and what to do if it is wrong:

| Piece | Expected after the Software Factory run | Why |
|---|---|---|
| `services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();` in `ConsumerTestApp.Application/DependencyInjection.cs` | **Kept**, in place after the handler line | `AddApplication` is `[IntentMerge]`, so statements added by hand are not removed. |
| Its using `using ConsumerTestApp.Application.Commands.Products;` | **Kept** | After Phase B the handler registration generates it. Before then, usings in this file are merged, not pruned. |
| `Scrutor` 7.0.0 and `Microsoft.Extensions.Logging.Abstractions` 10.0.9 in `ConsumerTestApp.Application.csproj` | **Kept** | The project template adds the packages templates ask for and does not remove PackageReferences added by hand. P2 adds no PackageReferences to this project. |
| `ConsumerTestApp.Application/Commands/Products/CreateProductCommandLoggingDecorator.cs` | **Untouched** | No template outputs this path and it is not a managed file, so the Software Factory neither overwrites nor deletes it. |

How to handle them:
- If the staged changes propose removing the `Decorate` line, its using, or either PackageReference, exclude that change and apply the rest. If it was already applied, restore the file with `git checkout -- <file>`.
- Either way the decorator file stays as the hand-written spec for P2.5.
- With all four pieces in place, p2-done builds, and the decorator still wraps the generated handler registration at runtime.
- `POST api/products` reaches the handler through `Dispatcher`, then the decorator, then the handler. It returns **500** because the preserved hand-written body throws `NotImplementedException`; the decorator logs the request first. To see a 201, implement the body of `Handle`. `Body = Mode.Ignore` keeps the implementation on later runs.

Differences from the current hand-written files that are expected and accepted:

- Every generated file gains the `Intent.RoslynWeaver.Attributes` using and the two `[assembly: …]` attributes. Like every Intent-generated file, each ends without a trailing newline.
- `CreateProductCommandHandler.cs`: `Handle` gains `[IntentManaged(Mode.Fully, Body = Mode.Ignore)]` and `async`. The body is unchanged.
- `ProductsController.cs`: Controllers adds XML doc comments, `typeof(ProblemDetails)` on the 400, and a 500 `[ProducesResponseType]`.

Things to check while verifying:

- `ProductsController` has no `GetProducts` action, and ConsumerTestApp builds.
  - If `GetProducts` appears anyway, `ControllerTemplate` read `Model.Operations` before the installer's `OnAfterTemplateRegistrations` ran. Report it.
- Commands land in `ConsumerTestApp.Application/Commands/Products/`. The CommandRequest Role `Application.Command` resolves to the `Application` Output Anchor, the same way installed modules' dotted roles (for example `Domain.NotFoundException`) resolve to their project. If the command file appears outside `ConsumerTestApp.Application`, the role did not resolve; report it.
- `Intent.AspNetCore.Controllers.Dispatch.ServiceContract` 5.2.20 is installed in ConsumerTestApp. Confirm it added nothing to `ProductsController` (no service-contract field, no validation or unit-of-work statements). If it did, report it.
- A void command (no return type) produces `IAppRequest<Unit>`, `IAppRequestHandler<…, Unit>`, and a `Task<Unit>` `Handle`.
- *Stretch Exercise:* once a Query template with Role `Application.Query` exists, re-running the Software Factory adds `GetProducts` with `var result = await _dispatcher.Send(new GetProductsQuery(), cancellationToken);` and `return Ok(result);`. There is no installer change.

ConsumerTestApp builds with `dotnet build ConsumerTestApp/ConsumerTestApp.sln`.
