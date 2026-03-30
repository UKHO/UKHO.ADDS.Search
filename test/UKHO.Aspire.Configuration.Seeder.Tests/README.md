# UKHO.Aspire.Configuration.Seeder.Tests

Executable unit coverage for `configuration/UKHO.Aspire.Configuration.Seeder`.

## Scope

- Additional configuration key generation, file enumeration, and file-backed seeding behaviour.
- JSON comment stripping, JSON flattening, and external service definition parsing.
- Seeder orchestration, retry behaviour, hosted-service lifecycle, and best-effort `Program` helper coverage.
- Deterministic file-system and environment-variable-backed test scenarios using handwritten test doubles.

## Execution

Run `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`.
