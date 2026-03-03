
namespace TestShared;

public enum BloodType
{
    ABPositive,
    ABNegative,

    APositive,
    ANegative,

    BPositive,
    BNegative,

    OPositive,
    ONegative
}

public static class BloodTypeExtensions
{
    public static BloodType ParseFromString(string? s) => s switch
    {
        "AB+" => BloodType.ABPositive,
        "AB-" => BloodType.ABNegative,
        "A+" => BloodType.APositive,
        "A-" => BloodType.ANegative,
        "B+" => BloodType.BPositive,
        "B-" => BloodType.BNegative,
        "O+" => BloodType.OPositive,
        "O-" => BloodType.ONegative,
        _ => (BloodType)0
    };

    public static string? ConvertToString(BloodType bloodType) => bloodType switch
    {
        BloodType.ABPositive => "AB+",
        BloodType.ABNegative => "AB-",
        BloodType.APositive => "A+",
        BloodType.ANegative => "A-",
        BloodType.BPositive => "B+",
        BloodType.BNegative => "B-",
        BloodType.OPositive => "O+",
        BloodType.ONegative => "O-",
        _ => null
    };
}

public sealed class Person
{
#nullable disable warnings

    public string Name { get; set; }
    public int Age { get; set; }
    public BloodType BloodType { get; set; }

#nullable restore warnings

    public Person(string name, int age, BloodType bloodType)
    {
        Name = name;
        Age = age;
        BloodType = bloodType;
    }

    public override bool Equals(object? obj)
    {
        return obj is Person person &&
               Name == person.Name &&
               Age == person.Age &&
               BloodType == person.BloodType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Age, BloodType);
    }
}