# P1 — Correlation ID module

## 1. Goal

Make WorkshopModule generate the correlation ID pattern that ConsumerTestApp currently has written by hand, so that any Clean Architecture application with WorkshopModule installed gets it. That pattern is: an `ICorrelationContext` abstraction, its scoped `CorrelationContext` implementation, a `CorrelationMiddleware` that reads or creates the ID, echoes it and opens a logging scope, and a `CorrelationDelegatingHandler` that forwards the ID on outgoing HTTP calls. The module delivers this through four Single File C# templates built with the C# File Builder, a `Tracing Settings` module settings group, and registration requests that the host DI and startup templates turn into `AddScoped`/`AddTransient`/`UseCorrelationId()` lines.

## 2. Starting point

The workspace from the workshop's _Workspace Setup_, plus the hand-written pattern that Practical 1's Prompt A just wrote (uncommitted).

What exists:

- `intent/ModuleBuildingTraining.isln` is an Intent Architect solution with three applications: ModuleBuilderAiSkills, WorkshopModule and ConsumerTestApp.
- **WorkshopModule** is a Module Builder application.
  - Its Module Builder designer has one package, `WorkshopModule` (specialization `Intent Module`). The package is empty: no templates, no settings, no factory extensions.
  - Its C# project is `WorkshopModule/WorkshopModule/WorkshopModule.csproj` (net8.0, which already references `Intent.Modules.Common.CSharp` 3.8.1). Its VS solution is `WorkshopModule/ModuleBuildingTraining.sln`.
  - `WorkshopModule/WorkshopModule/WorkshopModule.imodspec` is at version 1.0.0 and has empty `<templates>` and `<moduleSettings>` sections.
  - Building writes `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod`. That folder is the solution's `Workshop` module repository.
- **ConsumerTestApp** is a Clean Architecture .NET 10 app at `ConsumerTestApp/`. WorkshopModule 1.0.0 is installed from the `Workshop` repository.
  - Its projects have these Output Anchor roles: `ConsumerTestApp.Api` has `Distribution` and `Startup`, `ConsumerTestApp.Application` has `Application`, `ConsumerTestApp.Infrastructure` has `Infrastructure`.
  - All projects have `ImplicitUsings` and `Nullable` enabled.
  - The hand-written pattern is the reference output:
    - `ConsumerTestApp/ConsumerTestApp.Application/Common/Interfaces/ICorrelationContext.cs`
    - `ConsumerTestApp/ConsumerTestApp.Infrastructure/Configuration/Correlation/CorrelationContext.cs`
    - `ConsumerTestApp/ConsumerTestApp.Api/Middleware/CorrelationMiddleware.cs` (contains `CorrelationMiddleware` **and** `CorrelationMiddlewareExtensions.UseCorrelationId()`)
    - `ConsumerTestApp/ConsumerTestApp.Infrastructure/HttpClients/CorrelationDelegatingHandler.cs`
    - The registration lines in `ConsumerTestApp/ConsumerTestApp.Infrastructure/DependencyInjection.cs` (`services.AddScoped<ICorrelationContext, CorrelationContext>();` and `services.AddTransient<CorrelationDelegatingHandler>();`, after the `IUnitOfWork` line)
    - The pipeline line in `ConsumerTestApp/ConsumerTestApp.Api/Program.cs` (`app.UseCorrelationId();`, the first statement after `// Configure the HTTP request pipeline.`, before `app.UseSerilogRequestLogging();`)

Read these files before Step A1. They are the specification, and you only read them.

## 3. Skills to load

Load these before any other step by reading `.claude/skills/<name>/SKILL.md` and the resources it points to. Use them instead of decompiling NuGet packages.

