using AonFreelancing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;

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
        public IActionResult Get(int id)
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
    

        [HttpPut("{id}")]
        
        public IActionResult Update(int id, [FromBody]Project Updateproject)
        { 
           Project project=_projects.FirstOrDefault(pr=>pr.Id == id);
            if (project != null)
            {
               
                project.Name=Updateproject.Name;
                return NoContent();

            }
            return NotFound();
        }

    }
}

