using System;
using AElf.EventHandler.MockService.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AElf.EventHandler.MockService.Controllers
{
    [ApiController]
    [Route("score")]
    public class ScoreQueryController : ControllerBase
    {
        [HttpGet("query")]
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

        [HttpGet]
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