- `file-builder-expert`: read `resources/api-cheatsheet.md`, `resources/builder-patterns.md`, `resources/troubleshooting.md` and `resources/patterns/*.cs`.
- `intent-module-orchestrator`: read `resources/orchestration-cheatsheet.md`, sections §DI Registration, §Host Wiring and §Two-Tier Module Dependency.
- `intent-metadata-consumer`: read `resources/metadata-cheatsheet.md`.
- `scripting-the-model`: required before `run_designer_query` / `run_designer_script`. Read `references/elements.md`, `references/stereotypes.md` and `references/types.md`.
- `exploring-the-model`
- `changing-the-model`
- `generating-and-applying-code`
- `resolve-destructive-changes`: load it only if `run_software_factory` reports a change as destructive "yes" or "unknown".

Every Intent Architect MCP tool call needs the `intention` parameter (10 words or fewer).

## 4. Scope

**In**

- WorkshopModule Module Builder metadata: four C# Templates and one Module Settings Configuration, as listed in Step A3 and Step A4.
- Running the Software Factory on **WorkshopModule only**, and applying its changes.
- Bespoke code in the four generated `*TemplatePartial.cs` files under `WorkshopModule/WorkshopModule/Templates/`: the constructor body plus an added `BeforeTemplateExecution()` override.
- Building `WorkshopModule/ModuleBuildingTraining.sln`.

**Out**

- Never run the Software Factory on ConsumerTestApp.
- Never hand-edit files owned by other modules. That includes ConsumerTestApp's `DependencyInjection.cs` and `Program.cs`, and every generated file in WorkshopModule outside the bodies listed under In.
- Any change to ConsumerTestApp at all. The hand-written pattern stays exactly as it is, and the facilitator verifies the module against it.
- Factory extensions. None are created.
- Changing WorkshopModule's version. It stays 1.0.0.
- Attaching `CorrelationDelegatingHandler` to any `HttpClient` registration (`AddHttpMessageHandler`). The hand-written pattern does not do it.
- NuGet package dependencies on generated output. None are needed: everything used is in the BCL or the ASP.NET Core shared framework.
- Release notes, docs, icons, migrations, and tests in ConsumerTestApp.
- Anything inside `.intent` or `Intent.Metadata` folders, and any `.application.config` file. Change the model only through `run_designer_script`.
- Committing or tagging. The facilitator tags `p1-done`.

## 5. Decisions already made — do not ask

### Facilitator decisions (the user specified these)

1. One type per template. Reference other templates' types through the generated extension methods (e.g. `GetICorrelationContextName()`), never a hard-coded namespace.
2. Read the header name and Echo To Response through the generated `TracingSettings` accessors at generation time. The header name becomes a constant, and the `Response.OnStarting` block is generated only when Echo To Response is on.
3. `ICorrelationContext` is injected into `InvokeAsync`, not the constructor.
4. Use `ILogger.BeginScope`, not Serilog's `LogContext`.
5. The echo uses `Response.OnStarting`, because the exception handler clears headers on a 500.
6. Use the C# File Builder API only. Do all type resolution and member generation inside `OnBuild`, never before `CSharpFile` is assigned.
7. Registrations go through registration requests from the templates themselves, not a factory extension, and never through hand edits to files other modules own.
8. The four templates and the settings group are exactly those in Step A3 and Step A4: names, roles, default locations, control types and defaults.

### Plan-author decisions (derived from the hand-written pattern and the loaded skills; not facilitator decisions)

