using Bogus;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test.Extensions;

public static class FakerExtensions
{
    public static Faker<T> CreateFaker<T>()
        where T : class
    {
        return new Faker<T>()
            .UseSeed(1337)
            .RuleForType(typeof(string), f => f.Lorem.Word())
            .RuleForType(typeof(int), f => f.Random.Int(1, 100))
            .RuleForType(typeof(Guid), f => f.Random.Guid())
            .RuleForType(typeof(DateTime), f => f.Date.Recent().ToUniversalTime())
            .RuleForType(typeof(bool), f => f.Random.Bool())
            .RuleForType(typeof(decimal), f => f.Random.Decimal(0, 1000))
            .RuleForType(typeof(double), f => f.Random.Double(0, 1000))
            .RuleForType(typeof(float), f => f.Random.Float(0, 1000))
            .RuleForType(typeof(long), f => f.Random.Long(1, 1000000))
            .RuleForType(typeof(Uri), f => new Uri(f.Internet.Url()));
    }

    public static Dictionary<string, string> CreateDictionary(this Faker faker, int count = 5)
    {
        return faker
            .Make(count, i => new KeyValuePair<string, string>(i.ToString(), faker.Lorem.Word()))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Stream CreateStream(this Faker faker, int size = 1024)
    {
        var bytes = faker.Random.Bytes(size);
        return new MemoryStream(bytes);
    }
}
