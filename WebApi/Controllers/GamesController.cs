using ClickHouse.Driver;
using ClickHouse.Driver.ADO;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class GamesController : ControllerBase
{
    private readonly ILogger<GamesController> _logger;

    public GamesController(ILogger<GamesController> logger)
    {
        _logger = logger;
    }
    
    public record GenreDto(string Genre, int Games, long AvgFollowers);

    [HttpGet]
    public async Task<IEnumerable<GenreDto>> Genres([FromServices] IClickHouseConnection connection)
    {
        // примерное
        const string sql = @"
            SELECT
                genre,
                COUNT() AS games,
                AVG(followers) AS avgFollowers
            FROM games
            ARRAY JOIN genres AS genre
            GROUP BY genre
            ORDER BY games DESC
            LIMIT 5;
        ";

        var result = (await connection.QueryAsync(sql))
            .Select(r => new GenreDto((string)r.genre, Convert.ToInt32(r.games), Convert.ToInt64(r.avgFollowers)));

        return result;
    }

    public record GameDto(
        long AppId,
        string Title,
        string ReleaseDate,
        string[] Tags,
        long FollowersCount,
        string ShopUrl,
        string ImageUrl,
        string ShortDescription,
        string[] Platforms);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="month">2025-11</param>
    [HttpGet("")]
    public async Task<IEnumerable<GameDto>> Get(string month, string[]? platforms, string[]? tags)
    {
        throw new NotImplementedException();
    }

    public record GamesCalendarDayDto(string Date, int Count);

    public record GamesCalendarDto(string Month, GamesCalendarDayDto[] Days);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="month">2025-11</param>
    /// <param name="platforms"></param>
    /// <param name="tags"></param>
    [HttpGet]
    public async Task<GamesCalendarDto> Calendar(string month, string[]? platforms, string[]? tags)
    {
        throw new NotImplementedException();
    }

    

    public record GenreForDynamicsDto(string Month, string Genre, long AvgFollowers);

    [HttpGet("genres/dynamics")]
    public async Task<IEnumerable<GenreForDynamicsDto>> GenresDynamics()
    {
        throw new NotImplementedException();
    }
}