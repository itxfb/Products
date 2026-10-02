using System.ComponentModel.DataAnnotations;

namespace Products.Api.UnitTests;

internal static class Validation
{
    public static IEnumerable<string> Errors(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames);
    }
}
