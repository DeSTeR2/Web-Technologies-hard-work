using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectInfrastructure.Models;

namespace ProjectInfrastructure.Context;

public class UserApplicationDbContext : IdentityDbContext<User>
{
    public UserApplicationDbContext(DbContextOptions<UserApplicationDbContext> options) : base(options) {}
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
    
}