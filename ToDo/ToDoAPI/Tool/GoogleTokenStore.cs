using MongoDB.Driver;
using ToDoAPI.Auth;
using ToDoAPI.Model;

namespace ToDoAPI.Tool;

public class GoogleTokenStore
{
    private readonly IMongoCollection<GoogleCredential> _col;
    private readonly TokenProtector _protector;

    public GoogleTokenStore(IMongoDatabase db, TokenProtector protector)
    {
        _col = db.GetCollection<GoogleCredential>("GoogleCredentials");
        _protector = protector;
    }

    public async Task SaveAsync(string userId, TokenResult tokens)
    {
        var cred = new GoogleCredential
        {
            UserId = userId,
            EncryptedRefreshToken = _protector.Protect(tokens.RefreshToken ?? ""),
            EncryptedAccessToken = tokens.AccessToken != null ? _protector.Protect(tokens.AccessToken) : null,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(int.TryParse(tokens.ExpiresIn, out var s) ? s : 3600),
            Scope = tokens.Scope
        };

        var filter = Builders<GoogleCredential>.Filter.Eq(c => c.UserId, userId);
        await _col.ReplaceOneAsync(filter, cred, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<GoogleCredential?> GetByUserIdAsync(string userId)
    {
        var filter = Builders<GoogleCredential>.Filter.Eq(c => c.UserId, userId);
        var res = await _col.Find(filter).FirstOrDefaultAsync();
        return res;
    }

    public string UnprotectRefreshToken(GoogleCredential cred)
    {
        return _protector.Unprotect(cred.EncryptedRefreshToken);
    }

    public string? UnprotectAccessToken(GoogleCredential cred)
    {
        return cred.EncryptedAccessToken == null ? null : _protector.Unprotect(cred.EncryptedAccessToken);
    }
}