9. **Exception to decision 1 for the middleware extension class.** The hand-written `CorrelationMiddleware.cs` declares two types, `CorrelationMiddleware` and `CorrelationMiddlewareExtensions`, and the scope has only four templates. So the CorrelationMiddleware template emits both classes into its one file. `CorrelationMiddleware` is declared **first**, so `CSharpFile.GetConfig()` reports `ClassName = CorrelationMiddleware` and `GetCorrelationMiddlewareName()` resolves to it. No other template emits more than one type.
10. **BeginScope shape.** `file-builder-expert` forbids raw-string object-initializer braces. `CSharpObjectInitializerBlock` always renders across several lines, and `AddUsingBlock` only takes a string expression, so the one-line `new Dictionary<string, object> { ["CorrelationId"] = correlationId }` inside `using (...)` cannot be reproduced. Emit it as a separate variable, `var scopeState = new Dictionary<string, object>` with an object-initializer block (`AddInitStatement("[\"CorrelationId\"]", "correlationId")`, `.WithSemicolon()`). Then emit `using (_logger.BeginScope(scopeState))`. Section 8 shows the exact shape.
11. **Constructor vs OnBuild.** `CSharpFile.GetConfig()` throws when no type is declared, and `ClassName`/`FullTypeName()` are read before the build. So the constructor assigns `CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())` and declares each type **by name only** (`AddInterface("ICorrelationContext")` / `AddClass("…")`, with no configure lambda). The constructor then registers `.OnBuild(file => { … }, 0)`. Everything else happens inside that `OnBuild`: members, base types and interfaces, `UseType`/`GetXName()` resolution, and settings reads.
12. **Where the requests are published.** Each template overrides `public override void BeforeTemplateExecution()`, calls `base.BeforeTemplateExecution()`, and then calls `ExecutionContext.EventDispatcher.Publish(...)`. This is the template equivalent of the orchestrator skill's `OnBeforeTemplateExecution` rule.
    - CorrelationContext: `ContainerRegistrationRequest.ToRegister(this).ForInterface(GetTemplate<IClassProvider>(ICorrelationContextTemplate.TemplateId)).ForConcern("Infrastructure").WithPerServiceCallLifeTime().WithPriority(10)`
    - CorrelationDelegatingHandler: `ContainerRegistrationRequest.ToRegister(this).ForConcern("Infrastructure").WithPriority(11)`. The default lifetime is Transient.
    - CorrelationMiddleware: `ApplicationBuilderRegistrationRequest.ToRegister("UseCorrelationId").HasDependency(this).WithPriority(-1000)`
    - Why these values:
      - `ForConcern("Infrastructure")` is mandatory, because `Concern` defaults to null and the Infrastructure DI template ignores anything else.
      - Priorities 10 and 11 put the two lines after the default-priority (0) `IUnitOfWork` registration, in the hand-written order.
      - −1000 sorts ahead of `UseSerilogRequestLogging` (−100) and `UseExceptionHandler` (−40).
      - Use the `IClassProvider` overload of `ForInterface` and `HasDependency(this)` so the host templates add the `using` directives. Never pass a namespace string.
13. **Settings group name.** Name the Module Settings Configuration `Tracing Settings`. It then generates `GetTracingSettings()` on `IApplicationSettingsProvider`, returning a `TracingSettings` class with `CorrelationHeaderName()` (string) and `EchoToResponse()` (bool). Read them inside `OnBuild` as `ExecutionContext.Settings.GetTracingSettings()`. If `CorrelationHeaderName()` is null or whitespace, use `"X-Correlation-Id"`, which is the modelled default. Use `EchoToResponse()` as returned.
14. **Usings.** Do not `AddUsing` framework namespaces. Resolve framework types the generated code spells out through `UseType("Namespace.Type")`, as `file-builder-expert` Must 8 and Must-Not 7 require, and resolve WorkshopModule types through the generated `Get…Name()` extension methods. The Roslyn weaver drops usings already covered by the target project's implicit usings. ConsumerTestApp's existing generated Api files show this. So the expected outputs in Section 8 contain only the `ConsumerTestApp.*` and `Intent.RoslynWeaver.Attributes` usings.
15. **Layout.** Put the templates and the settings configuration directly under the `WorkshopModule` package, not in a folder. Each C# Template's type reference is `Single File`.

## 6. Steps

### Phase A — Module Builder metadata

**A1. Read the reference.** Read the six ConsumerTestApp files listed in Section 2. Do not modify them.

