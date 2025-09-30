using Microsoft.EntityFrameworkCore;
using ProjectInfrastructure.Context;
using ProjectInfrastructure.Models;

namespace ProjectMVC.Utils.Extensions;

public static class Extension
{
    public static void ApplyMigration<TContext>(this IApplicationBuilder applicationBuilder) where TContext : DbContext
    {
        using IServiceScope serviceScope = applicationBuilder.ApplicationServices.CreateScope();
        using TContext context =
            serviceScope.ServiceProvider.GetRequiredService<TContext>();
    }

    public static async Task<LeaderboardModel?> FindLeaderboardAsync(this LeaderboardDbContext leaderboardDbContext, string id)
    {
        return await leaderboardDbContext.Leaderboards
            .Include(l => l.Records)
            .FirstOrDefaultAsync(l => l.Id == id);
    }
}