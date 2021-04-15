using System;
using System.Threading.Tasks;
using AElf.Boilerplate.EventHandler.MockService.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AElf.Boilerplate.EventHandler.MockService.Controllers
{
    [ApiController]
    [Route("price")]
    public class PriceQueryController : ControllerBase
    {
        [HttpPost("elf")]
        public PriceDto QueryElfPrice()
        {
            return new PriceDto
            {
                Symbol = "ELF",
                Price = (decimal) 4.1 +
                        decimal.Parse(DateTime.UtcNow.ToString("HH")) * (decimal) 0.01 +
                        decimal.Parse(DateTime.UtcNow.ToString("ss")) * (decimal) 0.001,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        [HttpPost("btc")]
        public PriceDto QueryBtcPrice()
        {
            return new PriceDto
            {
                Symbol = "BTC",
                Price = 60000 +
                        decimal.Parse(DateTime.UtcNow.ToString("mmss")) +
                        decimal.Parse(DateTime.UtcNow.ToString("HH")) * 100,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        [HttpPost("eth")]
        public PriceDto QueryEthPrice()
        {
            return new PriceDto
            {
                Symbol = "ETH",
                Price = 2000 +
                        decimal.Parse(DateTime.UtcNow.ToString("HH")) +
                        decimal.Parse(DateTime.UtcNow.ToString("mmss")) * (decimal) 0.1,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        [HttpPost]
        public PriceDto QueryPrice(string symbol)
        {
            return new PriceDto
            {
                Symbol = symbol,
                Price = Math.Abs(HashHelper.ComputeFrom(symbol).ToInt64()) % 100 +
                        decimal.Parse(DateTime.UtcNow.ToString("HH")) +
                        decimal.Parse(DateTime.UtcNow.ToString("mmss")) * (decimal) 0.1,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}