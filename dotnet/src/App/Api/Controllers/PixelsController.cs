using System;
using System.Collections.Generic;
using App.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Threading.Tasks;
using App.Api.Models;

namespace App.Api.Controllers
{
    [Route("api/[controller]")]
    public class PixelsController : Controller
    {
		private readonly IMemoryCache _cache;

	    public PixelsController(AppDb db, IMemoryCache cache)
	    {
			_cache = cache;
	    }
		
        // GET api/pixels/5
        [HttpGet("{batchNumber}")]
        public async Task<IActionResult> GetAsync(int batchNumber, [FromQuery]DateTime since)
        {
            var batch = await PixelFetcher.FetchBatchAsync(_cache, batchNumber);
            var data = new List<Pixel>();
            var lastTouched = default(DateTime);

            for (var x = 0; x < PixelFetcher.PixelsInBatch; x++)
            {
                for (var y = 0; y < PixelFetcher.Pixels; y++)
                {
                    var pixel = batch[x, y];
                    if (pixel.Touched > lastTouched)
                        lastTouched = pixel.Touched;
                    if (since == default(DateTime) || pixel.Touched > since)
                        data.Add(pixel);
                }
            }

            return new OkObjectResult(new
            {
                LastTouched = lastTouched,
                Data = data
            });
        }

        // POST api/pixels/001/002
        [HttpPost("{x}/{y}")]
        public async Task<IActionResult> UpdateAsync(int x, int y, [FromBody] PixelColorRequest pixelColorRequest)
        {
            if (x < 0 || x >= PixelFetcher.Pixels || y < 0 || y >= PixelFetcher.Pixels)
                return BadRequest($"X and Y must be between 0 and {PixelFetcher.Pixels}");

            if (string.IsNullOrWhiteSpace(pixelColorRequest.Color) || pixelColorRequest.Color.Length != 6)
                return BadRequest("Color must be 6 hex characters");

            var color = Enumerable.Range(0, pixelColorRequest.Color.Length / 2)
                .Select(i => Convert.ToByte(pixelColorRequest.Color.Substring(i * 2, 2), 16))
                .ToArray();

			await PixelFetcher.UpdatePixelAsync(_cache, x, y, color);
			return Ok();
        }
    }

    public class PixelColorRequest
    {
        public string Color;
    }
}