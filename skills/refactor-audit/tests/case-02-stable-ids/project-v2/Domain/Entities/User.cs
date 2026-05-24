// REF-001 fix applied: file renamed from UserAccount.cs to User.cs
namespace Mock.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
