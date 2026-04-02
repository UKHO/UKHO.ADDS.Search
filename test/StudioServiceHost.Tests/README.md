# StudioServiceHost.Tests

Focused integration and contract tests for the Studio service host.

## Scope

- Protect the lightweight diagnostics endpoints such as `/echo`.
- Protect the fixed HTTPS Studio shell origin used by the local CORS policy.
- Protect the existing OpenAPI and provider-composition behavior.

## Execution

Run `dotnet test .\test\StudioServiceHost.Tests\StudioServiceHost.Tests.csproj` when validating Studio service host changes.
