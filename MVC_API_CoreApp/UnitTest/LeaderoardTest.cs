using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ProjectInfrastructure.Context;
using ProjectInfrastructure.Models;
using ProjectMVC.Controllers;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
    
namespace TestProject1
{
    public class LeaderboardsControllerTests
    {
        private LeaderboardDbContext _context;
        private Mock<UserManager<User>> _userManagerMock;
        private LeaderboardsController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LeaderboardDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _context = new LeaderboardDbContext(options);

            var userStore = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(userStore.Object, null, null, null, null, null, null, null, null);

            _controller = new LeaderboardsController(_context, _userManagerMock.Object);
        }

        [Test]
        public async Task Get_ReturnsAllLeaderboards()
        {
            // Arrange
            _context.Leaderboards.Add(new LeaderboardModel { Id = "1", Name = "L1", UserId = "u1" });
            _context.Leaderboards.Add(new LeaderboardModel { Id = "2", Name = "L2", UserId = "u2" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Get(null) as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var list = result.Value as List<LeaderboardModel>;
            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Upload_AddsLeaderboard()
        {
            // Arrange
            var user = new User { Id = "user123", UserName = "TestUser", LeaderboaradIds = new List<string>() };
            _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                        new Claim(ClaimTypes.NameIdentifier, "user123")
                    }))
                }
            };

            var leaderboard = new LeaderboardModel { Name = "TestBoard" };

            // Act
            var result = await _controller.Upload(leaderboard) as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var created = result.Value as LeaderboardModel;
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Name, Is.EqualTo("TestBoard"));
            Assert.That(created.UserId, Is.EqualTo("user123"));
        }

        [Test]
        public async Task UpdateLeaderboard_UpdatesExisting()
        {
            // Arrange
            var leaderboard = new LeaderboardModel { Id = "lb1", Name = "Before", UserId = "user" };
            _context.Leaderboards.Add(leaderboard);
            await _context.SaveChangesAsync();

            var updatedModel = new LeaderboardModel { Id = "lb1", Name = "After", UserId = "user" };

            // Act
            var result = await _controller.UpdateLeaderboard(updatedModel) as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var updated = result.Value as LeaderboardModel;
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated.Name, Is.EqualTo("After"));
        }

        [Test]
        public async Task GetLeaderboard_ReturnsCorrectLeaderboard()
        {
            // Arrange
            var lb = new LeaderboardModel { Id = "id123", Name = "LB", UserId = "user" };
            _context.Leaderboards.Add(lb);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLeaderboard("id123") as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var returned = result.Value as LeaderboardModel;
            Assert.That(returned, Is.Not.Null);
            Assert.That(returned.Name, Is.EqualTo("LB"));
        }

        [Test]
        public async Task Delete_RemovesLeaderboard()
        {
            // Arrange
            var lb = new LeaderboardModel { Id = "idDel", Name = "ToDelete", UserId = "user" };
            _context.Leaderboards.Add(lb);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete("idDel") as OkResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var deleted = await _context.Leaderboards.FindAsync("idDel");
            Assert.That(deleted, Is.Null);
        }
        
                [Test]
        public async Task Upload_AssignsGeneratedName_WhenNameIsNull()
        {
            var user = new User { Id = "user1", LeaderboaradIds = new List<string>() };
            _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var leaderboard = new LeaderboardModel { Name = null };

            var result = await _controller.Upload(leaderboard) as OkObjectResult;
            var returned = result?.Value as LeaderboardModel;

            Assert.That(returned, Is.Not.Null);
            Assert.That(returned!.Name, Does.StartWith("New leaderboard "));
        }

        [Test]
        public async Task Upload_ReturnsBadRequest_OnException()
        {
            _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                            .ThrowsAsync(new Exception("Simulated"));

            var result = await _controller.Upload(new LeaderboardModel()) as BadRequestObjectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value!.ToString(), Does.Contain("Simulated"));
        }

        [Test]
        public async Task GetLeaderboard_ReturnsBadRequest_WhenNotFound()
        {
            var result = await _controller.GetLeaderboard("invalid-id") as BadRequestObjectResult;

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task Delete_ReturnsBadRequest_WhenNotFound()
        {
            var result = await _controller.Delete("nonexistent") as BadRequestObjectResult;

            Assert.That(result, Is.Null);
        }
        
        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

    }
}
