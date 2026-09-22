using System.Text.Json.Serialization;

namespace Api.Domain;

public sealed class Stokvel
{
    // Kept private as members can only be changed through AddMember/RemoveMember
    private readonly List<User> _members = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public decimal MonthlyContribution { get; private set; }

    public int CurrentPeriod { get; private set; }

    public int TotalPeriods { get; private set; }

    public string? Rules { get; private set; }

    //Read-only to prevent outside code to add/remove members
    public IReadOnlyCollection<User> Members => _members.AsReadOnly();

    [JsonConstructor]
    public Stokvel(
        string name,
        decimal monthlyContribution,
        int currentPeriod,
        int totalPeriods,
        string? rules = null)
    {
        Id = Guid.NewGuid();

        Name = ValidateName(name);
        MonthlyContribution =
            ValidateContribution(monthlyContribution);

        ValidatePeriods(currentPeriod, totalPeriods);

        CurrentPeriod = currentPeriod;
        TotalPeriods = totalPeriods;
        Rules = NormalizeRules(rules);
    }

    //Updates are controlled by stokvel entity instead making controllers change properties
    public void UpdateDetails(
        string name,
        decimal monthlyContribution,
        int currentPeriod,
        int totalPeriods,
        string? rules)
    {
        Name = ValidateName(name);

        MonthlyContribution =
            ValidateContribution(monthlyContribution);

        ValidatePeriods(currentPeriod, totalPeriods);

        CurrentPeriod = currentPeriod;
        TotalPeriods = totalPeriods;
        Rules = NormalizeRules(rules);
    }

    public void AddMember(User user)
    {
        if (_members.Any(member => member.Id == user.Id))
        {
            throw new InvalidOperationException(
                "The user is already a member of this stokvel.");
        }

        _members.Add(user);
    }

    public bool RemoveMember(Guid userId)
    {
        var member =
            _members.FirstOrDefault(user => user.Id == userId);

        if (member is null)
        {
            return false;
        }

        _members.Remove(member);

        return true;
    }

    public bool HasMember(Guid userId)
    {
        return _members.Any(user => user.Id == userId);
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A stokvel must have a name.");
        }

        return name.Trim();
    }

    private static decimal ValidateContribution(
        decimal monthlyContribution)
    {
        if (monthlyContribution <= 0)
        {
            throw new ArgumentException("The monthly contribution must be greater than zero.");
        }

        return monthlyContribution;
    }

    private static void ValidatePeriods(
        int currentPeriod,
        int totalPeriods)
    {
        if (totalPeriods < 1)
        {
            throw new ArgumentException("A stokvel must have at least one period.");
        }

        if (currentPeriod < 1 ||
            currentPeriod > totalPeriods)
        {
            throw new ArgumentException("The current period must be between 1 and the total number of periods.");
        }
    }

    private static string? NormalizeRules(string? rules)
    {
        return string.IsNullOrWhiteSpace(rules)? null : rules.Trim();
    }
}