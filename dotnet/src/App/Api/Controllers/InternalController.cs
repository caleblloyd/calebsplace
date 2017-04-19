using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using App.Api.Repositories;

namespace App.Api.Controllers
{
    [Route("api/[controller]")]
    public class InternalController : Controller
    {
		
        // GET api/internal/5
        [HttpGet("{batchNumber}")]
        public async Task<FileStreamResult> GetAsync(int batchNumber, [FromQuery]DateTime since)
        {
            var pixelsUpdatedSince = await PixelFetcher.UpdatedSinceAsync(batchNumber, since);
            return new FileStreamResult(pixelsUpdatedSince.GetMemoryStream(), "text/plain");
        }

    }

}