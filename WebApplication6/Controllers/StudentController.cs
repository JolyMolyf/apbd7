using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WebApplication6.DTOs;
using WebApplication6.Models;
using WebApplication6.Services;

namespace WebApplication6.Controllers
{
    [ApiController]
   
    [Route("/api")]
    public class StudentController : ControllerBase
    {

        private IConfiguration iconfig;
        private IDbService idbService;
        

        public StudentController(IDbService dbService, IConfiguration configuration)
        {
            idbService = dbService;
            iconfig = configuration;
        }


        private static String connectionStringGlobal =
            "Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s18714;User ID=inzs18714;Password=admin";

        // GET
        [HttpGet]
        [Authorize (Roles =  "teacher, admin")]
        public IActionResult Index()
        {
            SqlConnection con = new SqlConnection(connectionStringGlobal);
            SqlCommand com = new SqlCommand();
            com.Connection = con;
            com.CommandText = "select * from Student";
            
            con.Open();
            SqlDataReader dr = com.ExecuteReader();
           
            var list = new List<Student>();
            while (dr.Read())
            {
                list.Add(new Student()
                {
                    IndexNumber = dr["IndexNumber"].ToString(),
                    FirstName = dr["Firstname"].ToString(),
                    LastName = dr["Lastname"].ToString()
                });
               
            }
            
            Console.WriteLine("Responded");

            return Ok(list);
        }

        [HttpPost]
        public IActionResult getToken(RequestDTO request)
        {
            Console.WriteLine("Posted");

            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Jan"),
                new Claim(ClaimTypes.Role, "admin"), 
            };
            
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(iconfig["JWT:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "Gakko", 
                audience: "Students", 
                claims: claims, 
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds
                );
            return Ok(new {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = Guid.NewGuid() 
            }); 
        }
        [HttpPost]
        [Route("ref-token/{refToken}")]
        public IActionResult RefreshToken(string refToken)
        {
            var student = idbService.getStudentFromRefreshToken(refToken);
            if (student == null) return NotFound();

            if (student.IndexNumber.Equals("admin"))
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "admin"),
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, "student"),
                    new Claim(ClaimTypes.Role, "teacher")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(iconfig["JWT:Key"]));
                var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken
                (
                    issuer: "Gakko", 
                    audience: "Students", 
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: cred
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    refreshToken = idbService.setRefreshToken("admin", Guid.NewGuid().ToString())
                });
            }
            else
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, student.IndexNumber),
                    new Claim(ClaimTypes.Name, student.FirstName + " " + student.LastName),
                    new Claim(ClaimTypes.Role, "student")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(iconfig["JWT:Key"]));
                var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken
                (
                    issuer: "Gakko", 
                    audience: "Students", 
                    claims: claims, 
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: cred
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    refreshToken = idbService.setRefreshToken(student.IndexNumber, Guid.NewGuid().ToString())
                });
            }
        }
    }
}