**A2. Explore the designer.**
- Call `get_applications` to get the ids of the WorkshopModule application and its `Module Builder` designer.
- With `run_designer_query`, call `getDesignerSchema()` and `getDesignerModelStructure()`.
- Confirm:
  - The `WorkshopModule` package exists and has no elements.
  - The element types `C# Template`, `Module Settings Configuration` and `Module Settings Field Configuration` are available.
  - The stereotypes `C# Template Settings` (property `Templating Method`), `Template Settings` (properties `Role` and `Default Location`, applicable through the `Single File` type reference), `Configuration` (property `Settings Type`) and `Field Configuration` (properties `Control Type` and `Default Value`) are available.
- Check: the query result lists all of the above. If the package already has elements, stop and report.

**A3. Model the four templates.** Use one `run_designer_script` that creates four `C# Template` elements under the `WorkshopModule` package. For each, set the type reference to `Single File`, set `C# Template Settings` → `Templating Method` = `C# File Builder`, and set these `Template Settings` values:

| Template name | Role | Default Location |
|---|---|---|
| `ICorrelationContext` | `Application` | `Common/Interfaces` |
| `CorrelationContext` | `Infrastructure` | `Configuration/Correlation` |
| `CorrelationMiddleware` | `Distribution` | `Middleware` |
| `CorrelationDelegatingHandler` | `Infrastructure` | `HttpClients` |

Check: run `getElementDetails` on each of the four with `run_designer_query`. It must show the type `Single File`, Templating Method `C# File Builder`, and the Role and Default Location above.

**A4. Model the settings group.** Use one `run_designer_script` that creates a `Module Settings Configuration` named `Tracing Settings` under the `WorkshopModule` package, with `Configuration` → `Settings Type` = `Application Settings`. Under it, create two `Module Settings Field Configuration` children:

| Field name | Control Type | Default Value |
|---|---|---|
| `Correlation Header Name` | `Text Box` | `X-Correlation-Id` |
| `Echo To Response` | `Switch` | `true` |

Check: `getElementDetails` shows both fields with these values.

**A5. Validate.** Run `getDesignerValidationErrors()` with `run_designer_query`. It must return zero errors. If it does not, fix the model with a follow-up `run_designer_script` and check again.

**A6. Generate WorkshopModule.**
- Call `run_software_factory` for the **WorkshopModule** application. Review the staged changes with `get_file_diffs`, then apply them with `apply_staged_file_changes`. Follow `generating-and-applying-code`. If a change is reported destructive "yes" or "unknown", follow `resolve-destructive-changes`.
- Expected changes:
  - New files in `WorkshopModule/WorkshopModule/Templates/<Name>/`: `<Name>TemplatePartial.cs` and `<Name>TemplateRegistration.cs` for each of the four templates.
  - A new `WorkshopModule/WorkshopModule/Templates/TemplateExtensions.cs` with `GetICorrelationContextName()`, `GetCorrelationContextName()`, `GetCorrelationMiddlewareName()` and `GetCorrelationDelegatingHandlerName()`.
  - A new `WorkshopModule/WorkshopModule/Settings/ModuleSettingsExtensions.cs` with `GetTracingSettings()`, `TracingSettings.CorrelationHeaderName()` and `TracingSettings.EchoToResponse()`.
  - Updates to `WorkshopModule.imodspec`: four `<template>` entries, a `<moduleSettings>` entry for Tracing Settings, and a dependency on `Intent.Common.CSharp`.
- Check: all of these files and entries exist. Record each template's `TemplateId` constant; use the constants, never the literal strings.

