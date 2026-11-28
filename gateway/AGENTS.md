# Vaultly Gateway

`Vaultly.Gateway` (YARP) is the single entry to the domain services. Clients (today the web API/BFF, later a mobile
app or webhooks) call it with the Identity service's access token; services only ever see a token the gateway
issued for them. Flow and security model: `../docs/architecture/authentication.md`.

## Responsibilities
| What | How |
|---|---|
| Authenticate every request | Identity access token (RS256, `UserTokens:Issuer`, `UserTokens:Audience` = `vaultly.api`), keys from `UserTokens:JwksUrl` — not from discovery, which advertises the browser-facing URL a container may not reach |
| Coarse authorization | No `AuthorizationPolicy` on a route = any signed-in user (fallback policy); `admin` = role `admin`; YARP's `anonymous` = everyone (webhooks; the service checks their signature) |
| Route | `ReverseProxy` section: route by path prefix, path forwarded unchanged; the route's cluster id names the service |
| Internal token | Replaces `Authorization` on every forwarded request: issuer `InternalTokens:Issuer`, audience = cluster id, claims `sub`/`sid`/`role` only, ≤ `LifetimeMinutes` and never longer than the user's token; public key at `/.well-known/jwks.json` |
| Own errors | 401/403 and failed forwards (502/504) as `application/problem+json`; services' responses pass through untouched |
| Health | `/health/live`; `/health/ready` = Identity's signing keys loaded |

Not here: business logic, response composition, sessions/cookies/CSRF (BFF), resource ownership checks (services).
Planned: instant session revocation (Identity events → Redis), plan-based feature gates and rate limits (Billing).

## Commands
- from `gateway/src`: `dotnet build Vaultly.Gateway.slnx`, `dotnet test Vaultly.Gateway.slnx`
- `dotnet run --project Vaultly.Gateway` — http://localhost:5000 (the BFF's `Web:GatewayBaseUrl` in Development);
  needs the Identity service on http://localhost:5096
- Docker: `docker compose -f gateway/docker-compose.yml up --build` (from the repo root)

## Structure
```
Vaultly.Gateway/
├── Program.cs          # composition only
├── UserTokens/         # Identity token validation, JWKS retriever, policies, readiness check
├── InternalTokens/     # signing key, issuer, YARP transform, JWKS endpoint
└── Http/               # problem responses, health endpoints
```

## Rules
- No DDD layering — this is an edge, not a bounded context.
- Claim names and roles come from `Vaultly.Identity.PublicTypes` (project reference to `../auth`); never spell them
  by hand. The Docker image gets that project as the named build context `identity-public-types`.
- Adding a service = a route + cluster in `ReverseProxy` (cluster id = the service's name and token audience).
- Never forward the user's access token or log any token or the signing key.
- C#: latest language version, `net11.0`, nullable, primary constructors, minimal comments (root rule), Serilog,
  NUnit tests (`GatewayFactory` hosts the gateway with Identity's keys and the services stubbed).
