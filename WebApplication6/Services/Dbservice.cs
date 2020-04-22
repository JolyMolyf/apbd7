using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using WebApplication6.Models;

namespace WebApplication6.Services
{
    public class SqlServerDbService : IDbService
    {
        private string sqlConGlobal =
            "Data Source=db-mssql.pjwstk.edu.pl;Initial Catalog=s18714;User ID=inzs18714;Password=admin";

        public IActionResult enrollStudent(Student student)
        {
            try
            {
                Enrollement enrollment = new Enrollement();

                var con = new SqlConnection(sqlConGlobal);
                var com = new SqlCommand();


                com.Connection = con;
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();
                com.Transaction = transaction;

                List<string[]> resultSet = new List<string[]>();

                com.CommandType = CommandType.StoredProcedure;
                com.CommandText = "BEGIN " +
                                  "DECLARE @idStudy int = (SELECT Studies.IdStudy FROM Studies" +
                                  "WHERE Studies.Name = @studiesName); " +
                                  "DECLARE @idEnrollment int = (SELECT TOP 1 Enrollment.IdEnrollment FROM Enrollment " +
                                  "ORDER BY Enrollment.IdEnrollment DESC) + 1; " +
                                  "INSERT INTO Enrollment(IdEnrollment, Semester, IdStudy, StartDate)" +
                                  "VALUES (@idEnrollment, 1, @idStudy, CURRENT_TIMESTAMP) ; " +
                                  "Select @idEnrollment;" +
                                  "END";
                if (com.Connection.State != ConnectionState.Open) com.Connection.Open();

                SqlDataReader dr;
                dr = com.ExecuteReader();

                while (dr.Read())
                {
                    string[] tmp = new string[dr.FieldCount];
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        tmp[i] = dr.GetValue(i).ToString();
                    }

                    resultSet.Add(tmp);
                }

                
                com.CommandType = CommandType.Text;
                com.Parameters.Clear();
                
                if (resultSet.Count == 0) return new BadRequestResult();

                com.CommandText = "SELECT * FROM Student WHERE Student.IndexNumber = @indexNumber";
                com.Parameters.AddWithValue("indexNumber", student.IndexNumber);

                dr = com.ExecuteReader();
                if (dr.Read()) return new BadRequestResult();
                dr.Close();

                com.CommandText = "DECLARE @datetmp date = PARSE(@bdate as date USING 'en-GB');" +
                                  " INSERT INTO Student(IndexNumber, FirstName, LastName, BirthDate, IdEnrollment, Password, Salt)" +
                                  " VALUES (@indexNumber, @name, @lname, @datetmp, '1', @pass, @salt)";
                com.Parameters.Clear();
                com.Parameters.AddWithValue("indexNumber", student.IndexNumber);

                byte[] randomBytes = new byte[256 / 4];
                var generator = RandomNumberGenerator.Create();

                generator.GetBytes(randomBytes);
                var resultSalt = Convert.ToBase64String(randomBytes);
                var pass = generateHash("pas" + student.IndexNumber, resultSalt);

                com.Parameters.AddWithValue("pass", pass);
                com.Parameters.AddWithValue("salt", resultSalt);
                com.Parameters.AddWithValue("name", student.FirstName);
                com.Parameters.AddWithValue("lname", student.LastName);
                com.Parameters.AddWithValue("bdate", student.BirthDate);
                com.ExecuteNonQuery();

                com.Parameters.Clear();

                com.Parameters.AddWithValue("indexNumber", student.IndexNumber);
                
                List<string[]> resultSet2 = new List<string[]>();

                com.CommandType = CommandType.StoredProcedure;
                com.CommandText = "DECLARE @datetmp date = PARSE(@bdate as date USING 'en-GB'); INSERT INTO Student VALUES(@indexNumber, @fname, @lname, @datetmp, @idEnrollment)";
                com.Parameters.Clear();
                if (com.Connection.State != ConnectionState.Open) com.Connection.Open();

                dr = com.ExecuteReader();

                while (dr.Read())
                {
                    string[] tmp = new string[dr.FieldCount];
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        tmp[i] = dr.GetValue(i).ToString();
                    }

                    resultSet2.Add(tmp);
                }

