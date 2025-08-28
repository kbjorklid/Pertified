# Testing Conventions for Claude Code

This document provides testing guidelines specifically for code generation and test writing by Claude Code.

## Quick Reference

### Testing Strategy (Test Diamond)
- **Primary**: System Tests (extensive coverage through REST APIs)
- **Secondary**: Unit Tests (only for domain logic in Aggregate Roots and Domain Services)
- **Never**: Unit tests for Value Objects

### Required Libraries
- **XUnit**: Test framework
- **NSubstitute**: Mocking framework

### Test Data Creation
- **Always use Test Object Builders** for creating test data
- **Never create helper methods** like `CreateValidUser()`
- **Only specify properties relevant to the test** ("Assert only what you arrange")

---

# Project Structure & Organization

## Test Project Structure
Tests mirror the `src` folder structure. Each Domain and Application project has a corresponding test project.

```text
tests/
├─── BuildingBlocks/
│    ├─── Base.Domain.Tests/          # Base domain logic tests
│    └─── Base.Application.Tests/     # Base application tests
├─── {ModuleName}/
│    ├─── {ModuleName}.Domain.Tests/      # Domain unit tests
│    └─── {ModuleName}.Application.Tests/ # Application unit tests
└─── SystemTests/                     # End-to-end system tests
```

## What Each Test Project Contains

### {ModuleName}.Domain.Tests
- **Purpose**: Unit tests for Aggregates and Domain Services
- **Focus**: Business logic validation
- **❌ Do NOT test**: Value Objects (tested through Aggregate Root tests)
- **Other instructions**: Aim to test as much of the aggregate as possible through the aggregate root (do not test entities that are not aggregate roots separately)

### {ModuleName}.Application.Tests  
- **Purpose**: Unit tests for Command and Query Handlers (**avoid when possible**)
- **Focus**: Complex orchestration logic that's hard to reach through REST APIs
- **Preference**: Use system tests instead - only create Application unit tests for complex logic that cannot be easily tested through the REST API

### SystemTests
- **Purpose**: End-to-end integration tests through REST APIs
- **Scope**: Complete system functionality with real database

### Projects WITHOUT Test Coverage
- **Infrastructure**: Tested through system tests
- **Contracts**: Contains only DTOs and interfaces

# System Tests (Primary Testing Strategy)

System tests exercise the complete system through REST APIs and form the bulk of our test coverage.

## System Test Requirements

### ✅ Required Practices
- **Test through REST APIs**: Exercise endpoints as external clients would
- **Use real database**: No database mocking
- **Start from empty state**: Each test sets up its own data
- **Seed data through APIs**: Use external APIs to create test data
- **Single test project**: All system tests in `SystemTests/`

### ⚠️ Mocking Guidelines
- **Minimize mocking**: Avoid where possible
- **Mock external services only**: Email sending, LLM usage, payment gateways
- **Mock background services**: Don't run them during tests; call their logic directly

### 🚫 Prohibited
- Running background services during tests
- Starting tests with pre-existing data
- Database mocking

## System Test Naming Convention

**Format**: `{EndpointAndVerb}_{Scenario}_{ExpectedOutcome}`

### Examples by HTTP Method
```csharp
// POST endpoints
PostJobs_WithMissingTitle_ReturnsBadRequest()
PostUsers_WithValidData_ReturnsCreatedWithUserId()

// GET endpoints  
GetJobById_JobExists_ReturnsOkWithJobPayload()
GetJobById_JobNotFound_ReturnsNotFound()

// PUT endpoints
PutUser_WithValidUpdate_ReturnsOkWithUpdatedUser()
PutUser_UserNotFound_ReturnsNotFound()

// DELETE endpoints
DeleteUser_UserExists_ReturnsNoContent()
DeleteUser_UserIsReferencedByOrders_ReturnsConflict()
```

## Database for system tests

System tests of modules that use a database must use Testcontainers to use the actual database type (PostgreSQL).

# Unit Tests (Strategic Coverage Only)

Unit tests provide targeted coverage for critical domain logic following the test diamond strategy.

## When to Create Unit Tests

### ✅ ALWAYS Unit Test
- **Aggregate Root business logic**: Extensively test all domain rules
- **Domain Service logic**: Extensively test domain orchestration

### ❌ AVOID Unit Testing (Prefer System Tests)
- **Value Objects**: Covered through Aggregate Root and system tests
- **Application Services**: Use system tests instead (only unit test if logic is complex and hard to reach via REST API)
- **Infrastructure components**: Use system tests instead

