# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Pertified** is a .NET 9.0 backend service for advanced project planning and estimation, built as a **Modular Monolith** using Domain-Driven Design, Clean Architecture, and CQRS patterns. It serves as a complementary analysis layer for existing project management systems like Jira.

## Development Commands

### Building and Testing
```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SystemTests/SystemTests.csproj
dotnet test tests/BuildingBlocks/Base.Domain.Tests/Base.Domain.Tests.csproj

# Run API locally
dotnet run --project src/ApiHost/ApiHost.csproj
```

### Module Management
```powershell
# Create new module with complete structure
.\Add-Module.ps1 -ModuleName "ModuleName"

# Create new modular monolith from scratch
.\Create-ModularMonolith.ps1 -SolutionName "ProjectName" -DotNetVersion "net9.0"
```

## Architecture

### Modular Monolith Structure
The solution is organized as a modular monolith where each **Module** represents a **Bounded Context** from DDD:

```
src/
├── BuildingBlocks/           # Shared cross-cutting concerns
│   ├── Base.Domain/          # Base DDD classes (Entity, AggregateRoot, etc.)
│   ├── Base.Application/     # Shared application patterns
│   └── Base.Infrastructure/  # Common infrastructure logic
├── ApiHost/                  # Composition root & API controllers
└── [ModuleName]/            # Business modules (created via Add-Module.ps1)
    ├── [ModuleName].Contracts/    # Public API (Commands, Queries, Events, DTOs)
    ├── [ModuleName].Domain/       # Core business logic
    ├── [ModuleName].Application/  # Use cases & orchestration
    └── [ModuleName].Infrastructure/ # External service implementations
```

### Clean Architecture Layers (within each module)
Dependencies flow inward: `Domain ← Application ← Infrastructure`

- **Domain**: Core business logic, zero external dependencies
- **Application**: Use cases orchestrating domain logic, depends only on Domain and Contracts
- **Infrastructure**: External service implementations, depends on Application

### CQRS Implementation
- **Commands** (writes): Use full Domain model through repositories
- **Queries** (reads): Bypass Domain model, query data directly for performance

### Inter-Module Communication
- Modules communicate only through `Contracts` projects
- No direct references to other modules' Domain/Application/Infrastructure
- Message bus mediates all cross-module interactions

## Key Conventions

### Naming
- Entity IDs: Strongly typed value objects (e.g., `UserId`, not `Guid`)
- Commands: End with `Command` suffix
- Queries: End with `Query` suffix  
- Handlers: End with `CommandHandler`/`QueryHandler` suffix
- Domain Events: End with `DomainEvent` suffix
- Domain Services: End with `DomainService` suffix
- Repository interfaces: End with `Repository` suffix
- Ports: End with `Port` suffix
- Adapters: End with `Adapter` suffix

### Testing Strategy
- **Test Diamond**: Emphasis on system tests, fewer unit tests
- **XUnit** for testing framework, **NSubstitute** for mocking
- **System Tests**: Exercise full system via REST API with real database
- **Unit Tests**: Focus on Domain logic (Aggregates, Entities) and complex Application logic
- **Test Object Builders**: Always use builder pattern for test data creation

### Error Handling & Mapping
- Default: Use result pattern
- Validation errors: Throw validation exceptions
- Manual mapping only (no AutoMapper)
- Methods returning 0-1 values: Return nullable types

## Project Dependencies Flow

1. **Domain** → Base.Domain only
2. **Application** → Domain + Contracts + Base.Application + (other module Contracts)
3. **Infrastructure** → Application + Base.Infrastructure
4. **ApiHost** → All module Applications + Contracts
5. **Tests** → Corresponding source projects

## Development Guidelines

- Entity identifiers must be strongly typed value objects
- Value Objects validate at construction time
- Manual mapping between layers (extension methods/static methods)
- Always use Test Object Builders for test data
- Follow REST conventions: kebab-case URIs, camelCase JSON, versioned APIs
- Commands change state, Queries read data - never mix responsibilities