**A7. Build.** Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` from the solution root. It must succeed.

**Checkpoint A:** the model has four templates and Tracing Settings with zero validation errors, the generated scaffolding is applied, and the build is green. Phase B starts from here.

### Phase B — Template implementations

These rules apply to every template:
- Edit only the constructor body of `<Name>TemplatePartial.cs`. It is generated `[IntentManaged(Mode.Fully, Body = Mode.Ignore)]`.
- Keep the generated `CSharpFile`, `DefineFileConfig()` and `TransformText()` members exactly as generated.
- Follow decisions 1–15.
- Build with warnings visible after each template. Treat any `CS0618` "obsolete" warning as a failure.
- Match the blank-line layout of the hand-written file. Section 8 has the expected output. Where the builder's default separators differ, use the separator API (`SeparatedFromPrevious(...)` / `BeforeSeparator`) rather than raw text.

**B1. ICorrelationContextTemplate** (`Templates/ICorrelationContext/ICorrelationContextTemplatePartial.cs`)
- Constructor: `AddInterface("ICorrelationContext")`, then `OnBuild(…, 0)`.
- In `OnBuild`, add:
  - A public interface property `string? CorrelationId { get; }` (get-only).
  - A method `void SetCorrelationId(string correlationId)`.
  - No blank line between the two members.
- Check: build is green.

**B2. CorrelationContextTemplate** (`Templates/CorrelationContext/CorrelationContextTemplatePartial.cs`)
- Constructor: `AddClass("CorrelationContext")`, then `OnBuild(…, 0)`.
- In `OnBuild`:
  - `ImplementsInterface(this.GetICorrelationContextName())`.
  - Property `public string? CorrelationId { get; private set; }`.
  - Method `public void SetCorrelationId(string correlationId)` with the statement `CorrelationId = correlationId;`.
- Check: build is green, and `ConsumerTestApp` appears nowhere in the file.

**B3. CorrelationDelegatingHandlerTemplate** (`Templates/CorrelationDelegatingHandler/CorrelationDelegatingHandlerTemplatePartial.cs`)
- Constructor: `AddClass("CorrelationDelegatingHandler")`, then `OnBuild(…, 0)`.
- In `OnBuild`:
  - Read the header name per decision 13.
  - Base type `UseType("System.Net.Http.DelegatingHandler")`.
  - Field `private const string HeaderName = "<header>";` (`PrivateConstant`).
  - Constructor parameter `this.GetICorrelationContextName()` named `correlationContext`, with `IntroduceReadonlyField()`, which gives `_correlationContext`. Put a blank line between the const and the readonly field.
  - Method `protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)`, with framework types resolved through `UseType`. Its body:
    1. `var correlationId = _correlationContext.CorrelationId;`
    2. `AddIfStatement("!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(HeaderName)")` containing `request.Headers.TryAddWithoutValidation(HeaderName, correlationId);`
    3. A blank line, then `return base.SendAsync(request, cancellationToken);`
- Check: build is green.

**B4. CorrelationMiddlewareTemplate** (`Templates/CorrelationMiddleware/CorrelationMiddlewareTemplatePartial.cs`)
- Constructor, in this order: `AddClass("CorrelationMiddleware")`, then `AddClass("CorrelationMiddlewareExtensions")`, then `OnBuild(…, 0)`.
- In `OnBuild`, read both settings per decision 13.
- `CorrelationMiddleware` class:
  - Field `public const string HeaderName = "<header>";`
  - A blank line, then a constructor with `RequestDelegate next` and `ILogger<CorrelationMiddleware> logger`. Both use `IntroduceReadonlyField()`, giving `_next` and `_logger`. Resolve `RequestDelegate` and `ILogger<>` through `UseType`, and use the template's own `ClassName` for the generic argument.
  - Method `public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)`. `ICorrelationContext` comes from `this.GetICorrelationContextName()` (decisions 1 and 3). The body, in order:
    1. The three-line `var correlationId = … ? values.ToString() : Guid.NewGuid().ToString();` assignment, as in the hand-written file.
    2. A blank line, then `correlationContext.SetCorrelationId(correlationId);`
    3. **Only when `EchoToResponse()` is true**: a blank line, then `context.Response.OnStarting(...)`. Build the argument as an invocation with a `CSharpLambdaBlock("()")` containing `context.Response.Headers[HeaderName] = correlationId;` and `return Task.CompletedTask;`.
    4. A blank line, then the `scopeState` dictionary variable from decision 10.
    5. A blank line, then `AddUsingBlock("_logger.BeginScope(scopeState)", …)` containing `await _next(context);`.
- `CorrelationMiddlewareExtensions` class:
  - `Static()`.
  - Method `public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)` returning `app.UseMiddleware<CorrelationMiddleware>()`. Use the template's own `ClassName`, and resolve `IApplicationBuilder` through `UseType`.
- Check: build is green. `grep -n "LogContext\|Serilog" WorkshopModule/WorkshopModule/Templates -r` returns nothing.

**Checkpoint B:** the four templates are implemented and the build is green. Phase C starts from here.

### Phase C — Registrations

**C1.** In `CorrelationContextTemplatePartial.cs`, add the `BeforeTemplateExecution()` override that publishes the `ContainerRegistrationRequest` from decision 12.

**C2.** In `CorrelationDelegatingHandlerTemplatePartial.cs`, add the `BeforeTemplateExecution()` override that publishes the transient `ContainerRegistrationRequest` from decision 12.

**C3.** In `CorrelationMiddlewareTemplatePartial.cs`, add the `BeforeTemplateExecution()` override that publishes the `ApplicationBuilderRegistrationRequest` from decision 12.

Requests live in `Intent.Modules.Common.CSharp.DependencyInjection`, which the project already references. WorkshopModule needs no dependency on the AspNetCore or Infrastructure.DependencyInjection modules (Tier 1 in `intent-module-orchestrator`).

Check: build is green. There is no `FactoryExtensions` folder and no `*FactoryExtension.cs` in `WorkshopModule/WorkshopModule`.

**Checkpoint C:** the templates publish their registration requests and the build is green. Phase D starts from here.

### Phase D — Verification

**D1.** Run `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental` from the solution root. It must finish with 0 errors and no `CS0618` warnings. Check that `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod` has a fresh timestamp.

**D2.** Run `run_software_factory` on **WorkshopModule** again, then `get_file_diffs`. It must propose zero changes.
- If it proposes removing the bespoke code (for example the `BeforeTemplateExecution()` overrides), do not apply it. Protect those members with `[IntentManaged(Mode.Ignore)]`, then rebuild and re-run until zero changes are proposed.

**D3.** Review each template against Section 8 and confirm all of the following:
- `grep -rn "ConsumerTestApp" WorkshopModule/WorkshopModule` returns nothing.
- Every cross-template type comes from a `Get…Name()` extension or a `TemplateId` constant.
- All settings reads happen inside `OnBuild`.
- No member is added outside `OnBuild` except the bare type declarations.

**D4.** Write the final report:
- The files changed.
- Any decision taken that this plan does not cover (choose the option closest to the hand-written pattern).
- Each template's `TemplateId`.

## 7. Done when

- `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental`, run from the solution root, succeeds with no `CS0618` warnings.
- A fresh Software Factory run on WorkshopModule proposes zero changes.
- The WorkshopModule Module Builder has zero validation errors.

## 8. Expected consumer result

These are the facilitator's steps, not the executor's.

1. Update WorkshopModule in ConsumerTestApp from the `Workshop` repository. The version is still 1.0.0, and Intent Architect caches `intent/.cache/modules/WorkshopModule.1.0.0`, so force a reinstall (clear that cache entry, or reinstall the module) so the rebuilt `.imod` is picked up.
2. Open ConsumerTestApp's **Settings → Tracing Settings**. Confirm `Correlation Header Name` = `X-Correlation-Id` and `Echo To Response` = on, then save so the values are persisted. The generated `EchoToResponse()` accessor returns `false` for a value that was never persisted. In that case the `OnStarting` block would silently not be generated.
3. Run ConsumerTestApp's Software Factory.

With the default settings, the result is:

**`ConsumerTestApp.Application/Common/Interfaces/ICorrelationContext.cs`** (an overwrite; adds the Intent header)

```csharp
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("<ICorrelationContextTemplate.TemplateId>", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Interfaces
{
    public interface ICorrelationContext
    {
        string? CorrelationId { get; }
        void SetCorrelationId(string correlationId);
    }
}
```

**`ConsumerTestApp.Infrastructure/Configuration/Correlation/CorrelationContext.cs`**

```csharp
using ConsumerTestApp.Application.Common.Interfaces;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("<CorrelationContextTemplate.TemplateId>", Version = "1.0")]