### 🤔 Only if User Explicitly Requests
- Any other logic not listed above

## Unit Test Naming Convention

**Format**: `{MethodToTest}_{Scenario}_{ExpectedOutcome}`

### Examples by Method Type
```csharp
// Boolean methods
IsPremium_UserHasRecentPurchase_ReturnsTrue()
IsPremium_UserHasNoRecentPurchase_ReturnsFalse()

// Void methods with exceptions
DeactivateUser_UserNotFound_ThrowsUserNotFoundException()
DeactivateUser_ValidUser_DeactivatesSuccessfully()

// Methods returning values
CalculateDiscount_PremiumUser_ReturnsDiscountedPrice()
CalculateDiscount_RegularUser_ReturnsFullPrice()

// Methods with state changes
AddItemToCart_ValidItem_AddsItemAndIncreasesTotalPrice()
AddItemToCart_DuplicateItem_ThrowsDuplicateItemException()
```


# Test Implementation Guidelines

## Required Test Structure
- **Use AAA pattern**: Add inline comments for Arrange, Act, and Assert sections
- **Use Test Object Builders**: Always use builders for test data creation

## Test Object Builders (Mandatory Pattern)

### ✅ Always Use Builders For Test Data

Test Object Builders are the **standard approach** for ALL test data creation, not an optional pattern.

```csharp
// ✅ Simple with defaults
var jobPosting = new JobPostingBuilder().Build();

// ✅ Customized with fluent API
var jobPosting = new JobPostingBuilder()
    .WithListingItem(new JobListingItemBuilder()
        .WithTitle("Senior Software Engineer")
        .WithJobPostingSite("LinkedIn")
        .Build())
    .Build();
```

### ❌ Never Create Helper Methods For Building Test Objects
```csharp
// ❌ Don't do this
var user = CreateValidUser("John", "Doe");

// ✅ Do this instead
var user = new UserBuilder().WithFirstName("John").WithLastName("Doe").Build();
```

### 🎯 Key Principle: "Assert Only What You Arrange"
Only specify properties that are relevant to your specific test:

```csharp
// ✅ Only specify what you're testing
var user = new UserBuilder().WithFirstName("John").Build();

// ❌ Don't specify irrelevant properties
var user = new UserBuilder()
    .WithFirstName("John")
    .WithLastName("irrelevant")  // Not testing this
    .WithAge(25)                 // Not testing this
    .Build();
```

### Use Inline Literals in Assertions

**Principle**: Prefer inline literal values in assertions over introducing variables, making tests more direct and readable.

**Why**: Variables add indirection and cognitive load. Inline literals clearly show what the test expects without requiring readers to track variable definitions.

#### ✅ Preferred: Inline Literals
```csharp
// ✅ Direct and clear - no variables to track
public void ChangeEmail_WithValidNewEmail_UpdatesEmailSuccessfully()
{
    // Arrange
    User user = new UserBuilder().Build();

    // Act
    Result result = user.ChangeEmail("newemail@example.com");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("newemail@example.com", user.Email.Value.Address);
}

// ✅ Multiple assertions with literals
public void CreateUser_WithValidData_SetsAllPropertiesCorrectly()
{
    // Arrange & Act
    var user = new User(new UserId(Guid.NewGuid()), "john.doe@example.com", "John Doe");

    // Assert
    Assert.Equal("john.doe@example.com", user.Email.Value);
    Assert.Equal("John Doe", user.Name.Value);
    Assert.Equal(UserStatus.Active, user.Status);
}
```

#### ❌ Avoid: Unnecessary Variables
```csharp
// ❌ Extra variables create indirection
public void ChangeEmail_WithValidNewEmail_UpdatesEmailSuccessfully()
{
    // Arrange
    User user = new UserBuilder().Build();
    string newEmail = "newemail@example.com";  // Unnecessary variable

    // Act
    Result result = user.ChangeEmail(newEmail);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(newEmail, user.Email.Value.Address);  // Now reader must track variable
}
```

#### 🎯 When Variables Are Acceptable

Use variables only when they provide genuine value:

