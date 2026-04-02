# Keycloak and Workbench integration

## Purpose

This document explains how Keycloak is wired into the local Aspire developer environment for Workbench, how the `ukho-search` realm is configured, how role claims flow into Workbench authorization, and how to recreate the full realm export later when users, roles, groups, clients, or mappers change.

This is intended to be the single operational reference for the local Keycloak setup.

## Current realm and import file

The current realm name is:

- `ukho-search`

The realm import file used by Aspire is:

- `src/Hosts/AppHost/Realms/ukho-search-realm.json`

Keycloak is strict about the import file naming convention. The filename must match the realm name inside the JSON:

- realm name in JSON: `ukho-search`
- required filename: `ukho-search-realm.json`

If the filename and the internal realm name do not match exactly, Keycloak startup import fails.

## Where the integration is configured in code

### Aspire AppHost

Keycloak is added to the local Aspire environment in:

- `src/Hosts/AppHost/AppHost.cs`

The important setup is the Keycloak resource registration:

- `builder.AddKeycloak(ServiceNames.KeyCloak, 8080, keyCloakUsernameParameter, keyCloakPasswordParameter)`
- `.WithDataVolume()`
- `.WithRealmImport("./Realms")`

What this means:

- Aspire starts a local Keycloak container
- Keycloak persists its database in a Docker volume
- Keycloak imports realm JSON files from the `Realms` folder on first startup against an empty data volume

Because `.WithDataVolume()` is enabled, changing the JSON file later does **not** update an already-imported realm automatically. To force a clean re-import after changing the JSON, the Keycloak data volume must be deleted before restarting Aspire.

### Workbench OpenID Connect setup

Workbench authentication is configured in:

- `src/Workbench/server/WorkbenchHost/Program.cs`

Workbench is configured to use Keycloak with the `ukho-search` realm via:

- `AddKeycloakOpenIdConnect("keycloak", "ukho-search", oidcScheme, options => ...)`

Important options in the current setup:

- `ClientId = "search-workbench"`
- `ResponseType = OpenIdConnectResponseType.Code`
- `RequireHttpsMetadata = false`
- `SaveTokens = true`
- cookie auth is used as the sign-in scheme

This means Workbench authenticates users with the Keycloak client:

- `search-workbench`

### Login and logout endpoints

Workbench maps explicit auth endpoints in:

- `src/Workbench/server/WorkbenchHost/Extensions/LoginLogoutEndpointRouteBuilderExtensions.cs`

Current endpoints:

- `GET /authentication/login`
- `GET /authentication/logout`
- `POST /authentication/logout`

These are useful during testing when forcing a fresh login after mapper or role changes.

## How role claims reach Workbench

Role claim transformation is implemented in:

- `src/Workbench/server/WorkbenchHost/Extensions/KeycloakRealmRoleClaimsTransformation.cs`

The transformer currently reads role information from these claim shapes:

- `roles`
- `realm_access.roles`
- `realm_access` containing JSON with a `roles` array

It then adds ASP.NET role claims of type:

- `System.Security.Claims.ClaimTypes.Role`

This is why the mapper configuration matters.

## Required Keycloak mapper configuration

For the Workbench client, the role mapper is configured in Keycloak under:

- `Clients`
- select client `search-workbench`
- `Client details`
- `Dedicated scopes`
- `Mapper details`

Important required settings:

- the mapping is for **realm roles**
- token claim name must be `realm_access.roles`
- `Add to ID token` must be **On**

Why this matters:

- the Workbench login principal is built from OpenID Connect identity information
- the claims transformer reads the claims on that principal
- if the mapper is not emitted into the ID token, the roles will not appear in Workbench even if they exist in Keycloak

### Very important: assign the correct role type

This mapping is for **realm roles**, not client roles.

When assigning permissions to a user, make sure you add the role as a **realm role** on the user. If a client role is assigned instead, the current mapper/transformer path will not produce the expected Workbench role claims.

## Accessing Keycloak locally

When launching through Aspire, access the Keycloak admin UI from the **HTTP** endpoint shown in the Aspire dashboard.

- use the **HTTP** endpoint
- do **not** use the HTTPS endpoint

For this local setup, the HTTPS endpoint does not work for the admin UI path used during development.

The default Keycloak admin username and password are available in the Aspire dashboard:

- open the Aspire dashboard
- go to the `Parameters` tab
- find the Keycloak username and password parameters there

## Fresh realm import behavior

The realm import JSON is only used automatically when Keycloak starts with a fresh empty data store.

If the Keycloak volume already contains a previously imported realm, Keycloak will keep using the persisted state and ignore later JSON changes.

To force a clean import:

1. stop Aspire or stop the Keycloak container
2. delete the persisted Keycloak Docker volume
3. restart Aspire

This causes Keycloak to start from scratch and import the realm JSON from `src/Hosts/AppHost/Realms/` again.

## Full realm export: when to use it

