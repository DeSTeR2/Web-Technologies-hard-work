using MongoDB.Bson.Serialization.Attributes;

namespace ToDoAPI.Model;

public class GoogleCredential
{
    [BsonId] // UserId is now the unique _id
    public string UserId { get; set; } = null!;

    [BsonElement("accessToken")] public string? EncryptedAccessToken { get; set; }

    [BsonElement("refreshToken")] public string EncryptedRefreshToken { get; set; } = null!;

    [BsonElement("expiresAt")] public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("scope")] public string? Scope { get; set; }

    [BsonElement("createdAt")] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}