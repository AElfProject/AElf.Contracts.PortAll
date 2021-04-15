using System;
using AElf.Boilerplate.EventHandler.MockService.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Boilerplate.EventHandler.MockService.Controllers
{
    [ApiController]
    [Route("score")]
    public class ScoreQueryController : ControllerBase
    {
        [HttpPost("query")]
        public ScoreDto QueryScore(QueryScoreInput input)
        {
            return new ScoreDto
            {
                Id = input.Id,
                Player1 = input.Player1,
                Player2 = input.Player2,
                Score1 = FabricScore(input.Id, input.Player1),
                Score2 = FabricScore(input.Id, input.Player2)
            };
        }

        [HttpPost]
        public ScoreDto QueryScore(string id, string player1, string player2)
        {
            return new ScoreDto
            {
                Id = id,
                Player1 = player1,
                Player2 = player2,
                Score1 = FabricScore(id, player1),
                Score2 = FabricScore(id, player2)
            };
        }

        private int FabricScore(string id, string player)
        {
            var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(id), HashHelper.ComputeFrom(player));
            return Math.Abs(1 + (int) hash.ToInt64() % 9);
        }
    }
}