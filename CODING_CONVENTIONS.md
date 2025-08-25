# DDD class implementations

## Value Objects

- Value Objects must validate data at construction time.
- Consider making simple value objects `struct`.
- Consider using `record`.

## Entities

### Entity Identifiers

Do not use primitive types or common library types such ad `Guid` as entity IDs. Instead, create a new value type 
or reference type that wraps the common data type. The aim is to strongly type the Ids.

Example of a `Guid`-valued Entity ID:

```csharp
public readonly record struct UserId(Guid Value)
{
    public static implicit operator Guid(UserId userId) => UserId.Value;
    public static implicit operator UserId(Guid value) => new(value);
    public static UserId New() => new(UserId.NewGuid());
    public override string ToString() => Value.ToString();
}
```

If the value type is not an unique identifier (such as `Guid`) that can be created on the fly, then the `New()` method should be left out.

### Current time

Entity methods requiring current time should take it as a parameter as opposed to using System.CurrentTime.
This is for testability
```csharp
    // Good
    public static User Register(Email email, UserName userName, DateTime createdAt)
    {
        // ...
        var user = new User(userId, email, userName, createdAt);
        // ...
    }

    // Avoid
    public static User Register(Email email, UserName userName)
    {
        // ...
        DateTime createdAt = DateTime.UtcNow;
        var user = new User(userId, email, userName, createdAt);
        // ...
    }
```

# Error Handling

- Default: use result pattern
- Validation errors: throw a validation exception
- Methods that may return 0 or 1 values: return a nullable value. Example: `public User? GetById(UserId userId)`

# Data mapping between Entities, DTOs, etc.

- Use manual mapping, e.g. extension methods or static methods. Do not use AutoMapper or similar.

# Naming Conventions

- Interfaces MUST start with `I` prefix.
- Controllers MUST end with `Controller` suffix.
- Exceptions MUST end with `Exception` suffix.
- Command Handler classes MUST end with `CommandHandler` suffix.
- Query Handler classes MUST end with `QueryHandler` suffix.
- Commands MUST end with `Command` suffix.
- Queries MUST end with `Query` suffix.
- Domain Events MUST end with `Event` suffix.
- Domain Services MUST end with `Service` suffix.
- Repository interfaces MUST end with `Repository` suffix.
- Repository implementations MUST end with `Repository` suffix.
- Port interfaces MUST end with `Port` Suffix.
- Adapter implementations (that implement Port interfaces) MUST end with `Adapter` suffix.

# Inheritance Conventions

- Entities should inherit from the [Entity<TId>](./src/AICleanTemplate.SharedKernel/Entity.cs) abstract class.
- Aggregate Roots should inherit from the [AggregateRoot<TId>](./src/AICleanTemplate.SharedKernel/AggregateRoot.cs) abstract class.
- Domain Events MUST implement the [IDomainEvent](./src/AICleanTemplate.SharedKernel/IDomainEvent.cs) interface.
- Commands and Queries should be implemented as immutable `record` types.
- Repository interfaces should be defined in the Domain layer and implemented in the Infrastructure layer.

# Code documentation

- Add XML documentation for classes
- Avoid adding documentation that is redundant given the class/type/method name
- DO NOT add XML documentation to methods or properties. Some exceptions:
  - Do document methods that modify data in Aggregate Roots and Entities
  - Do document methods of Domain Services
- DO NOT add inline (`//`) comments. Exceptions:
  - Do add `// Arrange`, `// Act` and `// Assert` comments to tests.
- NEVER remove useful pre-existing documentation, even when it is against the guidelines above.