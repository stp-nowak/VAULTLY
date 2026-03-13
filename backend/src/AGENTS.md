## Commands
 - from backend/src directory `dotnet build` - build solution
 - from backend/src directory `dotnet test` - run tests for solution
 
## Skills
 - all skills are in `./.agents/skills` directory

## C# Instructions
- Always use the latest version C#, currently C# 14 features.
- Keep all projects targeting `net10.0` with nullable reference types enabled, matching the existing service and test projects.
- Prefer primary constructors
- Write clear and concise comments for each function.
- Use test-driven Development
- Use ddd skill when creating new C# files or refactoring existing ones

## General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.
- Always build and run tests after changes to functionality. Do not run build or tests if changes made do not effect functionality.

## Logging and Monitoring
- Write usefull logs using Serilog
- Never log raw auth codes, refresh tokens, cookie values, OAuth client secrets, or `Identity:JwtPrivateKeyPem` contents from configuration.

## Testing
- Use `test-writer` skill when creating tests.
