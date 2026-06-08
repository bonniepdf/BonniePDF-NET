using BonniePDF.Examples.Templates.UserDirectory.Data.Models;
using BonniePDF.Prototyping;

namespace BonniePDF.Examples.Templates.UserDirectory.Data;

public static class UsersData
{
    public static IList<User> GetMockUsers()
    {
        string[] statuses = ["Active", "Active", "Active", "Inactive", "Pending"];
        var source = Placeholders.WithSeed(100);
        var users = new List<User>(50);

        for (int i = 0; i < 50; i++)
        {
            string first = source.FirstName();
            string last = source.LastName();
            users.Add(new User
            {
                Id = $"USR-{(i + 1):D3}",
                Name = $"{first} {last}",
                Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@company.com",
                Department = source.Department(),
                Role = source.Role(),
                Location = source.Location(),
                Status = statuses[i % statuses.Length]
            });
        }

        return users;
    }
}
