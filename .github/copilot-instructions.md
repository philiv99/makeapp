# GitHub Copilot Instructions

This file provides context and guidelines for GitHub Copilot when working in this repository.

## Project Overview

MakeApp is a C#.NET Web API service that orchestrates feature development workflows using GitHub Copilot. It exposes RESTful endpoints for managing repositories, branches, feature requirements, and AI-driven code generation workflows. The API enables client applications to automate branch management, capture feature requirements, and create pull requests programmatically.

## Code Style Guidelines

### C# Conventions
- Follow Microsoft's C# coding conventions and .NET naming guidelines
- Use PascalCase for public members, camelCase for private fields with underscore prefix (`_fieldName`)
- Include XML documentation comments for all public types and members
- Use `async/await` for all I/O-bound operations
- Prefer expression-bodied members for simple single-line implementations
- Use nullable reference types (`#nullable enable`)
- Use file-scoped namespaces
- Prefer records for DTOs and immutable data structures

### File Organization
- Follow Clean Architecture with separate projects:
  - `MakeApp.Api` - Controllers, middleware, and API configuration
  - `MakeApp.Application` - Services, DTOs, validators, and mappings
  - `MakeApp.Core` - Entities, interfaces, enums, and domain logic
  - `MakeApp.Infrastructure` - External service implementations, persistence
- Configuration in `appsettings.json` and `appsettings.{Environment}.json`
- Templates in `/templates` directory
- Tests in separate test projects mirroring the main project structure

## Architecture Patterns

### Clean Architecture Layers
1. **Core** - Domain entities, interfaces, and business rules (no dependencies)
2. **Application** - Use cases, DTOs, validators, AutoMapper profiles
3. **Infrastructure** - External service implementations (Git, GitHub CLI, file system)
4. **Api** - Controllers, middleware, API versioning, Swagger configuration

### Dependency Injection
- Register services in `DependencyInjection.cs` in each layer
- Use `IServiceCollection` extension methods for clean registration
- Prefer `AddScoped` for request-scoped services
- Use `IOptions<T>` pattern for configuration

### Controller Conventions
- Use attribute routing with `[Route("api/v{version:apiVersion}/[controller]")]`
- Return `ActionResult<T>` for typed responses
- Use proper HTTP status codes (200, 201, 400, 404, 500)
- Validate inputs using FluentValidation
- Keep controllers thin - delegate to services

### Error Handling
- Use custom exception types in `MakeApp.Core.Exceptions`
- Implement global exception handling middleware
- Return consistent error response DTOs
- Log errors with structured logging (Serilog)
- Never expose internal exception details to clients

## Testing Guidelines

- Write xUnit tests for all public services and controllers
- Use descriptive test names following `MethodName_Scenario_ExpectedResult` pattern
- Mock external dependencies using Moq
- Follow Arrange-Act-Assert pattern
- Maintain ≥80% code coverage for new code
- Include integration tests for API endpoints
- Use `WebApplicationFactory<Program>` for integration tests

### Test Project Structure
- `MakeApp.Api.Tests` - Controller and integration tests
- `MakeApp.Application.Tests` - Service, validator, and mapper tests
- `MakeApp.Infrastructure.Tests` - External service implementation tests
- `MakeApp.E2E.Tests` - End-to-end workflow tests

## Documentation

- Update README.md for user-facing changes
- Include XML comments for all public APIs
- Keep Swagger/OpenAPI documentation accurate
- Keep the `docs/convert-to-api.md` file steps status up to date

## Security Considerations

- Never commit secrets or credentials
- Use environment variables or Azure Key Vault for sensitive configuration
- Validate all user inputs using FluentValidation
- Implement proper authorization on endpoints
- Sanitize paths to prevent directory traversal attacks
- Use HTTPS in production

## Dependencies

- **.NET 8** - Runtime and SDK
- **ASP.NET Core** - Web API framework
- **Serilog** - Structured logging
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping
- **xUnit, Moq** - Testing frameworks
- **Git** - Version control operations (via Infrastructure layer)
- **GitHub CLI (`gh`)** - GitHub operations and Copilot integration

---
*This file helps GitHub Copilot understand the project context and generate more relevant suggestions.*