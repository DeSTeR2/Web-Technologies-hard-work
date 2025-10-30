using MongoDB.Driver;
using ToDoAPI.Model;

namespace ToDoAPI.Tool;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(IMongoDatabase db)
    {
        _users = db.GetCollection<User>("Users");
    }

    public async Task<User> EnsureUserByEmailAsync(string email, string? displayName = null)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        var user = await _users.Find(filter).FirstOrDefaultAsync();
        if (user != null) return user;

        var newUser = new User { Email = email, DisplayName = displayName };
        await _users.InsertOneAsync(newUser);
        return newUser;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }
}