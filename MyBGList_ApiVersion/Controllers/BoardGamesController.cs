using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;
using MyBGList.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.ComponentModel.DataAnnotations;
using MyBGList.Attributes;
using MyBGList.Constants;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BoardGamesController : ControllerBase
    {
        private readonly ILogger<BoardGamesController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMemoryCache _memoryCache;

        public BoardGamesController(ILogger<BoardGamesController> logger, ApplicationDbContext dbContext, IMemoryCache memoryCache)
        {
            _logger = logger;
            _dbContext = dbContext;
            _memoryCache = memoryCache;
        }

        [HttpGet(Name = "GetBoardGames")]
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<BoardGame[]>> Get(
            [FromQuery] RequestDTO<BoardGameDTO> input)
        {
            _logger.LogInformation(CustomLogEvents.BoardGamesController_Get, "Get method started.");
            var query = _dbContext.BoardGames.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(b => b.Name.Contains(input.FilterQuery));
            var recordCount = await query.CountAsync();

            BoardGame[]? result = null;

            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";

            if (!_memoryCache.TryGetValue<BoardGame[]>(cacheKey, out result))
            {
                query = query
                    .OrderBy($"{input.SortColumn} {input.SortOrder}")
                    .Skip(input.PageIndex * input.PageSize)
                    .Take(input.PageSize);
                result = await query.ToArrayAsync();
                _memoryCache.Set(cacheKey, result, new TimeSpan(0,0,30));
            }

            return new RestDTO<BoardGame[]>()
            {
                Data = result,
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = recordCount,
                Links = new List<LinkDTO> {
                new LinkDTO(
                    Url.Action(null, "BoardGames",new { input.PageIndex, input.PageSize }, null, Request.Scheme)!,
                    "self",
                    "GET")
                }
            };
        }

        [HttpGet("{id}")]
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<BoardGame?>> GetBoardGame(int id)
        {
            _logger.LogInformation(CustomLogEvents.BoardGamesController_Get,
            "GetBoardGame method started.");
            BoardGame? result = null;
            var cacheKey = $"GetBoardGame-{id}";
            if (!_memoryCache.TryGetValue<BoardGame>(cacheKey, out result))
            {
                result = await _dbContext.BoardGames.FirstOrDefaultAsync(bg => bg.Id == id);
                _memoryCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }
            return new RestDTO<BoardGame?>()
            {
                Data = result,
                PageIndex = 0,
                PageSize = 1,
                RecordCount = result != null ? 1 : 0,
                Links = new List<LinkDTO> {
                    new LinkDTO(
                    Url.Action(
                    null,
                    "BoardGames",
                    new { id },
                    Request.Scheme)!,
                    "self",
                    "GET"),
                    }
            };
        }
        [HttpPost(Name = "UpdateBoardGame")]
        [ResponseCache(NoStore = true)]
        [Authorize(Roles = RoleNames.Moderator)]
        public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
        {
            var boardgame = await _dbContext.BoardGames
                .Where(b => b.Id == model.Id)
                .FirstOrDefaultAsync();
            if (boardgame != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    boardgame.Name = model.Name;
                if (model.Year.HasValue && model.Year.Value > 0)
                    boardgame.Year = model.Year.Value;
                boardgame.LastModifiedDate = DateTime.Now;
                _dbContext.BoardGames.Update(boardgame);
                await _dbContext.SaveChangesAsync();
            };

            return new RestDTO<BoardGame?>()
            {
                Data = boardgame,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "BoardGames", model, Request.Scheme)!, 
                    "self", 
                    "POST")
                }
            };
        }

        [HttpDelete(Name = "DeleteBoardGame")]
        [ResponseCache(NoStore = true)]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<RestDTO<BoardGame?>> Delete(int id)
        {
            var boardgame = await _dbContext.BoardGames
                .Where(b => b.Id == id)
                .FirstOrDefaultAsync();
            if (boardgame != null)
            {
                _dbContext.Remove(boardgame);
                await _dbContext.SaveChangesAsync();
            }


            return new RestDTO<BoardGame?>()
            {
                Data = boardgame,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(Url.Action(null, "BoardGames", id, Request.Scheme)!, "self", "DELETE")
                }
            };
        }
    }
}

