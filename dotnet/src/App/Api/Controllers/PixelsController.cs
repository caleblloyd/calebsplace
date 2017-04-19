using System;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using App.Api.Repositories;
using Microsoft.AspNetCore.Http;

namespace App.Api.Controllers
{
    [Route("api/[controller]")]
    public class PixelsController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

	    public PixelsController(IHttpContextAccessor httpContextAccessor)
	    {
	        _httpContextAccessor = httpContextAccessor;
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

			await PixelFetcher.UpdatePixelAsync(x, y, color);
			return Ok();
        }

        // GET api/pixels/draw
        [HttpGet("draw")]
        public async Task<FileStreamResult> Draw()
        {
            return new FileStreamResult(await ImageDraw.Draw.GetMemoryStreamAsync(), "image/png");
        }

        // GET api/pixels/sse
        [HttpGet("sse")]
        public async Task SseAsync([FromQuery]int limit)
        {
            var since = DateTime.UtcNow - TimeSpan.FromSeconds(10);
            var response = _httpContextAccessor.HttpContext.Response;
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("X-Accel-Buffering", "no");

            var seconds = 0;
            while (true)
            {
                var task = Task.Delay(TimeSpan.FromSeconds(1));
                var pixelsUpdatedSinceList = SseFetcher.UpdatedSinceList(since);
                foreach (var pixelsUpdatedSince in pixelsUpdatedSinceList)
                {
                    await response.WriteAsync("data: " + pixelsUpdatedSince.SseString + "\n\n");
                    since = pixelsUpdatedSince.LastUpdated;
                }
                response.Body.Flush();
                await task;
                seconds++;
                if (limit > 0 && seconds > limit)
                    break;
            }
        }
    }

    public class PixelColorRequest
    {
        public string Color;
    }
}
