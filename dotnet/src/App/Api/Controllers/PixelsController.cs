using App.Db;
using App.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Threading.Tasks;

namespace App.Api.Controllers
{
    [Route("api/[controller]")]
    public class PixelsController : Controller
    {
		private readonly IMemoryCache _cache;
	    private readonly AppDb _db;

	    public PixelsController(AppDb db, IMemoryCache cache)
	    {
			_cache = cache;
		    _db = db;
	    }
		
        // GET api/pixels/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAsync(int id)
        {
			var model = await _db.Authors.FindAsync(id);
			await _db.Entry(model).Collection(m => m.Posts).Query().OrderByDescending(m => m.Id).ToListAsync();
			if (model != null)
			{
				return new OkObjectResult(model);
			}
			return NotFound();
        }

        // PUT api/async/5
        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] Author body)
        {
			var model = await _db.Authors.FindAsync(id);
			if (model != null)
			{
				model.Name = body.Name;
				await _db.SaveChangesAsync();
				return new OkObjectResult(model);
			}
			return NotFound();
        }

        // DELETE api/async/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
			var model = await _db.Authors.FindAsync(id);
			if (model != null)
			{
				_db.Authors.Remove(model);
				await _db.SaveChangesAsync();
				return Ok();
			}
			return NotFound();
        }
    }
}