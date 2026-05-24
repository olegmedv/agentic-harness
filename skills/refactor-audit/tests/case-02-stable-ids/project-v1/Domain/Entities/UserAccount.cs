// VIOLATION: file name "UserAccount.cs" but public type is "User"
namespace Mock.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
