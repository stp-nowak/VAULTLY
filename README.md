# VAULTLY

Personal Finance &amp; Budget Intelligence Platform

## Backend configuration

The identity API supports both `appsettings*.json` and a local `.env` file.

Use `backend\src\services\identity\Vaultly.Identity.Api\.env.example` as the starting point for a local `.env` file. The `.env` file is optional, but when the same key exists in both places, the `.env` value overrides `appsettings.json`.

Use native .NET hierarchical keys in `.env`, such as `Identity__Issuer` and `ConnectionStrings__IdentityDb`.
