# AppHost.Tests

Contract tests for the AppHost project.

## Scope

- Protect the fixed Studio shell port defined in `src/Hosts/AppHost/appsettings.json`.
- Protect the preserved Studio shell Aspire wiring in `src/Hosts/AppHost/AppHost.cs`.

## Execution

Run `dotnet test .\test\AppHost.Tests\AppHost.Tests.csproj` when validating AppHost changes.
