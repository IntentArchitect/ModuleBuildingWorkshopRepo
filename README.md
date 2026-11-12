# Module Building Workshop: pre-baked workspace

The finished workspace for the _Architectural Guardrails & Agentic Module Building_ workshop. It was built from the same prompts the trainees run, with one tag per step. Use it as a reference while your agents run, as a fallback if one stalls, and as a reference for module building after the workshop.

```
├── 🤖 ModuleBuilderAiSkills  AI module-building skills & rules (outputs to the repo root)
├── 📦 WorkshopModule         the module under construction
└── 🚀 ConsumerTestApp        proves the module works (Clean Architecture, .NET 10, no MediatR)
```

The Intent solution is `intent/ModuleBuildingTraining.isln`. The pattern and plan prompts sit at the repo root (`Workshop Pattern P*.md`, `Workshop Plan P*.md`).

## Tags

| Tag | What it contains |
|---|---|
| `setup-done` | The three apps, the `Workshop` asset repository (`intent/intent.repositories.config` → `WorkshopModule/Intent.Modules`), and an empty `WorkshopModule` installed in `ConsumerTestApp` |
| `p1-done` | Practical 1: correlation IDs: four Single File templates, `Tracing Settings` module settings, DI and pipeline registration requests |
| `p2-done` | Practical 2: in-memory CQRS dispatcher: dispatcher contracts, File Per Model command/handler templates over the CQRS `Command`, and a factory extension that reaches into the ASP.NET Core Controllers module |
| `p2.5-done` | Closing tour: the `Serilog Logging` stereotype, which generates a Scrutor logging decorator per command |
| `p3-done` | Closing tour: WorkshopModule generates the consumer's agent skills (`custom-dispatcher-command-handler`, `custom-dispatcher-middleware`) |

## Using it

- Clone it into its own folder next to your workspace, and open `intent/ModuleBuildingTraining.isln` in its own Intent Architect window.
- **AI harness folders (`.claude`, `.agents`, …) are git-ignored.** After cloning or checking out a tag, run the Software Factory on `ModuleBuilderAiSkills` (and on `ConsumerTestApp` from `p3-done`) to regenerate the skills and rules.
- To look at a step, check out its tag and rebuild the module so the `.imod` matches:
  ```bash
  git checkout p1-done
  dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental
  ```
- To carry on from a step when your own run stalled, branch from its tag, e.g. `git switch -c my-p2 p1-done`.

## Pinned versions

- Intent Architect 5.4.x (`Intent.ModuleBuilder.AI.Guidelines` needs 5.4)
- .NET 10 SDK for `ConsumerTestApp` (the module project targets net8.0). On Linux, also install the ASP.NET Core targeting pack.
- PowerShell 7 for the test commands
- AI skills modules: `Intent.ModuleBuilder.AI.Guidelines` 1.0.0, `Intent.ModuleBuilder.AI.Modelers` 1.0.0, `Intent.ModuleBuilder.AI.DotNet` 1.0.1
- `ConsumerTestApp`: `Intent.Modelers.Services` 4.0.17, `Intent.Modelers.Services.CQRS` 6.0.9, `Intent.AspNetCore.Controllers` 7.1.16
- `WorkshopModule` NuGet: `Intent.Modules.Common` 3.11.6, `Intent.Modules.Common.CSharp` 3.10.11, `Intent.SoftwareFactory.SDK` 3.14.0, `Intent.Modules.AspNetCore.Controllers` 7.1.15

## Building blocks → where to look

All paths are under `WorkshopModule/WorkshopModule/` unless stated otherwise.

| Building block | Files |
|---|---|
| **Templates: Single File, C# File Builder** (P1, P2 Phase A) | `Templates/ICorrelationContext`, `CorrelationContext`, `CorrelationMiddleware`, `CorrelationDelegatingHandler`, `IAppRequest`, `IAppRequestHandler`, `IDispatcher`, `Dispatcher` |
| **Type-safe template references** | `Templates/TemplateExtensions.cs` (`GetICorrelationContextName()` …) |
| **DI and pipeline registration requests** | the `BeforeTemplateExecution()` overrides in `CorrelationContextTemplatePartial.cs`, `CorrelationMiddlewareTemplatePartial.cs`, `DispatcherTemplatePartial.cs`, `CommandHandlerTemplatePartial.cs` |
| **Module Settings** (P1) | `Settings/ModuleSettingsExtensions.cs` (`GetTracingSettings().EchoToResponse()`), used in `CorrelationMiddlewareTemplatePartial.cs` |
| **Templates: File Per Model over a designer** (P2) | `Templates/CommandRequest`, `Templates/CommandHandler` (Services designer → CQRS `Command`) |
| **Factory Extension: reaching into another module** (P2) | `FactoryExtensions/CqrsControllerInstaller.cs`, `Templates/CqrsController/CqrsControllerModel.cs` (`IControllerModel`) |
| **Stereotypes** (P2.5) | `Api/CommandModelStereotypeExtensions.cs`, `Templates/SerilogLoggingDecorator` |
| **Non-code templates: Markdown File Builder** (P3) | `Templates/CommandHandlerSkill`, `Templates/CorrelationMiddlewareSkill` (Role `AI.Context.Skills`) |
| **Module manifest** | `WorkshopModule.imodspec`; the built module is `WorkshopModule/Intent.Modules/WorkshopModule.1.0.0.imod` |

Generated consumer output: `ConsumerTestApp/ConsumerTestApp.Api/Middleware`, `ConsumerTestApp.Api/Controllers/ProductsController.cs`, `ConsumerTestApp.Application/Common/Dispatching`, `ConsumerTestApp.Application/Commands/Products`, and the registrations in both `DependencyInjection.cs` files and `Program.cs`.

## Trying it

```powershell
dotnet run --project ConsumerTestApp/ConsumerTestApp.Api
# port from ConsumerTestApp.Api/Properties/launchSettings.json (44387)

# P1: echoed / generated correlation ID
(Invoke-WebRequest "https://localhost:44387/does-not-exist" -SkipHttpErrorCheck -SkipCertificateCheck -Headers @{ "X-Correlation-Id" = "abc-123" }).Headers["X-Correlation-Id"]

# P2: 500 from the handler stub, correlation ID kept on the error response
Invoke-WebRequest -Uri "https://localhost:44387/api/products" -Method POST -SkipHttpErrorCheck -SkipCertificateCheck `
  -ContentType "application/json" -Headers @{ "X-Correlation-Id" = "workshop-123" } `
  -Body '{"title":"Mechanical Keyboard","price":149.99}'
```

`-SkipCertificateCheck` is only needed where the ASP.NET Core dev certificate isn't trusted (common on Linux).

## Landmines

- **A module change doesn't reach the consumer.** Rebuild with `dotnet build WorkshopModule/ModuleBuildingTraining.sln --no-incremental`, delete `intent/.cache/modules/WorkshopModule.1.0.0`, then update `WorkshopModule` in `ConsumerTestApp`. At the same version, Intent can otherwise keep the stale cached copy.
- **Green module, broken consumer.** The module build compiles the template code, not the code it generates. Always build `ConsumerTestApp` too.
- **First generation over a hand-written pattern** shows as "could not be verified as safe". Read the diff: only Intent's header and attributes should differ.
- **Reload dialogs.** Rebuilding the `.imod` can raise "Underlying metadata files have changed… reload?", which blocks agent tools until someone clicks **Reload**.
- **An agent is decompiling NuGet packages.** Point it at the matching skill: `intent-modelers-integration` or `intent-dotnet-module-integration`.
- **Unsaved IDE buffers.** Save everything before running the Software Factory.
