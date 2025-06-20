namespace MyShop.Core;

public abstract record Result;

public record Failure(Error Error) : Result;
public record Failure<T>(T Value, Error Error, IEnumerable<string> Errors) : Failure(Error);

public record Success : Result;
public record Success<T>(T Value) : Success;

public enum Error
{
    Unknown,
    NotFound,
    Validation
}