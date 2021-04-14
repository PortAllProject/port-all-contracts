using System;
using AElf.Boilerplate.EventHandler.MockService.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Boilerplate.EventHandler.MockService.Controllers
{
    [ApiController]
    [Route("score")]
    public class ScoreQueryController : ControllerBase
    {
        [HttpPost]
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

        private int FabricScore(string id, string player)
        {
            var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(id), HashHelper.ComputeFrom(player));
            return Math.Abs(1 + (int) hash.ToInt64() % 9);
        }
    }
}