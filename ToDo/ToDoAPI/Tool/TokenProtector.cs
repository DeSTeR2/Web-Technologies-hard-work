using Microsoft.AspNetCore.DataProtection;

namespace ToDoAPI.Tool;

public class TokenProtector
{
    private readonly IDataProtector _protector;
    public TokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("GoogleTokenProtector.v1");
    }

    public string Protect(string plain) => _protector.Protect(plain);
    public string Unprotect(string cipher) => _protector.Unprotect(cipher);
}