namespace Api.Validation;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireIdempotencyKeyAttribute : Attribute
{
    
}