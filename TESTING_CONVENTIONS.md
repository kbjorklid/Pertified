# General Guidelines

- Testing strategy: Aim for 'testing diamond': a lot of 'System Tests', smaller amount of unit tests.

## Libraries

- Use XUnit for testing.
- Use NSubstitute for mocking.

# Test Project Organization

The test projects are organized in a `tests` folder that mirrors the structure of the `src` folder. For every Domain and Application project in each module, there is a corresponding unit test project.

```text
/YourApp.sln
|
├─── src
│    ├─── BuildingBlocks
│    │    ├─── Base.Domain/
│    │    ├─── Base.Application/
│    │    └─── Base.Infrastructure/
│    │
│    ├─── ModuleA
│    │    ├── ModuleA.Contracts/
│    │    ├── ModuleA.Domain/
│    │    ├── ModuleA.Application/
│    │    └── ModuleA.Infrastructure/
│    │
│    ├─── ModuleB
│    │    ├── ModuleB.Contracts/
│    │    ├── ModuleB.Domain/
│    │    ├── ModuleB.Application/
│    │    └── ModuleB.Infrastructure/
│    │
│    └─── ApiHost/
│
└─── tests
     ├─── BuildingBlocks
     │    ├─── Base.Domain.Tests/
     │    │    └── Base.Domain.Tests.csproj
     │    └─── Base.Application.Tests/
     │         └── Base.Application.Tests.csproj
     │
     ├─── ModuleA
     │    ├── ModuleA.Domain.Tests/
     │    │   └── ModuleA.Domain.Tests.csproj
     │    └── ModuleA.Application.Tests/
     │        └── ModuleA.Application.Tests.csproj
     │
     ├─── ModuleB
     │    ├── ModuleB.Domain.Tests/
     │    │   └── ModuleB.Domain.Tests.csproj
     │    └── ModuleB.Application.Tests/
     │        └── ModuleB.Application.Tests.csproj
     │
     └─── SystemTests/
          └── SystemTests.csproj
```

## Test Project Guidelines

- **Domain.Tests**: Unit tests for aggregate roots, entities, and domain services. Focus on business logic validation. Do NOT create unit tests for Value Objects.
- **Application.Tests**: Unit tests for command and query handlers. Test the orchestration logic and integration with domain objects.
- **SystemTests**: End-to-end integration tests that exercise the complete system through REST APIs, including database operations.

Note: Infrastructure and Contracts projects typically do not have dedicated unit test projects as they contain minimal logic that warrants isolated testing. Infrastructure components are tested through system tests, and Contracts contain only DTOs and interfaces.

# System Tests

- System tests mean tests that exercise the system through REST API, and/or other similar APIs.
- System tests reside in a single test project.
- System tests use real databse.
- Some mocking may be required, but should be avoided where possible
  - Examples of things that should be mocked: Email sending, LLM usage.
- If there are background services, these should not be running during the system tests. Background services' logic should be callable from the tests, simulating the background service being executed.
- System tests should aim to seed data through the external APIs
- Each test should start from an empty state. The test case is responsible for setting up the state (for example, adding data to database)

## Naming conventions

Format: `EndpointAndVerb_Scenario_ExpectedOutcome`

Examples:
- For endpoint `POST /api/jobs` :  `PostJobs_WithMissingTitle_ReturnsBadRequest()`
- For endpoint `GET /api/jobs/{id}` : `GetJobById_JobExists_ReturnsOkWithJobPayload()`
- For endpoint `DELETE /api/users/{id}` : `DeleteUser_UserIsReferencedByOrders_ReturnsConflict()`


# Unit tests

## What to unit test?

As test diamond is our goal, we aim to only unit test strategically:
- Domain logic in Aggregate Roots should be extensively tested
- Domain logic in domain services should be extensively tested

**Do NOT create unit tests for Value Objects** - their validation and behavior are sufficiently covered through Aggregate Root tests and system tests.

Unless user explicitly requests otherwise, no other logic should be unit tested (it should be tested with system tests)

## Naming Conventions

Format: `MethodToTest_Scenario_ExpectedOutcome`

Examples:
- For method `public bool IsPremium(User user)` : `IsPremium_UserHasRecentPurchase_ReturnsTrue()`
- For method `public void DeactivateUser(int userId)` : `DeactivateUser_UserNotFound_ThrowsUserNotFoundException()`


# Test Coding Practices

## General
- Use in-line comments for Arrange, Act, and Assert in each test case.

## Test Object Builders

The project uses the Builder pattern for creating test data with sensible defaults and fluent customization:

```csharp
// Simple with defaults
var jobPosting = new JobPostingBuilder().Build();

// Customized 
var jobPosting = new JobPostingBuilder()
    .WithListingItem(new JobListingItemBuilder()
        .WithTitle("Senior Software Engineer")
        .WithJobPostingSite("LinkedIn")
        .Build())
    .Build();
```

**Always use test object builders for creating test data.** This is the primary pattern for all test data creation - not a special case, but the standard approach.

Do not create helper methods like:
```csharp
var user = CreateValidUser("John", "Doe");
```

Always use builders instead:
```csharp
var user = new UserBuilder().WithFirstName("John").WithLastName("Doe").Build();
```

**Key principle: Only specify properties that are relevant to your test.** ("Assert only what you arrange"), if first name is asserted but last name is not, omit the irrelevant data (builder should use valid defaults):
```csharp
var user = new UserBuilder().WithFirstName("John").Build();
```

**Use builders for nested objects too.** When objects contain complex nested structures, use builders at every level:
```csharp
// Good - builders all the way down
.WithComplexProperty(
    new ComplexObjectBuilder().WithRelevantField("value").Build(),
    new ComplexObjectBuilder().WithDifferentField("other").Build()
)

// Avoid - mixing builders with manual construction
.WithComplexProperty(
    ("manual", new[] { "construction" }),
    ("makes", new[] { "tests", "harder", "to", "read" })
)
```

**One Builder per Type** DO NOT create multiple builder classes for one type.

**Do not use Builders in 'Act' (as the thing that is tested)**:

```csharp
// Act - Good, uses the actual code being tested:
var userId = new UserId(guid);

// Act - avoid, do not hide the code being tested
UserId userId = UserIdBuilder.Create().WithValue(guid).Build();
```

**Do not use a builder to set a single value of an object built when a constructor with that parameter exists**
```csharp
// Good
var email = new Email("some.address@test.com");

// avoid
Email email = new EmailBuilder().WithAddress("some.address@test.com").Build();
