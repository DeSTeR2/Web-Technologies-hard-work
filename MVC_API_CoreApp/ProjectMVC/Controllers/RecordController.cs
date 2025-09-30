using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectInfrastructure.Context;
using ProjectInfrastructure.Models;
using ProjectMVC.Models;
using ProjectMVC.Models.Requests;
using ProjectMVC.Utils.Errors;
using ProjectMVC.Utils.Extensions;
using ProjectMVC.Utils.Sorting;

namespace ProjectMVC.Controllers;

[Route("records")]
[ApiController]
public class RecordController : Controller
{
    private readonly LeaderboardDbContext _leaderboardDbContext;

    public RecordController(LeaderboardDbContext leaderboardDbContext)
    {
        _leaderboardDbContext = leaderboardDbContext;
    }

    [HttpGet("index")]
    public async Task<IActionResult> Index(string leaderboardId)
    {
        ViewData["LeaderboardId"] = leaderboardId;
        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);

        if (leaderboard == null)
        {
            return BadRequest(new LeaderboardError().Error(leaderboardId));
        }

        var records = UpdatePositions(leaderboardId).Result;
        return View(records);
    }


    private async Task<List<LeaderboardRecordModel>> UpdatePositions(string leaderboardId)
    {
        return await GetSortedRecordsAsync(new UpdatePositionsRequest()
        {
            LeaderboardId = leaderboardId,
            SortBy = SortingParameter.Value,
            Direction = SortingType.Descending,
            Take = 10
        });
    }

    [HttpPatch]
    public async Task<IActionResult> UpdatePositions([FromBody] UpdatePositionsRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.LeaderboardId))
        {
            return BadRequest(new { errors = new { leaderboardId = new[] { "The leaderboardId field is required." } } });
        }

        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(request.LeaderboardId);

        if (leaderboard == null)
        {
            return BadRequest(new LeaderboardError().Error(request.LeaderboardId));
        }

        try
        {
            var sortedList = await GetSortedRecordsAsync(request);

            for (int i = 0; i < sortedList.Count; i++)
            {
                sortedList[i].Place = i + 1;
            }

            return Ok(sortedList);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }



    [HttpPut]
    public async Task<IActionResult> UpdateRecord([FromBody] LeaderboardRecordModel record)
    {
        LeaderboardRecordModel updatedRecord = await FindRecordAsync(record.Id);
        if (updatedRecord is null)
        {
            return BadRequest(new RecordError().Error(record.Id));
        }

        updatedRecord.Update(record);

        _leaderboardDbContext.LeaderboardsRecords.Update(updatedRecord);
        await _leaderboardDbContext.SaveChangesAsync();

        return Ok(updatedRecord);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords(string leaderboardId)
    {
        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);

        if (leaderboard == null)
        {
            return BadRequest(new LeaderboardError().Error(leaderboardId));
        }

        var records = UpdatePositions(leaderboardId).Result;
        return Ok(records);
    }

    [HttpPost("{leaderboardId}")]
    public async Task<IActionResult> AddRecord([FromBody] LeaderboardRecordModel record, [FromRoute(Name = "leaderboardId")] string leaderboardId)
    {
        record.LeaderboardId = leaderboardId;
        record.Place = -1;
        record.Id = Guid.NewGuid().ToString();
    
        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);
        if (leaderboard is null)
        {
            return BadRequest(new LeaderboardError().Error(leaderboardId));
        }

        leaderboard.AddRecord(record);
    
        _leaderboardDbContext.LeaderboardsRecords.Add(record);
        _leaderboardDbContext.Leaderboards.Update(leaderboard);
        await _leaderboardDbContext.SaveChangesAsync();
        var records = UpdatePositions(leaderboardId).Result;

        return Ok(records);
    }


    [HttpDelete("{recordId}")]
    public async Task<IActionResult> DeleteRecord(string recordId)
    {
        var record = await _leaderboardDbContext.LeaderboardsRecords.FirstOrDefaultAsync(r => r.Id == recordId);
        if (record is null)
        {
            return BadRequest(new RecordError().Error(recordId));
        }
        
        string leaderboardId = record.LeaderboardId;

        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);
        leaderboard?.RemoveRecord(record);

        _leaderboardDbContext.Update<LeaderboardModel>(leaderboard);
        await _leaderboardDbContext.SaveChangesAsync();

        return Ok(leaderboard);
    }

    private async Task<List<LeaderboardRecordModel>> GetSortedRecordsAsync(UpdatePositionsRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.LeaderboardId))
            throw new ArgumentException("LeaderboardId is required");

        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(request.LeaderboardId);

        if (leaderboard == null)
            throw new Exception("Leaderboard not found");

        IEnumerable<LeaderboardRecordModel> records = leaderboard.Records;

        SortingStrategy sortingStrategy = new SortingFactory().GetStrategy(request.SortBy, request.Direction);

        var sortedList = records.ToList();
        sortingStrategy.Sort(sortedList);

        if (request.Take.HasValue)
            sortedList = sortedList.Take(request.Take.Value).ToList();

        for (int i = 0; i < sortedList.Count; i++)
            sortedList[i].Place = i + 1;

        return sortedList;
    }

    
    private async Task<LeaderboardRecordModel?> FindRecordAsync(string id)
    {
        return await _leaderboardDbContext.LeaderboardsRecords
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}