namespace ConsumerTestApp.Infrastructure.Configuration.Correlation
{
    public class CorrelationContext : ICorrelationContext
    {
        public string? CorrelationId { get; private set; }

        public void SetCorrelationId(string correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
```

**`ConsumerTestApp.Api/Middleware/CorrelationMiddleware.cs`**

The hand-written file already uses the `scopeState` variable (decision 10), so the logic matches exactly.

```csharp
using ConsumerTestApp.Application.Common.Interfaces;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("<CorrelationMiddlewareTemplate.TemplateId>", Version = "1.0")]

namespace ConsumerTestApp.Api.Middleware
{
    public class CorrelationMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;

        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values) && !string.IsNullOrWhiteSpace(values.ToString())
                ? values.ToString()
                : Guid.NewGuid().ToString();

            correlationContext.SetCorrelationId(correlationId);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            var scopeState = new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            };

            using (_logger.BeginScope(scopeState))
            {
                await _next(context);
            }
        }
    }

    public static class CorrelationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationMiddleware>();
        }
    }
}
```

**`ConsumerTestApp.Infrastructure/HttpClients/CorrelationDelegatingHandler.cs`**

```csharp
using ConsumerTestApp.Application.Common.Interfaces;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("<CorrelationDelegatingHandlerTemplate.TemplateId>", Version = "1.0")]

