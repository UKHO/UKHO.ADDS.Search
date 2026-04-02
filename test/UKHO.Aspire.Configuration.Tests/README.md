# UKHO.Aspire.Configuration.Tests

Executable unit coverage for `configuration/UKHO.Aspire.Configuration`.

## Scope

- `AddsEnvironment` parsing, equality, helper, and environment-variable behaviours.
- `ConfigurationExtensions` local endpoint resolution and non-local App Configuration registration metadata.
- `ExternalServiceRegistry` and `ExternalEndpoint` remote endpoint resolution behaviour.

## Execution

Run `dotnet test test\UKHO.Aspire.Configuration.Tests\UKHO.Aspire.Configuration.Tests.csproj --no-restore`.
