using System.Threading.Tasks;
using App.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [Route("api/[controller]")]
    public class ImageController : Controller
    {

        // GET api/image/draw
        [HttpGet("draw")]
        public async Task<FileStreamResult> Draw()
        {
            return new FileStreamResult(await ImageDraw.Draw.GetMemoryStreamAsync(), "image/png");
        }

	}
}
