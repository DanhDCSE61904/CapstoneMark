﻿using FuGradeLib;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Cors;
using System.Web.Mvc;
using static CapstoneProject.Models.AreaViewModel;

namespace CapstoneProject.Areas.Mark.Controllers
{
    
    public class MarkController : Controller
    {

        // GET: Mark/Mark
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ImportMark()
        {
            return View();
        }

        public ActionResult StudentCourses()
        {
            return View();
        }


        public ActionResult StudentMarkDetail(string rollNumber,int courseId)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var studentName = context.Students.Where(q => q.RollNumber.Equals(rollNumber)).FirstOrDefault().FullName;
                var course = context.Courses.Find(courseId);
                var semester = course.Semester;
                var subject = course.SubjectCode;
                ViewBag.studentName = studentName;
                ViewBag.semester = semester;
                ViewBag.subjectCode = subject;
                ViewBag.rollNumber = rollNumber;
                ViewBag.courseId = courseId;
            }
            return View();
        }

        public ActionResult LoadSemesterSelect()
        {
            using (var context = new CapstoneProjectEntities())
            {
                var semesters = context.RealSemesters.Select(q => new SemesterViewModel
                {
                    SemesterId = q.Id,
                    Semester = q.Semester,
                }).ToList();
                return Json(new { result = semesters, });
            }

        }
        [AllowCrossSite]
        public JsonResult LoadMarkTable(JQueryDataTableParamModel param, int semesterId)
        {
            try
            {
                using (var context = new CapstoneProjectEntities())
                {
                    IQueryable<CapstoneProject.Mark> searchList = context.Marks;
                    if (param.sSearch != null && param.sSearch != "")
                    {
                        searchList = context.Marks.Where(q => (q.Student.RollNumber.Contains(param.sSearch) || q.Student.FullName.Contains(param.sSearch) || q.Subject_MarkComponent.SubjectId.Contains(param.sSearch) || q.Subject_MarkComponent.MarkComponent.Name.Contains(param.sSearch)) && q.SemesterId == semesterId && q.Status == null);
                    }
                    else
                    {
                        searchList = context.Marks.Where(q => q.SemesterId == semesterId && q.Status == null);
                    }
                    var mark = searchList.AsEnumerable().OrderBy(q => q.Student.RollNumber).Skip(param.iDisplayStart)
                        .Take(param.iDisplayLength).Select(q => new IConvertible[]
                    {
                       q.Student.RollNumber,
                        q.Student.FullName,
                       q.Subject_MarkComponent.SubjectId,
                        q.Subject_MarkComponent.MarkComponent.Name,
                       q.AverageMark==null?"0":q.AverageMark.Value.ToString(),
                       q.Subject_MarkComponent.PercentWeight==null?"0":q.Subject_MarkComponent.PercentWeight.Value.ToString(),
                    }).ToList();


                    var totalRecords = searchList.Count();
                    var totalDisplay = mark.Count();
                    return Json(new
                    {
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = totalRecords,
                        aaData = mark,
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, message = "Errors in uploaded file. Please recheck" });
            }

        }
        [AllowCrossSite]
        public JsonResult LoadCoursesByStudent(JQueryDataTableParamModel param, string rollNumber)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var courses = context.Marks.Where(q => q.Student.RollNumber.Equals(rollNumber)).Select(q => q.Course).Distinct().ToList();
                var result = courses.Select(q => new IConvertible[] {
                    q.Semester,
                    q.SubjectCode,
                    q.Id,
                }).ToList();
                return Json(new {
                     sEcho = param.sEcho,
                    iTotalRecords = result.Count,
                    iTotalDisplayRecords = result.Count,
                    aaData = result,
                },JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult LoadMarkByStudentAndCourse(JQueryDataTableParamModel param, string rollNumber,int courseId)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var result = context.Marks.Where(q => q.CourseId == courseId && q.Student.RollNumber.Equals(rollNumber)&& q.Subject_MarkComponent.FinalComponent==null).AsEnumerable().Select(q=>new IConvertible[] {
                    q.Subject_MarkComponent.MarkComponent.Name,
                    q.AverageMark!=null?Math.Round(q.AverageMark.Value,1,MidpointRounding.ToEven).ToString():"-",
                    q.Subject_MarkComponent.PercentWeight,
                }).ToList();
                return Json(new
                {
                    sEcho = param.sEcho,
                    iTotalRecords = result.Count,
                    iTotalDisplayRecords = result.Count,
                    aaData = result,
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        [AllowCrossSite]
        public JsonResult LoadStudentSelector()
        {
            using (var context = new CapstoneProjectEntities())
            {
               var students = context.Students.AsEnumerable().Select(q => new Student
                {
                    RollNumber = q.RollNumber,
                    FullName = q.FullName,
                }).ToList();
                return Json(new { data=students  },JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult UploadMarkFiles()
        {
            int a = Request.Files.Count;
            //FileStream fileStream = new FileStream(@"C:\Users\USER\Desktop\SO DIEM FALL 2017\SO DIEM FALL 2017\10T\FA17__10T_VanTTN.fg", FileMode.Open);
            for (int i = 0; i < a; i++)
            {
                try
                {
                    using (var context = new CapstoneProjectEntities())
                    {
                        string extension = System.IO.Path.GetExtension(Request.Files[i].FileName);
                        if (extension.Equals(".fg"))
                        {
                            var gradeFile = (TeacherGrade)new BinaryFormatter
                            {
                                AssemblyFormat = FormatterAssemblyStyle.Simple
                            }.Deserialize(Request.Files[i].InputStream);

                            foreach (var mark in gradeFile.SubjectClassGrades)
                            {
                                var semesterId = context.RealSemesters.Where(q => q.Semester.Equals(gradeFile.Semester.ToUpper())).FirstOrDefault().Id;
                                var courseId = context.Courses.Where(q => q.SubjectCode.Equals(mark.Subject) && q.Semester.Equals(gradeFile.Semester.ToUpper())).FirstOrDefault().Id;
                                foreach (var student in mark.Students)
                                {
                                    try
                                    {
                                        var studentEntity = context.Students.Where(q => q.RollNumber.ToUpper().Equals(student.Roll.ToUpper())).FirstOrDefault();
                                        int studentId = 0;
                                        if (studentEntity != null)
                                        {
                                            studentId = studentEntity.Id;
                                        }
                                        else
                                        {
                                            Debug.WriteLine(student.Roll);
                                            Student newStu = new Student();
                                            newStu.FullName = student.Name;
                                            newStu.RollNumber = student.Roll;
                                            context.Students.Add(newStu);
                                            context.SaveChanges();
                                            studentEntity = context.Students.Where(q => q.RollNumber.ToUpper().Equals(student.Roll.ToUpper())).FirstOrDefault();
                                        }
                                        Dictionary<String, GradeTimes> dic = new Dictionary<string, GradeTimes>();
                                        foreach (var grade in student.Grades)
                                        {
                                            string gradeComp = new String(grade.Component.Where(c => (c < '0' || c > '9')).ToArray());
                                            if (!dic.ContainsKey(gradeComp))
                                            {
                                                GradeTimes newGradeTime = new GradeTimes();
                                                newGradeTime.Grade = grade.Grade;
                                                newGradeTime.GradeComp = gradeComp;
                                                newGradeTime.Times = 1;
                                                dic.Add(gradeComp, newGradeTime);
                                            }
                                            else
                                            {
                                                dic[gradeComp].Grade += grade.Grade;
                                                dic[gradeComp].Times += 1;
                                            }
                                        }
                                        foreach (var item in dic)
                                        {
                                            if (item.Value.Times > 1)
                                            {
                                                item.Value.Grade = item.Value.Grade / item.Value.Times;
                                            }
                                            CapstoneProject.Mark newMark = new CapstoneProject.Mark();
                                            newMark.IsActivated = true;
                                            newMark.IsEnabled = true;
                                            newMark.SemesterId = semesterId;
                                            newMark.StudentId = studentId;
                                            newMark.CourseId = courseId;
                                            //newMark.Comment = student.Comment;
                                            if (item.Value.Grade != null)
                                            {
                                                newMark.AverageMark = item.Value.Grade;
                                            }
                                            else
                                            {
                                                newMark.AverageMark = 0;
                                            }
                                            var subjectMarkComp = context.Subject_MarkComponent.Where(q => q.OldMarkName.Equals(item.Value.GradeComp) && q.SubjectId.Equals(mark.Subject) && q.SyllabusName.Contains("FA") && q.SyllabusName.Contains("17")).FirstOrDefault();
                                            if (subjectMarkComp != null)
                                            {
                                                newMark.SubjectMarkComponentId = subjectMarkComp.Id;
                                                if (context.Marks.Where(q => q.CourseId == courseId && q.StudentId == studentId && q.SubjectMarkComponentId == subjectMarkComp.Id).FirstOrDefault() == null)
                                                {
                                                    context.Marks.Add(newMark);
                                                }
                                                else
                                                {
                                                    Console.WriteLine();
                                                }
                                            }
                                            else
                                            {
                                                Debug.WriteLine("Sub_Comp:" + mark.Subject + "_" + item.Value.GradeComp);
                                            }

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        return Json(new { error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                                    }
                                }

                                context.SaveChanges();
                            }


                        }
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                }

            }
            return null;
        }

        public ActionResult UploadFinal()
        {
            try
            {
                if (Request.Files.Count > 0)
                {
                    using (var context = new CapstoneProjectEntities())
                    {
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
                                    var studentCodeCol = 4;
                                    var titleRow = 1;
                                    var firstRecordRow = 2;

                                    for (int i = firstRecordRow; i < totalRow; i++)
                                    {

                                        var semester = ws.Cells[i, 1].Text.ToUpper();
                                        var subjectId = ws.Cells[i, 2].Text.ToUpper();
                                        var course = context.Courses.Where(q => q.Semester.ToUpper().Equals(semester) && q.SubjectCode.ToUpper().Equals(subjectId)).FirstOrDefault();
                                        if(course == null)
                                        {
                                            return null;
                                        }
                                        var studentCode = ws.Cells[i, 4].Text.ToUpper();
                                        var student = context.Students.Where(q => q.RollNumber.ToUpper().Equals(studentCode)).FirstOrDefault();
                                        if(student == null)
                                        {
                                            return null;
                                        }
                                        var markGroup = ws.Cells[i, 5].Text.ToUpper();
                                        var subjectMarkComp = context.Subject_MarkComponent.Where(q => q.FinalComponent == null && q.MarkComponent.Name.ToUpper().Equals(markGroup) && q.SubjectId.ToUpper().Equals(subjectId)).FirstOrDefault();
                                        if (subjectMarkComp == null)
                                        {
                                            Subject_MarkComponent newFinalComp = new Subject_MarkComponent();
                                            newFinalComp.NumberOfTests = 1;
                                            newFinalComp.Name = subjectId + "_" + markGroup;
                                            newFinalComp.IsOngoing = false;
                                            newFinalComp.IsActive = false;
                                            newFinalComp.SyllabusName = subjectId + "_FA2017";          //Syllabus name
                                            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                                            newFinalComp.OldMarkName = textInfo.ToTitleCase(markGroup);
                                            var finalweight = context.Subject_MarkComponent.Where(q => q.FinalComponent == true && q.Name.ToUpper().Contains("FINAL")).FirstOrDefault();
                                            if(finalweight == null)
                                            {
                                                return null;
                                            }
                                            else
                                            {
                                                newFinalComp.PercentWeight = finalweight.PercentWeight;
                                            }
                                            newFinalComp.SubjectId = subjectId;
                                            if (ws.Cells[i, 5].Text.ToUpper().Contains("RESIT"))
                                            {
                                                var checkGroup = context.MarkComponents.Where(q => q.Name.ToUpper().Equals(markGroup)).FirstOrDefault();
                                                if (checkGroup == null)
                                                {
                                                    newFinalComp.MarkComponentId = 195; //Final Exam Resit
                                                }
                                                else
                                                {
                                                    newFinalComp.MarkComponentId = checkGroup.Id;
                                                }
                                            }
                                            else
                                            {
                                                var checkGroup = context.MarkComponents.Where(q => q.Name.ToUpper().Equals(markGroup)).FirstOrDefault();
                                                if (checkGroup == null)
                                                {
                                                    newFinalComp.MarkComponentId = 194; //Final Exam
                                                }
                                                else
                                                {
                                                    newFinalComp.MarkComponentId = checkGroup.Id;
                                                }
                                            }

                                            context.Subject_MarkComponent.Add(newFinalComp);
                                            context.SaveChanges();

                                            subjectMarkComp= context.Subject_MarkComponent.Where(q => q.FinalComponent == null && q.MarkComponent.Name.ToUpper().Equals(markGroup) && q.SubjectId.ToUpper().Equals(subjectId)).FirstOrDefault();
                                        }
                                        if (subjectMarkComp == null)
                                        {
                                            return null;
                                        }

                                        var oldMark = context.Marks.Where(q => q.Subject_MarkComponent.FinalComponent==false && q.SubjectMarkComponentId == subjectMarkComp.Id && q.StudentId == student.Id && q.CourseId == course.Id).FirstOrDefault();
                                        if (oldMark==null)
                                        {
                                            try
                                            {
                                                CapstoneProject.Mark newMark = new CapstoneProject.Mark();
                                                if (ws.Cells[i, 6].Text != null && !ws.Cells[i, 6].Text.ToUpper().Equals("NULL"))
                                                {
                                                    newMark.AverageMark = Double.Parse(ws.Cells[i, 6].Text);
                                                }
                                                newMark.CourseId = course.Id;
                                                newMark.IsActivated = false;
                                                newMark.IsEnabled = false;
                                                newMark.SemesterId = context.RealSemesters.Where(q => q.Semester.ToUpper().Equals(semester)).FirstOrDefault().Id;
                                                newMark.StudentId = student.Id;
                                                newMark.SubjectMarkComponentId = subjectMarkComp.Id;

                                                {
                                                    context.Marks.Add(newMark);
                                                }
                                            }catch(Exception ex)
                                            {
                                                return Json(new { error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                                            }
                                        }
                                        else
                                        {
                                            if (ws.Cells[i, 6].Text != null && !ws.Cells[i, 6].Text.ToUpper().Equals("NULL"))
                                            {
                                                oldMark.AverageMark = Double.Parse(ws.Cells[i, 6].Text);
                                            }
                                            
                                        }
                                    }
                                    context.SaveChanges();
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


        public ActionResult DownloadExcelMark(int semesterId)
        {
            MemoryStream ms = new MemoryStream();

            using (var context = new CapstoneProjectEntities())
            {
                var semester = context.RealSemesters.Find(semesterId);
                var fileName = semester.Semester + " Marks";

                using (ExcelPackage package = new ExcelPackage(ms))
                {
                    #region Excel format
                    ExcelWorksheet ws = package.Workbook.Worksheets.Add("Sheet1");
                    char StartHeaderChar = 'A';
                    int StartHeaderNumber = 1;
                    #region Headers
                    //ws.Cells[0, 0].Style.WrapText = true;
                    //Image img = CaptstoneProject.Properties.Resources.img_logo_fe;
                    //ExcelPicture pic = ws.Drawings.AddPicture("FPTLogo", img);
                    //pic.From.Column = 0;
                    //pic.From.Row = 0;
                    //pic.SetSize(320, 240);
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "No";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentRoll";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentName";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subject";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "ComponentName";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "OldComponentName";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Mark";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Percentage";


                    var EndHeaderChar = --StartHeaderChar;
                    var EndHeaderNumber = StartHeaderNumber;
                    StartHeaderChar = 'A';
                    StartHeaderNumber = 1;
                    #endregion
                    #region Header styling
                    ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                    ":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Font.Bold = true;


                    //StartHeaderNumber++;
                    #endregion
                    #region Set values for available fields
                    var count = 1;
                    var mark = context.Marks.Where(q => q.SemesterId == semesterId && q.Status == null).ToList()
                        .Select(q => new IConvertible[]
                    {
                       q.Student.RollNumber,
                        q.Student.FullName,
                       q.Subject_MarkComponent.SubjectId,
                        q.Subject_MarkComponent.MarkComponent.Name,
                        q.Subject_MarkComponent.OldMarkName,
                       q.AverageMark==null?"0":q.AverageMark.Value.ToString(),
                       q.Subject_MarkComponent.PercentWeight==null?"0":q.Subject_MarkComponent.PercentWeight.Value.ToString(),
                    });
                    foreach (var item in mark)
                    {
                        ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[0];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[1];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[2];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[3];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[4];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[5];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[6];
                        StartHeaderChar = 'A';
                    }
                    fileName += ".xlsx";

                    StartHeaderNumber = 1;
                    ws.Cells.AutoFitColumns();
                    //ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                    //":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    #endregion

                    #endregion

                    package.SaveAs(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return this.File(ms, contentType, fileName);
                }
            }
        }

    }
    public class GradeTimes
    {
        public String GradeComp { get; set; }
        public float? Grade { get; set; }
        public int Times { get; set; }

    }
}