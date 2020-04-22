using Microsoft.AspNetCore.Mvc;
using WebApplication6.Models;
using WebApplication6.Services;

namespace WebApplication6.Controllers
{
    public class EnrollementController : ControllerBase
    {
        
        [HttpPost]
        public IActionResult EnrollStudent([FromBody]Student student, [FromServices]IDbService dbService)
        {
            if (student.FirstName == null || student.LastName == null || student.IndexNumber == null
                || student.BirthDate == null ) return BadRequest();

            return dbService.enrollStudent(student);
        }
        [HttpPost("promotions")]
        public IActionResult PromoteSemester([FromBody] StudiesInfo studies, [FromServices] IDbService dbService)
        {
            return dbService.promoteStudents(studies);
        }
    }
}