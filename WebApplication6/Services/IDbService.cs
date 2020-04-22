using Microsoft.AspNetCore.Mvc;
using WebApplication6.Models;

namespace WebApplication6.Services
{
    public interface IDbService
    {
        public IActionResult enrollStudent(Student student);

        public IActionResult promoteStudents(StudiesInfo studies);

        
        public string getStudentsString();

        public Student getStudent(string indeks);

        public string getEnrollments(string index);

        public string setRefreshToken(string index, string token);

        public Student getStudentFromRefreshToken(string token);
        
        public void hashhify();
    }
}