// VIOLATION: multiple public types in one file
namespace Mock.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
