using CapstoneProject.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CapstoneProject.Areas.Attendance.Controllers
{
    public class AttendanceController : Controller
    {
        // GET: Attendance/Attendance
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ImportAttendance()
        {
            return View();
        }

        public ActionResult UploadAttendance()
        {
            try
            {
                if (Request.Files.Count > 0)
                {
                    using (var context = new CapstoneProjectEntities())
                    {
                        context.Configuration.AutoDetectChangesEnabled = false;
                        foreach (string file in Request.Files)
                        {
                            var fileContent = Request.Files[file];

                            if (fileContent != null && fileContent.ContentLength > 0)
                            {
                                var stream = fileContent.InputStream;

                                using (ExcelPackage package = new ExcelPackage(stream))
                                {
                                    var ws = package.Workbook.Worksheets.First();
                                    var totalCol = ws.Dimension.Columns;
                                    var totalRow = ws.Dimension.Rows;
                                    var studentCodeCol = 1;
                                    var titleRow = 1;
                                    var firstRecordRow = 2;
                                    var savePoint = 0;
                                    Dictionary<String, Student> wtf = new Dictionary<String, Student>();
                                    var studentList = context.Students.AsNoTracking().ToList();
                                    var courseList = context.Courses.AsNoTracking().Where(q => q.Semester.ToUpper().Equals("SPRING2018_1")).ToList();
                                    var context2 = new CapstoneProjectEntities();
                                    context2.Configuration.AutoDetectChangesEnabled = false;

                                    for (int i = firstRecordRow; i <= totalRow; i++)
                                    {
                                        savePoint++;

                                        var cellStudentRoll = ws.Cells[i, 1].Text.ToUpper();
                                        var cellSubject = ws.Cells[i, 3].Text.ToUpper();
                                        //var course = context.Courses.Where(q => q.Semester.ToUpper().Equals("FALL2017") && q.SubjectCode.ToUpper().Equals(cellSubject)).FirstOrDefault();
                                        bool status = false;
                                        if (ws.Cells[i, 2].Text.Equals("1"))
                                        {
                                            status = true;
                                        }
                                        var taker = ws.Cells[i, 4].Text;
                                        var numberOfSlots = int.Parse(ws.Cells[i, 5].Text);

                                        DateTime recoredTime = DateTime.Now;
                                        try
                                        {
                                            recoredTime = DateTime.ParseExact(ws.Cells[i, 6].Text, "M/d/yy H:mm",
                                     System.Globalization.CultureInfo.InvariantCulture);
                                        }
                                        catch (Exception ex)
                                        {
                                            return Json(new { error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                                        }


                                        //savePoint++;
                                        var student = studentList.Where(q => q.RollNumber.ToUpper().Equals(cellStudentRoll.ToUpper())).FirstOrDefault();
                                        if (student == null)
                                        {

                                            if (!wtf.ContainsKey(cellStudentRoll))
                                            {
                                                Student stu = new Student();
                                                stu.RollNumber = cellStudentRoll;
                                                wtf.Add(cellStudentRoll, stu);
                                                context2.Students.Add(stu);
                                                context2.SaveChanges();
                                            }

                                        }
                                        else
                                        {
                                            var course = courseList.Where(q => q.SubjectCode.ToUpper().Equals(cellSubject)).FirstOrDefault();


                                            //DELETE
                                            //var listRemove = context.Attendances.Where(q => q.StudentId == student.Id && q.CourseId == course.Id && q.RecordTime == q.RecordTime).ToList();

                                            //if (listRemove != null)
                                            //{
                                            //    foreach(var att in listRemove)
                                            //    {
                                            //        context.Attendances.Remove(att);
                                            //        recordDel++;
                                            //    }
                                            //}

                                            //ADD
                                            CapstoneProject.Attendance att = new CapstoneProject.Attendance();
                                            att.CourseId = course.Id;
                                            att.NumberOfSlots = numberOfSlots;
                                            att.StudentId = student.Id;
                                            att.RecordTime = recoredTime;
                                            att.Status = status;
                                            att.Taker = taker;
                                            context2.Attendances.Add(att);

                                        }
                                        if (savePoint == 1000)
                                        {
                                            context2.SaveChanges();
                                            savePoint = 0;
                                            GC.Collect();
                                            context2.Dispose();
                                            context2 = new CapstoneProjectEntities();
                                            context2.Configuration.AutoDetectChangesEnabled = false;
                                        }
                                        if (i == totalRow)
                                        {
                                            context2.SaveChanges();
                                        }
                                    }


                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { error = ex.Message, message = "Errors in uploaded file. Please recheck" });
            }
            return null;
        }

        public ActionResult ImportFromFap()
        {
            var dt = new DataTable();
            var conn = new SqlConnection();
            conn.ConnectionString =
            //"Data Source=116.193.67.20;" +
            "Data Source=10.23.0.77;" +
            "Initial Catalog=AP_HCM;" +
            "User Id=aphcm;" +
            "Password=Kh@nhKT123456&;";

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "SELECT a.RollNumber,a.Status,sub.SubjectCode,a.Taker,c.NumberOfSlots,a.RecordTime"
  + " FROM[AP_HCM].[dbo].[Attendances] a"
  + " Inner Join Schedules s on s.ScheduleID = a.ScheduleID"
  + " Inner Join Courses c on s.CourseID = c.CourseID"
  + " Inner Join Terms t on t.TermID = c.TermID"
  + " Inner Join Subjects sub on sub.SubjectID = c.SubjectID"
  + " Where t.SemesterName = 'Spring2018'";
            cmd.CommandType = CommandType.Text;
            cmd.Connection = conn;
            conn.Open();

            var adapter = new SqlDataAdapter(cmd);

            adapter.Fill(dt);

            var list = dt.AsEnumerable().Select(r => new TempAttendance()
            {
                RollNumber = (string)r["RollNumber"],
                Status = (bool)r["Status"],
                SubjectCode = (string)r["SubjectCode"],
                Taker = (string)r["Taker"],
                NumberOfSlots = (Byte?)r["NumberOfSlots"],
                RecordTime = (DateTime)r["RecordTime"],
                //TakeAttendance = (bool)r["TakeAttendance"],
            }).ToList();
            conn.Close();
            var savePoint = 0;
            using (var context = new CapstoneProjectEntities())
            {
                var studentList = context.Students.ToList();
                var recordDel = 0;
                var courseList = context.Courses.Where(q => q.Semester.ToUpper().Equals("SPRING2018_1")).ToList();
                var last = list.Last();
                Dictionary<String, Student> wtf = new Dictionary<String, Student>();

                foreach (var item in list)
                {
                    using (var context2 = new CapstoneProjectEntities())
                    {
                        savePoint++;
                        var student = studentList.Where(q => q.RollNumber.ToUpper().Equals(item.RollNumber.ToUpper())).FirstOrDefault();
                        if (student == null)
                        {

                            if (!wtf.ContainsKey(item.RollNumber))
                            {
                                Student stu = new Student();
                                stu.RollNumber = item.RollNumber;
                                wtf.Add(item.RollNumber, stu);
                                context2.Students.Add(stu);
                                context2.SaveChanges();
                            }

                        }
                        else
                        {
                            var course = courseList.Where(q => q.SubjectCode.ToUpper().Equals(item.SubjectCode)).FirstOrDefault();
                            var status = item.Status;
                            var recordTime = item.RecordTime;
                            var taker = item.Taker;
                            var numberOfSlots = item.NumberOfSlots;

                            //DELETE
                            //var listRemove = context.Attendances.Where(q => q.StudentId == student.Id && q.CourseId == course.Id && q.RecordTime == q.RecordTime).ToList();

                            //if (listRemove != null)
                            //{
                            //    foreach(var att in listRemove)
                            //    {
                            //        context.Attendances.Remove(att);
                            //        recordDel++;
                            //    }
                            //}

                            //ADD
                            CapstoneProject.Attendance att = new CapstoneProject.Attendance();
                            att.CourseId = course.Id;
                            att.NumberOfSlots = numberOfSlots;
                            att.StudentId = student.Id;
                            att.RecordTime = recordTime;
                            att.Status = status;
                            att.Taker = taker;
                            context2.Attendances.Add(att);

                            context2.SaveChanges();
                        }
                        GC.Collect();
                    }
                }


            }


            return null;
        }
    }
}