Use a full realm export whenever you change any of the following in Keycloak and want those changes preserved in source control for future local environments:

- users
- groups
- realm roles
- group memberships
- user role assignments
- clients
- client scopes
- protocol mappers
- redirect URIs
- client secrets or settings
- realm settings generally

Do **not** rely on the Keycloak admin UI partial export for this. The UI export is not the right mechanism for a complete local bootstrap.

Use the Keycloak CLI export command instead.

## Full realm export procedure

These steps produce a **full export** that includes users, groups, roles, mappings, clients, scopes, and other realm data.

### Summary

- stop the running Keycloak container first
- use a one-off container to run the export offline against the same mounted Keycloak data
- export to a host folder
- copy the generated file into `src/Hosts/AppHost/Realms/`
- ensure the filename matches the realm name exactly

### Step 1: find the running container

Example current container name:

- `keycloak-48f329f9`

To find it later if the name changes:

```powershell
docker ps --format "{{.Names}}`t{{.Image}}" | Select-String keycloak
```

### Step 2: find the image used by the container

```powershell
docker inspect keycloak-48f329f9 --format "{{.Config.Image}}"
```

This returns the image name to use in the export command.

### Step 3: stop the Keycloak container

Keycloak CLI export is an offline operation for this local H2-backed setup.

```powershell
docker stop keycloak-48f329f9
```

### Step 4: create a host export folder

```powershell
New-Item -ItemType Directory -Force D:\Temp\keycloak-export | Out-Null
```

### Step 5: run the full export

Run a one-off container using the same persisted Keycloak data volume as the stopped container.

```powershell
docker run --rm --volumes-from keycloak-48f329f9 -v D:\Temp\keycloak-export:/export <image-from-step-2> export --realm ukho-search --users same_file --file /export/ukho-search-realm.json
```

Example using an explicit image value:

```powershell
docker run --rm --volumes-from keycloak-48f329f9 -v D:\Temp\keycloak-export:/export quay.io/keycloak/keycloak:latest export --realm ukho-search --users same_file --file /export/ukho-search-realm.json
```

Important flag:

- `--users same_file`

This is what puts users into the same realm JSON so the export is suitable for a complete local developer bootstrap.

### Step 6: copy the exported file into the repo

```powershell
Copy-Item D:\Temp\keycloak-export\ukho-search-realm.json D:\Dev\UKHO\UKHO.Search\src\Hosts\AppHost\Realms\ukho-search-realm.json -Force
```

### Step 7: restart Keycloak if needed

If you stopped only the Keycloak container and are not immediately restarting Aspire:

```powershell
docker start keycloak-48f329f9
```

## File naming rules for AppHost/Realms

The import file in `src/Hosts/AppHost/Realms/` must follow this format:

- `<realm-name>-realm.json`

For the current realm, that means:

- `ukho-search-realm.json`

Do not use:

- `UKHOSearch-realm.json`
- `ADDSSearch-realm.json`
- `ukho-search-realm-original.json`

Those may cause Keycloak import problems if they end up in the import directory and do not match the internal realm name.

## Known issue: stale import files in the Docker volume

During troubleshooting it is possible to end up with stale files in the Keycloak Docker volume under:

- `/opt/keycloak/data/import`

That can happen even when the repo contains only the correct file.

To inspect the import directory in the Keycloak data volume:

```powershell
docker inspect keycloak-48f329f9 --format "{{json .Mounts}}"
```

If the container is stopped, you can inspect the volume contents with a temporary Alpine container:

```powershell
docker run --rm -v <keycloak-volume-name>:/data alpine sh -c "ls -la /data/import"
```

Only the correctly named realm file should remain there for a clean startup.

## Forcing a clean re-import after changing the realm file

If the realm JSON is changed and you want Keycloak to recreate the realm from that file:

1. stop the Keycloak container or stop Aspire
2. delete the Keycloak data volume
3. restart Aspire

To identify the Keycloak volume from the current container:

```powershell
docker inspect keycloak-48f329f9 --format "{{json .Mounts}}"
```

Once you have the volume name, delete it with:

```powershell
docker volume rm <keycloak-volume-name>
```

This deletes the persisted Keycloak database and import cache state, so only do this when you intentionally want a fresh bootstrap.

## Operational checklist when realm changes are made

When users, roles, groups, or mappers are updated in Keycloak:

1. make the changes in the Keycloak UI
2. verify the mapper for `search-workbench` still outputs `realm_access.roles`
3. verify `Add to ID token` is still on
4. export the realm again using the full CLI export procedure above
5. copy the file back to `src/Hosts/AppHost/Realms/ukho-search-realm.json`
6. if you want to test the import path itself, delete the Keycloak volume and restart Aspire
7. log into Workbench and confirm the expected role claims appear

## Security note

A full realm export can contain sensitive development data, including:

- users
- password hashes
- client settings
- secrets depending on client configuration

Treat the export file accordingly and review it carefully before committing changes.
