using Microsoft.AspNetCore.DataProtection;

namespace EnterprisePKI.Portal.Security;

public interface IDataProtectorFacade
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}

public sealed class DataProtectorFacade : IDataProtectorFacade
{
    private readonly IDataProtector _protector;

    public DataProtectorFacade(IDataProtector protector)
    {
        _protector = protector;
    }

    public string Protect(string plaintext)
    {
        return _protector.Protect(plaintext);
    }

    public string Unprotect(string ciphertext)
    {
        return _protector.Unprotect(ciphertext);
    }
}