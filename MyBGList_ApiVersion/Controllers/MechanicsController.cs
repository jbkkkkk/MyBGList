using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;
using MyBGList.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.ComponentModel.DataAnnotations;
using MyBGList.Attributes;
using System.Reflection.Metadata;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;
using MyBGList.Extensions;
using Microsoft.AspNetCore.Authorization;
using MyBGList.Constants;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MechanicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MechanicsController> _logger;
        private readonly IDistributedCache _distributedCache;

        public MechanicsController(ApplicationDbContext context, ILogger<MechanicsController> logger, IDistributedCache distributedCache)
        {
            _context = context;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        [HttpGet(Name = "GetMechanics")]
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<Mechanic[]>> Get([FromQuery] RequestDTO<MechanicDTO> input)
        {
            var query = _context.Mechanics.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(b => b.Name.Contains(input.FilterQuery));

            var recordCount = await query.CountAsync();

            Mechanic[]? result = null;

            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";

            if (!_distributedCache.TryGetValue<Mechanic[]>(cacheKey, out result))
            {
                query = query
                    .OrderBy($"{input.SortColumn} {input.SortOrder}")
                    .Skip(input.PageIndex * input.PageSize)
                    .Take(input.PageSize);
                result = await query.ToArrayAsync();
                _distributedCache.Set(cacheKey, result, new TimeSpan(0,0,30));
            }

            return new RestDTO<Mechanic[]>()
            {
                PageSize = input.PageSize,
                PageIndex = input.PageIndex,
                RecordCount = recordCount,
                Data = result,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(
                        Url.Action(null, "Mechanics", new {input.PageSize, input.PageIndex}, null, Request.Scheme)!,
                        "self",
                        "GET"
                    )
                }
            };
        }
        [HttpPost(Name = "UpdateMechanic")]
        [ResponseCache(NoStore = true)]
        [Authorize(Roles = RoleNames.Moderator)]
        public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
        {
            var mechanic = await _context.Mechanics
                .Where(b => b.Id == model.Id)
                .FirstOrDefaultAsync();
            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    mechanic.Name = model.Name;
                mechanic.LastModifiedDate = DateTime.Now;
                _context.Mechanics.Update(mechanic);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO> { new LinkDTO(
                    Url.Action(null, "Mechanic", model, null, Request.Scheme)!,
                    "self",
                    "POST")
                }
            };
        }

        [HttpDelete(Name = "DeleteMechanic")]
        [ResponseCache(NoStore = true)]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<RestDTO<Mechanic?>> Delete(int id)
        {
            var mechanic = await _context.Mechanics
                .Where(b => b.Id == id)
                .FirstOrDefaultAsync();
            if (mechanic != null)
            {
                _context.Mechanics.Remove(mechanic);
                await _context.SaveChangesAsync();
            }
            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                { new LinkDTO(
                    Url.Action(null, "Mechanic", id, null, Request.Scheme)!,
                    "self",
                    "DELETE"
                    )
                }
            };
        }
    }
}
