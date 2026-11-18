namespace BonniePDF.Examples.Security.Encryption.Data.Models;

public class User
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Department { get; set; }
    public required string Role { get; set; }
    public required string Location { get; set; }
    public required string Status { get; set; }
}