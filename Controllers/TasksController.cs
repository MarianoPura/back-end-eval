using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

using TaskManager.Models;
using TaskManager.Data;
namespace TaskManager.API
{
    [Route("tasks")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!.Value);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = GetUserId();
            var tasks = await _context.Tasks.Where(t => t.UserId == userId).ToListAsync();
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title is required" });

            var userId = GetUserId();

            var task = new TaskItem
            {
                Title = request.Title,
                IsDone = request.IsDone,
                UserId = userId
            };

            try
            {
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("duplicate key") == true ||
                    ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    return Conflict(new { message = "You already added this task." });
                }
                throw;
            }
        }

        [HttpPut("{id}")] 
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest updated)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            task.Title = updated.Title;
            task.IsDone = updated.IsDone;
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
    }
}