```csharp
// ✅ Variable justified - used in multiple places AND complex
public void ProcessOrder_WithMultipleItems_CalculatesTotalCorrectly()
{
    // Arrange
    decimal itemPrice = 19.99m;  // Used 3 times, easier than repeating literal
    var order = new OrderBuilder()
        .WithItem("Product A", itemPrice)
        .WithItem("Product B", itemPrice)
        .WithItem("Product C", itemPrice)
        .Build();

    // Act
    order.ProcessOrder();

    // Assert
    Assert.Equal(itemPrice * 3, order.Total);
}

// ✅ Variable justified - complex construction
public void ValidateEmail_WithLongDomain_RejectsCorrectly()
{
    // Arrange
    string longDomain = new string('a', 64) + ".com";  // Complex to inline
    
    // Act
    Result result = Email.Create($"user@{longDomain}");

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("domain too long", result.Error);
}
```

#### 🎯 Exception: Extract Result Values to Reduce Repetition

When testing methods that return `Result<T>`, extract the value to avoid repetitive `result.Value` calls that hurt readability:

```csharp
// ❌ Repetitive Result.Value access creates noise
public void Register_WithValidData_CreatesUserSuccessfully()
{
    // Act
    Result<User> result = User.Register("john.doe@example.com", "johndoe");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("john.doe@example.com", result.Value.Email.Value.Address);
    Assert.Equal("johndoe", result.Value.UserName.Value);
}

// ✅ Extract value once for cleaner assertions
public void Register_WithValidData_CreatesUserSuccessfully()
{
    // Act
    Result<User> result = User.Register("john.doe@example.com", "johndoe");

    // Assert
    Assert.True(result.IsSuccess);
    User user = result.Value;
    Assert.Equal("john.doe@example.com", user.Email.Value.Address);
    Assert.Equal("johndoe", user.UserName.Value);
}

// ✅ For error cases, extract error for clarity
public void Register_WithInvalidEmail_ReturnsValidationError()
{
    // Act
    Result<User> result = User.Register("invalid-email", "johndoe");

    // Assert
    Assert.False(result.IsSuccess);
    string error = result.Error;
    Assert.Contains("Invalid email format", error);
}
```


### 🏗️ Builder Nesting Rules

**Use builders at every level** for complex nested structures:
```csharp
// ✅ Builders all the way down
.WithComplexProperty(
    new ComplexObjectBuilder().WithRelevantField("value").Build(),
    new ComplexObjectBuilder().WithDifferentField("other").Build()
)

// ❌ Mixed builder/manual construction
.WithComplexProperty(
    ("manual", new[] { "construction" }),
    ("makes", new[] { "tests", "harder", "to", "read" })
)
```

### 🚫 Builder Restrictions

**Don't use Builders in 'Act' section**:
```csharp
// ✅ Act - Test the actual code
var userId = new UserId(guid);

// ❌ Act - Don't hide the code being tested  
UserId userId = UserIdBuilder.Create().WithValue(guid).Build();
```

**Don't use builders for simple constructor calls**:
```csharp
// ✅ Direct constructor when available
var email = new Email("some.address@test.com");

// ❌ Unnecessary builder usage
Email email = new EmailBuilder().WithAddress("some.address@test.com").Build();
```
**Exception: Use builders when parameter value is irrelevant**:
```csharp
// ✅ Use builder to "hide" irrelevant values
Email email = new EmailBuilder().Build();

// ❌ Noise by having to define parameter value not used by test
var email = new Email("this.address.is.not.relevant.to.this@test.com");
```

---

# Quick Decision Guide

## "Should I write tests for this?"

```
Is it an Aggregate Root or Domain Service?
├─ YES → Write unit tests
└─ NO → Is it a Value Object?
   ├─ YES → No tests (covered by Aggregate Root tests)
   └─ NO → Is it Infrastructure/Application layer?
      ├─ YES → Use system tests only  
      └─ NO → Use system tests only
```

## "What type of test should I write?"

```
Testing through REST API endpoints?
├─ YES → System Test
└─ NO → Testing domain logic directly?
   ├─ YES → Unit Test (if Aggregate Root/Domain Service)
   └─ NO → Don't test (or use system tests)
```

## "How do I name my test?"

```
System Test: {EndpointAndVerb}_{Scenario}_{ExpectedOutcome}
Example: PostUsers_WithInvalidEmail_ReturnsBadRequest

Unit Test: {MethodToTest}_{Scenario}_{ExpectedOutcome}  
Example: CalculateDiscount_PremiumUser_ReturnsDiscountedPrice
```

## "How do I create test data?"

```
✅ ALWAYS: Use Test Object Builders
✅ PRINCIPLE: Only specify properties relevant to the test
❌ NEVER: Create helper methods like CreateValidUser()
❌ NEVER: Use builders in the 'Act' section
```