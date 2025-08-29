using Users.Contracts;

namespace SystemTests.TestObjectBuilders;

/// <summary>
/// Test Object Builder for creating AddUserCommand instances in tests.
/// </summary>
public class AddUserCommandBuilder
{
    private string _email = "test@example.com";
    private string _userName = "testuser";

    public AddUserCommandBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public AddUserCommandBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }

    public AddUserCommand Build()
    {
        return new AddUserCommand(_email, _userName);
    }
}
