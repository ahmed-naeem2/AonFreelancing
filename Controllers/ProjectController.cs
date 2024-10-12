using AonFreelancing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AonFreelancing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private static List<Project> _projects = new List<Project>();
        [HttpGet]
        public IActionResult GetAll()
        {

            return Ok(_projects);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Project project)
        {
            _projects.Add(project);
            return CreatedAtAction("Create", new { Id = project.Id }, _projects);
        }

        [HttpGet("{id}")]
        public IActionResult GetFreelancer(int id)
        {
            Project pr = _projects.FirstOrDefault(pr => pr.Id == id);

            if (pr == null)
            {
                return NotFound("The resoucre is not found!");
            }

            return Ok(pr);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            Project pr = _projects.FirstOrDefault(pr => pr.Id == id);
            if (pr != null)
            {
                _projects.Remove(pr);
                return Ok("Deleted");

            }

            return NotFound();
        }
        [HttpPost("{id}")]
        public IActionResult update([FromBody] Project project)
        {
            _projects.Add(project);
            return CreatedAtAction("Update", new { id = project.Id }, _projects);
        }



    }
}

