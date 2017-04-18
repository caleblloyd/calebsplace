using App.Db;
using App.Api.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Api.Controllers
{
    [Route("api/[controller]")]
    public class ImageController : Controller
    {
	    private readonly AppDb _db;

	    public ImageController(AppDb db)
	    {
		    _db = db;
	    }

        // GET api/image
        [HttpGet]
        public async Task<IActionResult> ListAsync()
        {
			return new OkObjectResult(await _db.Posts.Include(m => m.Author).OrderByDescending(m => m.Id).ToListAsync());
        }
	}
}
