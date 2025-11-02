using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectInfrastructure.Context;
using ProjectInfrastructure.Models;
using ProjectInfrastructure.Utils.Sorting;
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
    public async Task<IActionResult> Index(string leaderboardId, int page = 1, int pageSize = 10)
    {
        ViewData["LeaderboardId"] = leaderboardId;
        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);

        if (leaderboard == null)
        {
            return BadRequest(new LeaderboardError().Error(leaderboardId));
        }

        var paginatedRecords = await GetPaginatedRecords(leaderboardId, page, pageSize);
        return View(paginatedRecords);
    }


    private async Task<List<LeaderboardRecordModel>> UpdatePositions(string leaderboardId)
    {
        return await GetSortedRecordsAsync(new UpdatePositionsRequest()
        {
            LeaderboardId = leaderboardId,
            SortBy = SortingParameter.Value,
            Direction = SortingType.Descending,
            Page = 1,
            PageSize = 10
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
    public async Task<IActionResult> GetRecords(string leaderboardId, int page = 1, int pageSize = 10)
    {
        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);

        if (leaderboard == null)
        {
            return BadRequest(new LeaderboardError().Error(leaderboardId));
        }

        var paginatedRecords = await GetPaginatedRecords(leaderboardId, page, pageSize);
        return Ok(paginatedRecords);
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

    private async Task<PaginatedResponse<LeaderboardRecordModel>> GetPaginatedRecords(
        string leaderboardId,
        int page = 1,
        int pageSize = 10,
        SortingParameter sortBy = SortingParameter.Value,
        SortingType direction = SortingType.Descending)
    {
        var request = new UpdatePositionsRequest
        {
            LeaderboardId = leaderboardId,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            Direction = direction,
            Take = pageSize
        };

        var leaderboard = await _leaderboardDbContext.FindLeaderboardAsync(leaderboardId);

        if (leaderboard == null)
            throw new Exception("Leaderboard not found");

        IEnumerable<LeaderboardRecordModel> records = leaderboard.Records;

        var totalCount = records.Count();
        var sortingStrategy = new SortingFactory().GetStrategy(request.SortBy, request.Direction);

        var sortedList = records.ToList();
        sortingStrategy.Sort(sortedList);

        
        for (var i = 0; i < sortedList.Count; i++)
            sortedList[i].Place = i + 1;

        
        var paginatedList = sortedList
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        return new PaginatedResponse<LeaderboardRecordModel>(
            paginatedList,
            page,
            pageSize,
            totalCount
        );
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

        sortedList = sortedList.Skip(request.Skip).Take(request.PageSize).ToList();

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

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedResponse(List<T> data, int page, int pageSize, int totalCount)
    {
        Data = data;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}