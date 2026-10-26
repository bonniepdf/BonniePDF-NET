using System.Linq;
using Xunit;

namespace BonniePDF.Tests.Prototyping;

/// <summary>
/// The seeded RNG is a 32-bit integer LCG that MUST produce an identical
/// sequence in the TypeScript implementation (core/tests/document/placeholders.test.ts
/// asserts the same golden values). If either side drifts — e.g. TypeScript
/// falls back to a float64 multiply that overflows Number.MAX_SAFE_INTEGER —
/// these break.
/// </summary>
public class PlaceholderParityTests
{
    [Fact]
    public void EmployeesSeed42MatchesCanonicalSequence()
    {
        var emps = Placeholders.WithSeed(42).Employees(3);
        var actual = emps.Select(e => (e.Id, e.Name, e.Role, e.Department)).ToArray();

        Assert.Equal(
            new (int, string, string, string)[]
            {
                (1, "Brian Martinez", "Support Lead", "Marketing"),
                (2, "Gina Mitchell", "Project Manager", "Engineering"),
                (3, "Pete Stewart", "IP Counsel", "Marketing"),
            },
            actual);

        Assert.NotNull(emps[0].Avatar);
        Assert.True(emps[0].Avatar.Length > 0);
    }

    [Fact]
    public void InvoiceLinesSeed42MatchesCanonicalSequence()
    {
        var items = Placeholders.WithSeed(42).InvoiceLines(5);
        var actual = items.Select(i => (i.Service, i.Quantity, i.UnitPrice, i.Total)).ToArray();

        Assert.Equal(
            new (string, int, decimal, decimal)[]
            {
                ("Brand Guidelines", 5, 3000m, 15000m),
                ("SEO Audit", 1, 500m, 500m),
                ("Development", 2, 6000m, 12000m),
                ("Website Design", 1, 3500m, 3500m),
                ("Development", 2, 5000m, 10000m),
            },
            actual);
    }

    [Fact]
    public void IntegerSequenceSeed7IsCanonical()
    {
        var p = Placeholders.WithSeed(7);
        Assert.Equal(
            new[] { 16, 33, 38, 71, 40 },
            new[] { p.Integer(0, 99), p.Integer(0, 99), p.Integer(0, 99), p.Integer(0, 99), p.Integer(0, 99) });
    }

    [Fact]
    public void AvatarReturnsValidPngBytes()
    {
        byte[] defaultAvatar = Placeholders.Avatar();
        Assert.NotNull(defaultAvatar);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, defaultAvatar.Take(8));

        byte[] customAvatar = Placeholders.Avatar(32);
        Assert.NotNull(customAvatar);
        Assert.True(customAvatar.Length > 0);
    }
}
