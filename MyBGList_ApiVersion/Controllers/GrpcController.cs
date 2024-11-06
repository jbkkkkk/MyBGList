using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Grpc.Net.Client;
using MyBGList.gRPC;

namespace MyBGList.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class GrpcController : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<BoardGameResponse> GetBoardgame(int id)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:55222");

            var client = new gRPC.Grpc.GrpcClient(channel);

            var response = await client.GetBoardGameAsync(new BoardGameRequest { Id = id });

            return response;
        }
    }
}