namespace ConsumerTestApp.Infrastructure.HttpClients
{
    public class CorrelationDelegatingHandler : DelegatingHandler
    {
        private const string HeaderName = "X-Correlation-Id";

        private readonly ICorrelationContext _correlationContext;

        public CorrelationDelegatingHandler(ICorrelationContext correlationContext)
        {
            _correlationContext = correlationContext;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = _correlationContext.CorrelationId;
            if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(HeaderName))
            {
                request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
```

**`ConsumerTestApp.Infrastructure/DependencyInjection.cs`**: no diff against the current file. The module now generates the lines that were added by hand:

```csharp
using ConsumerTestApp.Application.Common.Interfaces;
using ConsumerTestApp.Infrastructure.Configuration.Correlation;
using ConsumerTestApp.Infrastructure.HttpClients;
...
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ICorrelationContext, CorrelationContext>();
            services.AddTransient<CorrelationDelegatingHandler>();
            return services;
```

**`ConsumerTestApp.Api/Program.cs`**: no diff against the current file. The module now generates `using ConsumerTestApp.Api.Middleware;` and:

```csharp
                // Configure the HTTP request pipeline.
                app.UseCorrelationId();
                app.UseSerilogRequestLogging();
                app.UseExceptionHandler();
```

Differences from the current hand-written files that are expected and accepted:

- The four correlation files gain the `Intent.RoslynWeaver.Attributes` using and the two `[assembly: …]` attributes.
- Like every Intent-generated file, they end without a trailing newline.

Setting variations to spot-check:

- **Echo To Response off:** the `context.Response.OnStarting(...)` statement and the blank line before it disappear. Nothing else changes.
- **Correlation Header Name = `X-Request-Id`:** both `HeaderName` constants become `"X-Request-Id"`. Nothing else changes.

ConsumerTestApp builds with `dotnet build ConsumerTestApp/ConsumerTestApp.sln`.
