using Microsoft.AspNetCore.Mvc;

namespace Group8WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgeController : ControllerBase
    {
        public record AgeRequest(DateTime DateOfBirth);
        public record AgeResponse(int Years);

        [HttpPost]
        public IActionResult Post([FromBody] AgeRequest? request)
        {
            if (request is null)
                return BadRequest("Request body is required. JSON: { \"dateOfBirth\": \"YYYY-MM-DD\" }");

            var dob = request.DateOfBirth.Date;
            var today = DateTime.UtcNow.Date;

            if (dob > today)
                return BadRequest("Date of birth cannot be in the future.");

            var years = today.Year - dob.Year;
            if (dob > today.AddYears(-years)) years--;

            return Ok(new AgeResponse(years));
        }

        // New operation: accept a name and return a simple greeting
        public record NameRequest(string Name);
        public record NameResponse(string Name, string Greeting);

        [HttpPost("name")]
        public IActionResult PostName([FromBody] NameRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Request body is required. JSON: { \"name\": \"Your Name\" }");

            var name = request.Name.Trim();
            var greeting = $"Hello, {name}!";
            return Ok(new NameResponse(name, greeting));
        }
    }
}