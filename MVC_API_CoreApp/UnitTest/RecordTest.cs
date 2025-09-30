using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ProjectMVC.Controllers;
using ProjectInfrastructure.Context;
using ProjectInfrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Tests
{
    public class RecordControllerTests
    {
        private LeaderboardDbContext _context;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LeaderboardDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new LeaderboardDbContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task Index_Returns_View_When_LeaderboardExists()
        {
            var leaderboard = new LeaderboardModel { Id = "lb1" };
            _context.Leaderboards.Add(leaderboard);
            await _context.SaveChangesAsync();

            var controller = new RecordController(_context);

            var result = await controller.Index("lb1");

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task GetRecords_Returns_Ok_When_LeaderboardExists()
        {
            var leaderboard = new LeaderboardModel { Id = "lb1" };
            _context.Leaderboards.Add(leaderboard);
            await _context.SaveChangesAsync();

            var controller = new RecordController(_context);

            var result = await controller.GetRecords("lb1");

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task AddRecord_Returns_Ok_And_Adds_Record()
        {
            var leaderboard = new LeaderboardModel { Id = "lb2", Records = new List<LeaderboardRecordModel>() };
            _context.Leaderboards.Add(leaderboard);
            await _context.SaveChangesAsync();

            var controller = new RecordController(_context);
            var record = new LeaderboardRecordModel {Name = "name" ,Value = 100 };

            var result = await controller.AddRecord(record, "lb2");

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.Not.Null);
        }

        [Test]
        public async Task UpdateRecord_Changes_Value_When_RecordExists()
        {
            var record = new LeaderboardRecordModel { Name = "name", Place = -1, Id = "rec1", LeaderboardId = "lb3", Value = 10 };
            _context.LeaderboardsRecords.Add(record);
            await _context.SaveChangesAsync();

            var controller = new RecordController(_context);
            var update = new LeaderboardRecordModel { Id = "rec1", Value = 200 };

            var result = await controller.UpdateRecord(update);

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var updated = (result as OkObjectResult)?.Value as LeaderboardRecordModel;
            Assert.That(updated?.Value, Is.EqualTo(200));
        }
    }
}
