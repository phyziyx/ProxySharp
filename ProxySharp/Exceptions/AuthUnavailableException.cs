namespace ProxySharp.Exceptions;

public class AuthUnavailableException : Exception
{
    public AuthUnavailableException()
        : base("Authentication service is temporarily unavailable.")
    {
    }

    public AuthUnavailableException(string message)
        : base(message)
    {
    }

    public AuthUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
