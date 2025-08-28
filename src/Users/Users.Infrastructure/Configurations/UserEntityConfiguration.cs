using Base.Domain.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Domain;

namespace Users.Infrastructure.Configurations;

/// <summary>
/// EF Core entity configuration for the User aggregate root, handling value object conversions and database mapping.
/// </summary>
internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    // Safe conversion methods that handle potential database corruption
    private static UserId SafeConvertToUserId(Guid guid)
    {
        Result<UserId> result = UserId.FromGuid(guid);
        if (result.IsFailure)
            throw new InvalidOperationException($"Invalid UserId found in database: {guid}. Error: {result.Error.Description}");
        return result.Value;
    }

    private static Email SafeConvertToEmail(string emailString)
    {
        Result<Email> result = Email.Create(emailString);
        if (result.IsFailure)
            throw new InvalidOperationException($"Invalid Email found in database: {emailString}. Error: {result.Error.Description}");
        return result.Value;
    }

    private static UserName SafeConvertToUserName(string userNameString)
    {
        Result<UserName> result = UserName.Create(userNameString);
        if (result.IsFailure)
            throw new InvalidOperationException($"Invalid UserName found in database: {userNameString}. Error: {result.Error.Description}");
        return result.Value;
    }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "Users");

        // Primary Key - UserId value object
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                guid => SafeConvertToUserId(guid))
            .HasColumnName("Id");

        // Email value object - store as string
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value.Address,
                emailString => SafeConvertToEmail(emailString))
            .HasColumnName("Email")
            .HasMaxLength(320) // RFC 5321 maximum email length
            .IsRequired();

        // Add unique index on Email
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Email");

        // UserName value object - store as string  
        builder.Property(u => u.UserName)
            .HasConversion(
                userName => userName.Value,
                userNameString => SafeConvertToUserName(userNameString))
            .HasColumnName("UserName")
            .HasMaxLength(50)
            .IsRequired();

        // Add unique index on UserName
        builder.HasIndex(u => u.UserName)
            .IsUnique()
            .HasDatabaseName("UQ_Users_UserName");

        // Timestamps with precise datetime2(7) as per design plan
        builder.Property(u => u.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("LastLoginAt")
            .HasColumnType("datetime2(7)");

        // Optimistic concurrency control with RowVersion as per design plan
        builder.Property<byte[]>("Version")
            .IsRowVersion()
            .HasColumnName("Version");

        // Performance indexes as per design plan
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("IX_Users_LastLoginAt")
            .HasFilter("[LastLoginAt] IS NOT NULL");

        // Domain events are handled by the AggregateRoot base class and not persisted
    }
}
