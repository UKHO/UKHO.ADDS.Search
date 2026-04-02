# UKHO.Aspire.Configuration.Emulator.Tests

Unit-test coverage for the emulator project features delivered by Work Item 5.

## Scope

- configuration-setting factory behaviour for standard and feature-flag payloads
- feature-flag serialization and nested client-filter parameter round-tripping
- HMAC request validation, challenge responses, outbound signing, option binding, and service registration
- common utility coverage for string unescaping, JSON flattening and reconstruction, link headers, JSON projection filtering, and configuration-client pagination/request shaping

## Execution

Run the emulator test project only:

- `dotnet test test\\UKHO.Aspire.Configuration.Emulator.Tests\\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`
