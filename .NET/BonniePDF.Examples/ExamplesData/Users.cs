using System.Collections.Generic;

namespace BonniePDF.Examples.ExamplesData;

public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string Role { get; set; }
    public string Location { get; set; }
    public string Status { get; set; }
}

public static class Users
{
    public static List<User> GetMockUsers()
    {
        return new List<User>
        {
            new()
            {
                Id = "001", Name = "Alice Johnson", Email = "alice.johnson@company.com", Department = "Engineering",
                Role = "Senior Developer", Location = "San Francisco, CA", Status = "Active"
            },
            new()
            {
                Id = "002", Name = "Bob Smith", Email = "bob.smith@company.com", Department = "Marketing",
                Role = "Marketing Manager", Location = "New York, NY", Status = "Active"
            },
            new()
            {
                Id = "003", Name = "Carol Williams", Email = "carol.williams@company.com",
                Department = "Engineering", Role = "Tech Lead", Location = "Austin, TX", Status = "Active"
            },
            new()
            {
                Id = "004", Name = "David Brown", Email = "david.brown@company.com", Department = "Sales",
                Role = "Sales Representative", Location = "Chicago, IL", Status = "Active"
            },
            new()
            {
                Id = "005", Name = "Emma Davis", Email = "emma.davis@company.com", Department = "Human Resources",
                Role = "HR Specialist", Location = "Boston, MA", Status = "Active"
            },
            new()
            {
                Id = "006", Name = "Frank Miller", Email = "frank.miller@company.com", Department = "Finance",
                Role = "Financial Analyst", Location = "Miami, FL", Status = "Active"
            },
            new()
            {
                Id = "007", Name = "Grace Lee", Email = "grace.lee@company.com", Department = "Engineering",
                Role = "Junior Developer", Location = "Seattle, WA", Status = "Active"
            },
            new()
            {
                Id = "008", Name = "Henry Wilson", Email = "henry.wilson@company.com", Department = "Operations",
                Role = "Operations Manager", Location = "Denver, CO", Status = "Active"
            },
            new()
            {
                Id = "009", Name = "Ivy Chen", Email = "ivy.chen@company.com", Department = "Design",
                Role = "UX Designer", Location = "Portland, OR", Status = "Active"
            },
            new()
            {
                Id = "010", Name = "Jack Taylor", Email = "jack.taylor@company.com", Department = "Engineering",
                Role = "DevOps Engineer", Location = "Phoenix, AZ", Status = "Active"
            },
            new()
            {
                Id = "011", Name = "Kate Anderson", Email = "kate.anderson@company.com", Department = "Marketing",
                Role = "Content Strategist", Location = "Los Angeles, CA", Status = "Active"
            },
            new()
            {
                Id = "012", Name = "Liam Martinez", Email = "liam.martinez@company.com", Department = "Sales",
                Role = "Account Executive", Location = "Dallas, TX", Status = "Active"
            },
            new()
            {
                Id = "013", Name = "Mia Garcia", Email = "mia.garcia@company.com", Department = "Engineering",
                Role = "QA Engineer", Location = "San Jose, CA", Status = "Active"
            },
            new()
            {
                Id = "014", Name = "Noah Rodriguez", Email = "noah.rodriguez@company.com",
                Department = "Customer Support", Role = "Support Lead", Location = "Atlanta, GA", Status = "Active"
            },
            new()
            {
                Id = "015", Name = "Olivia Thompson", Email = "olivia.thompson@company.com", Department = "Product",
                Role = "Product Manager", Location = "San Francisco, CA", Status = "Active"
            },
            new()
            {
                Id = "016", Name = "Peter White", Email = "peter.white@company.com", Department = "Engineering",
                Role = "Backend Developer", Location = "Remote", Status = "Active"
            },
            new()
            {
                Id = "017", Name = "Quinn Harris", Email = "quinn.harris@company.com", Department = "Design",
                Role = "Visual Designer", Location = "New York, NY", Status = "Active"
            },
            new()
            {
                Id = "018", Name = "Rachel Clark", Email = "rachel.clark@company.com", Department = "Finance",
                Role = "Accountant", Location = "Chicago, IL", Status = "Active"
            },
            new()
            {
                Id = "019", Name = "Sam Lewis", Email = "sam.lewis@company.com", Department = "Engineering",
                Role = "Frontend Developer", Location = "Austin, TX", Status = "Active"
            },
            new()
            {
                Id = "020", Name = "Tina Robinson", Email = "tina.robinson@company.com",
                Department = "Human Resources", Role = "Recruiter", Location = "Boston, MA", Status = "Active"
            },
            new()
            {
                Id = "021", Name = "Ursula Walker", Email = "ursula.walker@company.com", Department = "Legal",
                Role = "Legal Counsel", Location = "Washington, DC", Status = "Active"
            },
            new()
            {
                Id = "022", Name = "Victor Hall", Email = "victor.hall@company.com", Department = "Engineering",
                Role = "System Architect", Location = "Seattle, WA", Status = "Active"
            },
            new()
            {
                Id = "023", Name = "Wendy Allen", Email = "wendy.allen@company.com", Department = "Marketing",
                Role = "Brand Manager", Location = "Los Angeles, CA", Status = "Active"
            },
            new()
            {
                Id = "024", Name = "Xavier Young", Email = "xavier.young@company.com", Department = "Operations",
                Role = "Logistics Coordinator", Location = "Miami, FL", Status = "Active"
            },
            new()
            {
                Id = "025", Name = "Yara King", Email = "yara.king@company.com", Department = "Engineering",
                Role = "Data Scientist", Location = "San Francisco, CA", Status = "Active"
            },
            new()
            {
                Id = "026", Name = "Zach Wright", Email = "zach.wright@company.com", Department = "Sales",
                Role = "Sales Director", Location = "New York, NY", Status = "Active"
            },
            new()
            {
                Id = "027", Name = "Amy Scott", Email = "amy.scott@company.com", Department = "Customer Support",
                Role = "Customer Success", Location = "Atlanta, GA", Status = "Active"
            },
            new()
            {
                Id = "028", Name = "Brian Torres", Email = "brian.torres@company.com", Department = "Engineering",
                Role = "Mobile Developer", Location = "Austin, TX", Status = "Active"
            },
            new()
            {
                Id = "029", Name = "Cindy Nguyen", Email = "cindy.nguyen@company.com", Department = "Design",
                Role = "Product Designer", Location = "Portland, OR", Status = "Active"
            },
            new()
            {
                Id = "030", Name = "Dan Hill", Email = "dan.hill@company.com", Department = "Finance",
                Role = "Controller", Location = "Chicago, IL", Status = "Active"
            },
            new()
            {
                Id = "031", Name = "Ella Flores", Email = "ella.flores@company.com", Department = "Marketing",
                Role = "SEO Specialist", Location = "Los Angeles, CA", Status = "Active"
            },
            new()
            {
                Id = "032", Name = "Fred Barnes", Email = "fred.barnes@company.com", Department = "Engineering",
                Role = "Security Engineer", Location = "Seattle, WA", Status = "Active"
            },
            new()
            {
                Id = "033", Name = "Gina Mendez", Email = "gina.mendez@company.com", Department = "Human Resources",
                Role = "HR Manager", Location = "Boston, MA", Status = "Active"
            },
            new()
            {
                Id = "034", Name = "Harry Rivera", Email = "harry.rivera@company.com", Department = "Operations",
                Role = "Supply Chain Mgr", Location = "Miami, FL", Status = "Active"
            },
            new()
            {
                Id = "035", Name = "Iris Cooper", Email = "iris.cooper@company.com", Department = "Product",
                Role = "Product Owner", Location = "San Francisco, CA", Status = "Active"
            },
            new()
            {
                Id = "036", Name = "John Richardson", Email = "john.richardson@company.com",
                Department = "Engineering", Role = "Tech Lead", Location = "Austin, TX", Status = "Active"
            },
            new()
            {
                Id = "037", Name = "Karen Cox", Email = "karen.cox@company.com", Department = "Sales",
                Role = "Enterprise AE", Location = "New York, NY", Status = "Active"
            },
            new()
            {
                Id = "038", Name = "Leo Howard", Email = "leo.howard@company.com", Department = "Design",
                Role = "Design Lead", Location = "Portland, OR", Status = "Active"
            },
            new()
            {
                Id = "039", Name = "Monica Ward", Email = "monica.ward@company.com",
                Department = "Customer Support", Role = "Support Manager", Location = "Atlanta, GA",
                Status = "Active"
            },
            new()
            {
                Id = "040", Name = "Nathan Peterson", Email = "nathan.peterson@company.com",
                Department = "Engineering", Role = "ML Engineer", Location = "San Francisco, CA", Status = "Active"
            },
            new()
            {
                Id = "041", Name = "Olivia Gray", Email = "olivia.gray@company.com", Department = "Marketing",
                Role = "Social Media Mgr", Location = "Los Angeles, CA", Status = "Active"
            },
            new()
            {
                Id = "042", Name = "Paul Ramirez", Email = "paul.ramirez@company.com", Department = "Finance",
                Role = "Tax Specialist", Location = "Chicago, IL", Status = "Active"
            },
            new()
            {
                Id = "043", Name = "Rachel James", Email = "rachel.james@company.com", Department = "Legal",
                Role = "Compliance Officer", Location = "Washington, DC", Status = "Active"
            },
            new()
            {
                Id = "044", Name = "Steve Watson", Email = "steve.watson@company.com", Department = "Operations",
                Role = "Facilities Mgr", Location = "Miami, FL", Status = "Active"
            },
            new()
            {
                Id = "045", Name = "Teresa Brooks", Email = "teresa.brooks@company.com", Department = "Engineering",
                Role = "Cloud Architect", Location = "Seattle, WA", Status = "Active"
            },
            new()
            {
                Id = "046", Name = "Victor Kelly", Email = "victor.kelly@company.com", Department = "Sales",
                Role = "SMB Account Exec", Location = "Dallas, TX", Status = "Active"
            },
            new()
            {
                Id = "047", Name = "Wanda Sanders", Email = "wanda.sanders@company.com",
                Department = "Human Resources", Role = "Training Coordinator", Location = "Boston, MA",
                Status = "Active"
            },
            new()
            {
                Id = "048", Name = "Yvonne Price", Email = "yvonne.price@company.com", Department = "Product",
                Role = "Growth Product Mgr", Location = "San Francisco, CA", Status = "Active"
            },
            new()
            {
                Id = "049", Name = "Zachary Bennett", Email = "zachary.bennett@company.com",
                Department = "Engineering", Role = "Platform Engineer", Location = "Austin, TX", Status = "Active"
            },
            new()
            {
                Id = "050", Name = "Angela Collins", Email = "angela.collins@company.com", Department = "Executive",
                Role = "VP of Engineering", Location = "San Francisco, CA", Status = "Active"
            }
        };
    }
}