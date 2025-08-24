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
    public static UserId UserId() => new(UserId.NewGuid());
    public override string ToString() => Value.ToString();
}
```

If the value type is not `Guid`, then the `NewId()` method should be left out.

# Error Handling

- Default: use result pattern
- Validation errors: throw a validation exception
- Methods that may return 0 or 1 values: return a nullable value. Example: `public User? GetById(UserId userId)`

# Data mapping between Entities, DTOs, etc.

- Use manual mapping, e.g. extension methods or static methods. Do not use AutoMapper or similar.

# Naming Conventions

- Interfaces should start with `I` prefix.
- Controllers should end with `Controller` suffix.
- Exceptions should end with `Exception` suffix.
- Command Handler classes should end with `CommandHandler` suffix.
- Query Handler classes should end with `QueryHandler` suffix.
- Commands should end with `Command` suffix.
- Queries should end with `Query` suffix.
- Domain Events should end with `DomainEvent` suffix.
- Domain Services should end with `DomainService` suffix.
- Repository interfaces should end with `Repository` suffix.
- Repository implementations should end with `Repository` suffix.
- Port interfaces should end with `Port` Suffix.
- Adapter implementations (that implement Port itnerfaces) should end with `Adapter` suffix.

# Inheritance Conventions

- Entities should inherit from the [Entity<TId>](./src/AICleanTemplate.SharedKernel/Entity.cs) abstract class.
- Aggregate Roots should inherit from the [AggregateRoot<TId>](./src/AICleanTemplate.SharedKernel/AggregateRoot.cs) abstract class.
- Domain Events should implement the [IDomainEvent](./src/AICleanTemplate.SharedKernel/IDomainEvent.cs) interface.
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