                dr.Close();
                com.CommandType = CommandType.Text;
                com.Parameters.Clear();


                enrollment.IdEnrollment = resultSet2[0][0];
                enrollment.IdStudy = resultSet2[0][2];
                enrollment.Semester = resultSet2[0][1];
                enrollment.StartDate = resultSet2[0][3];

                transaction.Commit();
                ObjectResult objectResult = new ObjectResult(enrollment);
                objectResult.StatusCode = 200;
                return objectResult;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new BadRequestResult();
            }
        }

        public IActionResult promoteStudents(StudiesInfo studies)
        {
            Enrollement enrollment = new Enrollement();
            using (SqlConnection con = new SqlConnection(sqlConGlobal))
            using (SqlCommand com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();


                com.Parameters.AddWithValue("Studies", studies.Studies);
                com.Parameters.AddWithValue("Semester", studies.Semester);
                List<string[]> resultSet3 = new List<string[]>();

                com.CommandType = CommandType.StoredProcedure;
                com.CommandText = "UPDATE Student SET Password = @password where Student.IndexNumber = @index";
                SqlDataReader dr = com.ExecuteReader();
                com.Parameters.AddWithValue("index", dr["IndexNumber"]);
                com.Parameters.AddWithValue("password", dr["password"]);
                if (com.Connection.State != ConnectionState.Open) com.Connection.Open();

                dr = com.ExecuteReader();

                while (dr.Read())
                {
                    string[] tmp = new string[dr.FieldCount];
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        tmp[i] = dr.GetValue(i).ToString();
                    }

                    resultSet3.Add(tmp);
                }

                dr.Close();
                com.CommandType = CommandType.Text;
                com.Parameters.Clear();


                if (resultSet3[0][0].Equals("404"))
                {
                    return new NotFoundResult();
                }

                enrollment.IdEnrollment = resultSet3[0][0];
                enrollment.IdStudy = resultSet3[0][2];
                enrollment.Semester = resultSet3[0][1];
                enrollment.StartDate = resultSet3[0][3];
            }

            ObjectResult objectResult = new ObjectResult(enrollment);
            objectResult.StatusCode = 201;
            return objectResult;
        }


        public string getStudentsString()
        {
            StringBuilder students = new StringBuilder();
            using (var con = new SqlConnection(sqlConGlobal))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                com.CommandText =
                    "SELECT IndexNumber, FirstName, LastName, BirthDate, Semester, Name FROM Student " +
                    "INNER JOIN Enrollment ON Student.IdEnrollment = Enrollment.IdEnrollment " +
                    "INNER JOIN Studies ON Studies.IdStudy = Enrollment.IdStudy";

                con.Open();
                var dr = com.ExecuteReader();
                while (dr.Read())
                {
                    var st = new Student();
                    st.FirstName = dr["FirstName"].ToString();
                    st.LastName = dr["LastName"].ToString();
                    st.BirthDate = dr["BirthDate"].ToString();
                    st.IndexNumber = dr["IndexNumber"].ToString();
                    students.AppendLine(st.ToString());
                }
            }

            return students.ToString();
        }

        public Student getStudent(string index)
        {
            var sudent = new Student();
            using (var con = new SqlConnection(sqlConGlobal))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                com.CommandText =
                    "SELECT IndexNumber, Password, FirstName, LastName, BirthDate, Semester, Name FROM Student " +
                    "INNER JOIN Enrollment ON Student.IdEnrollment = Enrollment.IdEnrollment " +
                    "INNER JOIN Studies ON Studies.IdStudy = Enrollment.IdStudy WHERE Student.IndexNumber = @index";

                com.Parameters.AddWithValue("index", index);

                con.Open();
                var dr = com.ExecuteReader();
                dr.Read();


                sudent.IndexNumber = dr["IndexNumber"].ToString();
                sudent.FirstName = dr["FirstName"].ToString();
                sudent.LastName = dr["LastName"].ToString();
                sudent.BirthDate = dr["BirthDate"].ToString();
            }

            return sudent;
        }

        public string getEnrollments(string index)
        {
            StringBuilder enrollements = new StringBuilder();

            var con = new SqlConnection(sqlConGlobal);
            var com = new SqlCommand();

            com.Connection = con;
            com.CommandText =
                "SELECT * FROM Enrollment INNER JOIN Student ON Enrollment.IdEnrollment = Student.IdEnrollment WHERE Student.IndexNumber = @index";
            com.Parameters.AddWithValue("index", index);

            con.Open();
            var dr = com.ExecuteReader();
            while (dr.Read())
            {
                var en = new Enrollement();
                en.IdEnrollment = dr["IdEnrollment"].ToString();
                en.Semester = dr["Semester"].ToString();
                en.IdStudy = dr["IdStudy"].ToString();
                en.StartDate = dr["StartDate"].ToString();
                enrollements.AppendLine(en.ToString());
            }


            return enrollements.ToString();
        }

        public string setRefreshToken(string index, string token)
        {
            var con = new SqlConnection(sqlConGlobal);
            var com = new SqlCommand();

            com.Connection = con;
            com.CommandText =
                "UPDATE Student SET RefToken = @token WHERE Student.IndexNumber = @index";
            com.Parameters.AddWithValue("index", index);
            com.Parameters.AddWithValue("token", token);

            con.Open();
            com.ExecuteNonQuery();

            return token;
        }

        public Student getStudentFromRefreshToken(string token)
        {
            using (SqlConnection con = new SqlConnection(sqlConGlobal))
            using (SqlCommand com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();
                com.CommandText = "SELECT IndexNumber, RefToken FROM Student WHERE Student.RefToken = @token";
                com.Parameters.AddWithValue("token", token);

                var dr = com.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    return getStudent(dr["IndexNumber"].ToString());
                }
                else
                {
                    return null;
                }
            }
        }


        public void hashhify()
        {
            var con = new SqlConnection(sqlConGlobal);
            var com = new SqlCommand();

            com.Connection = con;
            com.CommandText = "SELECT IndexNumber, Password FROM Student ";

            con.Open();
            var dr = com.ExecuteReader();
            while (dr.Read())
            {
                var index = dr["IndexNumber"].ToString();
                byte[] randomBytes = new byte[256 / 4];
                var generator = RandomNumberGenerator.Create();

                generator.GetBytes(randomBytes);
                var resultSalt = Convert.ToBase64String(randomBytes);

                var con2 = new SqlConnection(sqlConGlobal);
                var com2 = new SqlCommand();

                com2.Connection = con2;
                com2.CommandText =
                    "UPDATE Student SET Salt = @salt WHERE Student.IndexNumber = @index";
                com2.Parameters.AddWithValue("index", index);
                com2.Parameters.AddWithValue("salt", resultSalt);

                con2.Open();
                com2.ExecuteNonQuery();

                string pass = generateHash(dr["Password"].ToString(), resultSalt);
                var con3 = new SqlConnection(sqlConGlobal);
                var com3 = new SqlCommand();

                com3.Connection = con3;
                com3.CommandText =
                    "UPDATE Student SET Password = @pass WHERE Student.IndexNumber = @indeks";
                com3.Parameters.AddWithValue("indeks", index);
                com3.Parameters.AddWithValue("pass", pass);

                con3.Open();
                com3.ExecuteNonQuery();
            }

            dr.Close();
        }

      

        public static string generateHash(string value, string salt)
        {
            var valueBytes = KeyDerivation.Pbkdf2(
                password: value,
                salt: Encoding.UTF8.GetBytes(salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 4
            );

            var hashedData = Convert.ToBase64String(valueBytes);
            return Convert.ToBase64String(valueBytes);
        }

        public static bool Validate(string value, string salt, string hash)
        {
            return generateHash(value, salt).Equals(hash);
        }
    }
}