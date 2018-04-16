using FuGradeLib;
using OfficeOpenXml;
using System;
using System.Collections;
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

        public ActionResult ImportFinal()
        {
            return View();
        }

        public ActionResult StudentCourses()
        {
            return View();
        }

        public ActionResult ImportAverageVovinam()
        {
            return View();
        }

        public ActionResult CalculateAverageMark()
        {
            return View();
        }

        public ActionResult ExportAverageMark()
        {
            return View();
        }


        public ActionResult StudentMarkDetail(string rollNumber, int courseId)
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

        public ActionResult LoadSubjectSelectBySemesters(int semesterId)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var semester = context.RealSemesters.Find(semesterId).Semester;
                var subjectList = context.Courses.Where(q => q.Semester.Equals(semester)).Select(q => new SelectorViewModel
                {
                    Value = q.SubjectCode,
                }).ToList();
                return Json(new { result = subjectList, });
            }

        }

        //[AllowCrossSite]
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
                return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
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
                return Json(new
                {
                    sEcho = param.sEcho,
                    iTotalRecords = result.Count,
                    iTotalDisplayRecords = result.Count,
                    aaData = result,
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [AllowCrossSite]
        public JsonResult LoadMarkByStudentAndCourse(JQueryDataTableParamModel param, string rollNumber, int courseId)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var result = context.Marks.Where(q => q.CourseId == courseId && q.Student.RollNumber.Equals(rollNumber) && q.Subject_MarkComponent.FinalComponent == null).AsEnumerable().Select(q => new IConvertible[] {
                    q.Subject_MarkComponent.MarkName,
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
        public JsonResult LoadStudentSelectorCross()
        {
            using (var context = new CapstoneProjectEntities())
            {
                var students = context.Students.AsEnumerable().Select(q => new Student
                {
                    RollNumber = q.RollNumber,
                    FullName = q.FullName,
                }).ToList();
                return Json(new { data = students }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult UploadMarkFiles(int semesterId)
        {
            int a = Request.Files.Count;
            //FileStream fileStream = new FileStream(@"C:\Users\USER\Desktop\SO DIEM FALL 2017\SO DIEM FALL 2017\10T\FA17__10T_VanTTN.fg", FileMode.Open);
            List<IConvertible[]> errorList = new List<IConvertible[]>();
            for (int i = 0; i < a; i++)
            {
                try
                {
                    var context = new CapstoneProjectEntities();

                    var semester = context.RealSemesters.Find(semesterId);
                    var courseList = context.Courses.Where(q => q.Semester.Equals(semester.Semester.ToUpper())).ToList();
                    var markListWithoutAverage = context.Marks.Where(q => !q.Subject_MarkComponent.MarkComponent.Name.Equals("AVERAGE") && q.SemesterId == semesterId).ToList();
                    string extension = System.IO.Path.GetExtension(Request.Files[i].FileName);

                    if (extension.Equals(".fg"))
                    {
                        var gradeFile = (TeacherGrade)new BinaryFormatter
                        {
                            AssemblyFormat = FormatterAssemblyStyle.Simple
                        }.Deserialize(Request.Files[i].InputStream);

                        foreach (var mark in gradeFile.SubjectClassGrades)
                        {
                            //var semesterId = context.RealSemesters.Where(q => q.Semester.Equals(gradeFile.Semester.ToUpper())).FirstOrDefault().Id;

                            var course = courseList.Where(q => q.SubjectCode.Equals(mark.Subject)).FirstOrDefault();
                            if (course == null)
                            {
                                Console.WriteLine();
                                //Course newCourse = new Course();
                                //newCourse.Semester = semester.Semester;
                                //newCourse.SubjectCode = mark.Subject;
                                //context.Courses.Add(newCourse);
                                //context.SaveChanges();
                            }
                            var compList = context.Subject_MarkComponent.Where(q => q.SubjectId.Equals(mark.Subject));
                            var containSem = semester.Semester.Substring(0, 2).ToUpper();
                            var containYear = "";
                            if (semester.Semester.Contains('_'))
                            {
                                containYear = semester.Semester.Substring(semester.Semester.Length - 4, 2).ToUpper();
                            }
                            else
                            {
                                containYear = semester.Semester.Substring(semester.Semester.Length - 2, 2).ToUpper();
                            }

                            var subjectCompList = compList.Where(q => (q.SyllabusName.Contains(containSem) && q.SyllabusName.Contains(containYear))).ToList();
                            List<Subject_MarkComponent> oldsubjectCompList = new List<Subject_MarkComponent>();
                            if (containSem.Equals("SP"))
                            {
                                var lastYear = (int.Parse(containYear) - 1) + "";
                                oldsubjectCompList = compList.Where(q => (q.SyllabusName.Contains("FA") && q.SyllabusName.Contains(lastYear))).ToList();
                            }
                            if (containSem.Equals("SU"))
                            {
                                oldsubjectCompList = compList.Where(q => (q.SyllabusName.Contains("SP") && q.SyllabusName.Contains(containYear))).ToList();
                            }
                            if (containSem.Equals("FA"))
                            {
                                oldsubjectCompList = compList.Where(q => (q.SyllabusName.Contains("SU") && q.SyllabusName.Contains(containYear))).ToList();
                            }
                            if (subjectCompList == null
                            && oldsubjectCompList == null)
                            {
                                IConvertible[] item = new IConvertible[] { gradeFile.Login, mark.Class, mark.Subject, "Sai syllabus, xin nhập bằng excel hoặc sửa lại file FG đúng syllabus. Các lớp khác nhập thành công." };
                                errorList.Add(item);
                                continue;
                            }
                            if (subjectCompList == null && oldsubjectCompList != null)
                            {
                                subjectCompList = oldsubjectCompList;
                            }
                            var subCompDic = subjectCompList.ToDictionary(q => q.MarkName.Trim());
                            bool skip = false;
                            //var oldSubCompDic = oldsubjectCompList.ToDictionary(q => q.MarkName.Trim());

                            foreach (var item in mark.Components)
                            {
                                if (!item.ToUpper().Equals("STATUS"))
                                {
                                    if (!subCompDic.ContainsKey(item.Trim()))
                                    {
                                        //foreach (var item2 in mark.Components)
                                        //{
                                        //    if (!oldSubCompDic.ContainsKey(item2.Trim()))
                                        //    {
                                        skip = true;
                                        break;
                                        //    }
                                        //}
                                        //if (skip == true)
                                        //{
                                        //    break;
                                        //}
                                        //subCompDic = oldSubCompDic;
                                        //subjectCompList = oldsubjectCompList;
                                    }
                                }
                            }
                            if (skip == true)
                            {
                                IConvertible[] item = new IConvertible[] { gradeFile.Login, mark.Class, mark.Subject, "Sai syllabus, xin nhập bằng excel hoặc sửa lại file FG đúng syllabus. Các lớp khác nhập thành công." };
                                errorList.Add(item);
                                continue;
                            }



                            foreach (var student in mark.Students)
                            {
                                var context2 = new CapstoneProjectEntities();
                                try
                                {
                                    var studentEntity = context2.Students.Where(q => q.RollNumber.ToUpper().Equals(student.Roll.ToUpper())).FirstOrDefault();
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
                                        context2.Students.Add(newStu);
                                        context2.SaveChanges();
                                        studentEntity = context2.Students.Where(q => q.RollNumber.ToUpper().Equals(student.Roll.ToUpper())).FirstOrDefault();
                                    }
                                    //Hop thanh 1 Component. ex: Tinh ra Quiz tu quiz 1 va quiz 2 

                                    //Dictionary<String, GradeTimes> dic = new Dictionary<string, GradeTimes>();
                                    //foreach (var grade in student.Grades)
                                    //{
                                    ////string gradeComp = new String(grade.Component.Where(c => (c < '0' || c > '9')).ToArray());
                                    //if (!grade.Component.ToUpper().Equals("STATUS"))
                                    //{
                                    //    string gradeComp = grade.Component;
                                    //    if (!dic.ContainsKey(gradeComp))
                                    //    {
                                    //        GradeTimes newGradeTime = new GradeTimes();
                                    //        newGradeTime.Grade = grade.Grade;
                                    //        //newGradeTime.GradeComp = new String(grade.Component.Where(c => (c < '0' || c > '9')).ToArray());
                                    //        newGradeTime.GradeComp = grade.Component;
                                    //        newGradeTime.Times = 1;
                                    //        dic.Add(gradeComp, newGradeTime);
                                    //    }
                                    //    else
                                    //    {
                                    //        dic[gradeComp].Grade += grade.Grade;
                                    //        dic[gradeComp].Times += 1;
                                    //    }
                                    //}
                                    //}
                                    foreach (var grade in student.Grades)
                                    {
                                        //if (item.Value.Times > 1)
                                        //{
                                        //    item.Value.Grade = item.Value.Grade / item.Value.Times;
                                        //}
                                        CapstoneProject.Mark newMark = new CapstoneProject.Mark();
                                        newMark.IsActivated = true;
                                        newMark.IsEnabled = true;
                                        newMark.SemesterId = semesterId;
                                        newMark.StudentId = studentId;
                                        newMark.CourseId = course.Id;
                                        //newMark.Comment = student.Comment;
                                        if (grade.Grade != null)
                                        {
                                            newMark.AverageMark = grade.Grade;
                                        }
                                        else
                                        {
                                            newMark.AverageMark = 0;
                                        }
                                        //import FALL2017 mark (FA AND 17)

                                        var subjectMarkComp = subjectCompList.Where(q => q.MarkName.Equals(grade.Component)).FirstOrDefault();

                                        if (subjectMarkComp != null)
                                        {
                                            newMark.SubjectMarkComponentId = subjectMarkComp.Id;
                                            if (markListWithoutAverage.Where(q => q.CourseId == course.Id && q.StudentId == studentId && q.SubjectMarkComponentId == subjectMarkComp.Id).FirstOrDefault() == null)
                                            {
                                                context2.Marks.Add(newMark);
                                                context2.SaveChanges();
                                                //GC.Collect();
                                                context2.Dispose();
                                                context2 = new CapstoneProjectEntities();
                                            }
                                            else
                                            {
                                                var oldMark = markListWithoutAverage.Where(q => q.CourseId == course.Id && q.StudentId == studentId && q.SubjectMarkComponentId == subjectMarkComp.Id).FirstOrDefault();
                                                Console.WriteLine();
                                                oldMark.AverageMark = newMark.AverageMark;
                                                context2.SaveChanges();
                                                //GC.Collect();
                                                context2.Dispose();
                                                context2 = new CapstoneProjectEntities();
                                            }
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Sub_Comp:" + mark.Subject + "_" + grade.Component);
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                                }
                            }




                            //context.Dispose();
                        }
                        //GC.Collect();
                        context.SaveChanges();


                    }

                }
                catch (Exception ex)
                {
                    return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                }

            }
            if (errorList.Count == 0)
            {
                return Json(new { success = true, message = "Successful!" });
            }
            else
            {
                return Json(new { success = true, message = "Some classes doesn't match the syllabus", errorList = errorList });
            }
        }

        
        public ActionResult UploadFinal(int semesterId, int isResit)
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
                                var semester = context.RealSemesters.Find(semesterId);
                                var subjectCode = fileContent.FileName.Substring(0, 6).ToUpper();
                                var course = context.Courses.Where(q => q.Semester.ToUpper().Equals(semester.Semester.ToUpper()) && q.SubjectCode.ToUpper().Equals(subjectCode)).FirstOrDefault();
                                var markList = context.Marks.Where(q => q.CourseId == course.Id).ToList();
                                var studentList = markList.GroupBy(q => q.Student).Select(q => q.Key).ToList();
                                Dictionary<String, Subject_MarkComponent> finalList = new Dictionary<string, Subject_MarkComponent>();
                                Dictionary<int, Subject_MarkComponent> finalCols = new Dictionary<int, Subject_MarkComponent>();
                                var markGroup = context.Marks.Where(q => q.CourseId == course.Id).GroupBy(q => q.Subject_MarkComponent).Select(q => q.Key).ToList();
                                foreach (var item in markGroup)
                                {
                                    if (item.MarkName.ToUpper().Contains("RESIT"))
                                    {
                                        finalList.Add(item.MarkName.ToUpper().Replace(" RESIT", ""), markGroup.Where(q=>q.MarkName.ToUpper().Equals(item.MarkName.ToUpper().Replace(" RESIT", ""))).FirstOrDefault());
                                    }
                                }
                                using (ExcelPackage package = new ExcelPackage(stream))
                                {
                                    var ws = package.Workbook.Worksheets.First();
                                    var totalCol = ws.Dimension.Columns;
                                    var totalRow = ws.Dimension.Rows;
                                    var studentCodeCol = 2;
                                    List<int> markCol = new List<int>();
                                    for (int i = 3; i <= totalCol - 1; i++)
                                    {
                                        markCol.Add(i);
                                    }
                                    var titleRow = 1;
                                    var firstRecordRow = 2;
                                    var skip = false;
                                    if (markCol.Count() != finalList.Count())
                                    {
                                        return Json(new { success = false, message = "Wrong syllabus. Please recheck" });

                                    }
                                    if (isResit == 0)
                                    {
                                        if (markCol.Count > 1)
                                        {
                                            foreach (var col in markCol)
                                            {
                                                if (!finalList.ContainsKey(ws.Cells[titleRow, col].Text.ToUpper()))
                                                {
                                                    skip = true;
                                                    break;
                                                }
                                                finalCols.Add(col, finalList[ws.Cells[titleRow, col].Text.ToUpper()]);
                                            }
                                        }
                                        else
                                        {
                                            finalCols.Add(3, finalList.FirstOrDefault().Value);
                                        }
                                    }
                                    else
                                    {
                                        if (markCol.Count > 1)
                                        {
                                            foreach (var col in markCol)
                                            {
                                                if (!finalList.ContainsKey(ws.Cells[titleRow, col].Text.ToUpper().Replace(" RESIT", "")))
                                                {
                                                    skip = true;
                                                    break;
                                                }
                                                finalCols.Add(col, finalList[ws.Cells[titleRow, col].Text.ToUpper().Replace(" RESIT", "")]);
                                            }
                                        }
                                        else
                                        {
                                            finalCols.Add(3, finalList.FirstOrDefault().Value);
                                        }
                                    }

                                    if (skip == true)
                                    {
                                        return Json(new { success = false, message = "Wrong syllabus. Please recheck" });
                                    }


                                    for (int i = firstRecordRow; i <= totalRow; i++)
                                    {
                                        var student = studentList.Where(q => q.RollNumber.ToUpper().Equals(ws.Cells[i, studentCodeCol].Text.ToUpper().Trim())).FirstOrDefault();
                                        if (student == null)
                                        {
                                            continue;
                                        }
                                        if (isResit == 0)
                                        {
                                            foreach (var finalCol in finalCols)
                                            {
                                                var oldFinalMark = markList.Where(q => q.StudentId == student.Id && q.SubjectMarkComponentId == finalCol.Value.Id).FirstOrDefault();
                                                if (oldFinalMark == null)
                                                {
                                                    //CapstoneProject.Mark finalMark = new CapstoneProject.Mark();
                                                    //var mark = ws.Cells[titleRow, finalCol.Key].Text;
                                                    //if (mark == "" || mark == null)
                                                    //{
                                                    //    finalMark.AverageMark = 0;
                                                    //}
                                                    //else
                                                    //{
                                                    //    finalMark.AverageMark = Double.Parse(mark);
                                                    //}
                                                    //finalMark.CourseId = course.Id;
                                                    //finalMark.SemesterId = semesterId;
                                                    //finalMark.StudentId = student.Id;
                                                    //finalMark.SubjectMarkComponentId = finalCol.Value.Id;
                                                    //finalMark.IsActivated = true;
                                                    //finalMark.IsEnabled = true;
                                                    //context.Marks.Add(finalMark);
                                                    //var resitMark = markList.Where(q => q.StudentId == student.Id && q.Subject_MarkComponent.MarkName.ToUpper().Trim().Equals((finalCol.Value.MarkName.Trim() + " RESIT").ToUpper())).FirstOrDefault();
                                                    //if (resitMark != null)
                                                    //{
                                                    //    resitMark.AverageMark = null;
                                                    //}
                                                }
                                                else
                                                {
                                                    if (i == 58)
                                                    {
                                                        Console.WriteLine();
                                                    }
                                                    var mark = ws.Cells[i, finalCol.Key].Text;
                                                    if (mark == "" || mark == null ||mark.Equals("#REF!"))
                                                    {
                                                        oldFinalMark.AverageMark = 0;
                                                    }
                                                    else
                                                    {
                                                        oldFinalMark.AverageMark = Double.Parse(mark);
                                                    }
                                                    var resitMark = markList.Where(q => q.StudentId == student.Id && q.Subject_MarkComponent.MarkName.Trim().ToUpper().Equals((finalCol.Value.MarkName.Trim() + " RESIT").ToUpper())).FirstOrDefault();
                                                    if (resitMark != null)
                                                    {
                                                        resitMark.AverageMark = null;
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            foreach (var finalCol in finalCols)
                                            {
                                                var oldResitMark = markList.Where(q => q.StudentId == student.Id && q.Subject_MarkComponent.MarkName.ToUpper().Trim().Equals((finalCol.Value.MarkName.Trim() + " RESIT").ToUpper())).FirstOrDefault();
                                                if (oldResitMark == null)
                                                {
                                                    //CapstoneProject.Mark finalMark = new CapstoneProject.Mark();
                                                    //var mark = ws.Cells[titleRow, finalCol.Key].Text;
                                                    //if (mark == "" || mark == null)
                                                    //{
                                                    //    finalMark.AverageMark = 0;
                                                    //}
                                                    //else
                                                    //{
                                                    //    finalMark.AverageMark = Double.Parse(mark);
                                                    //}
                                                    //finalMark.CourseId = course.Id;
                                                    //finalMark.SemesterId = semesterId;
                                                    //finalMark.StudentId = student.Id;
                                                    //var resitSubjectMarkComp = markList.Where(q => q.CourseId == course.Id && q.Subject_MarkComponent.MarkName.ToUpper().Trim().Equals((finalCol.Value.MarkName.Trim() + " RESIT").ToUpper())).FirstOrDefault();
                                                    //finalMark.SubjectMarkComponentId = resitSubjectMarkComp.SubjectMarkComponentId;
                                                    //finalMark.IsActivated = true;
                                                    //finalMark.IsEnabled = true;
                                                    //context.Marks.Add(finalMark);

                                                }
                                                else
                                                {
                                                    var mark = ws.Cells[i, finalCol.Key].Text;
                                                    if (mark == "" || mark == null)
                                                    {
                                                        oldResitMark.AverageMark = 0;
                                                    }
                                                    else
                                                    {
                                                        oldResitMark.AverageMark = Double.Parse(mark);
                                                    }

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
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
            }
            return Json(new { success = true, message = "Successful!" });
        }

        public ActionResult UploadMarkExcel(int semesterId)
        {
            try
            {
                if (Request.Files.Count > 0)
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
                                var reset = 0;

                                using (var context = new CapstoneProjectEntities())
                                {
                                    var semester = context.RealSemesters.Find(semesterId);
                                    var courseInSemester = context.Courses.AsNoTracking().Where(q => q.Semester.ToUpper().Equals(semester.Semester)).ToList();
                                    var getAllStudents = context.Students.ToList();
                                    var subMarkList = context.Subject_MarkComponent.Where(q => !q.MarkComponent.Name.ToUpper().Equals("AVERAGE")).ToList();
                                    var markList = context.Marks.Where(q => q.SemesterId == semesterId).ToList();
                                    context.Configuration.AutoDetectChangesEnabled = false;
                                    try
                                    {
                                        for (int i = firstRecordRow; i <= totalRow; i++)
                                        {
                                            reset++;

                                            //var semester = ws.Cells[i, 1].Text.ToUpper();
                                            var subjectId = ws.Cells[i, 2].Text.ToUpper();
                                            var course = courseInSemester.Where(q => q.SubjectCode.ToUpper().Equals(subjectId)).FirstOrDefault();
                                            if (course == null)
                                            {
                                                return null;
                                            }
                                            var studentCode = ws.Cells[i, 4].Text.ToUpper();
                                            var student = getAllStudents.Where(q => q.RollNumber.ToUpper().Equals(studentCode)).FirstOrDefault();
                                            if (student == null)
                                            {
                                                return null;
                                            }
                                            var markGroup = ws.Cells[i, 5].Text.Trim().ToUpper();
                                            var subjectMarkComp = subMarkList.Where(q => q.MarkName.ToUpper().Equals(markGroup) && q.SubjectId.ToUpper().Equals(subjectId)).FirstOrDefault();
                                            if (subjectMarkComp == null)
                                            {
                                                Console.WriteLine();
                                            }
                                            var oldMark = markList.Where(q => q.SubjectMarkComponentId == subjectMarkComp.Id && q.StudentId == student.Id && q.CourseId == course.Id).FirstOrDefault();
                                            if (oldMark == null)
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
                                                    newMark.SemesterId = semesterId;
                                                    newMark.StudentId = student.Id;
                                                    newMark.SubjectMarkComponentId = subjectMarkComp.Id;


                                                    context.Marks.Add(newMark);

                                                }
                                                catch (Exception ex)
                                                {
                                                    return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                                                }
                                            }
                                            else
                                            {
                                                if (ws.Cells[i, 6].Text != null && !ws.Cells[i, 6].Text.ToUpper().Equals("NULL"))
                                                {
                                                    oldMark.AverageMark = Double.Parse(ws.Cells[i, 6].Text);
                                                }

                                            }

                                            if (reset == 1000)
                                            {
                                                context.SaveChanges();
                                                reset = 0;
                                            }
                                            if (i == totalRow)
                                            {
                                                context.SaveChanges();

                                            }
                                        }
                                    }
                                    finally
                                    {
                                        context.Configuration.AutoDetectChangesEnabled = true;
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
                return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
            }
            return Json(new { success = true, message = "Successful!" });
        }

        public ActionResult ImportVovinamAverage(int semesterId)
        {
            try
            {
                if (Request.Files.Count > 0)
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
                                var subjectCol = 2;
                                var titleRow = 1;
                                var markCol = 5;
                                var isPassed = 7;
                                var firstRecordRow = 2;

                                using (var context = new CapstoneProjectEntities())
                                {
                                    var semester = context.RealSemesters.Find(semesterId);
                                    var courseInSemester = context.Courses.AsNoTracking().Where(q => q.Semester.ToUpper().Equals(semester.Semester)).ToList();
                                    var getAllStudents = context.Students.ToList();
                                    var averageMarkComp = context.Subject_MarkComponent.Where(q => q.MarkComponent.Name.ToUpper().Equals("AVERAGE")).ToList();
                                    var markList = context.Marks.Where(q => q.SemesterId == semesterId && q.Subject_MarkComponent.MarkComponent.Name.ToUpper().Equals("AVERAGE")).ToList();
                                    context.Configuration.AutoDetectChangesEnabled = false;
                                    try
                                    {
                                        for (int i = firstRecordRow; i <= totalRow; i++)
                                        {
                                            using (var context2 = new CapstoneProjectEntities())
                                            {
                                                //var semester = ws.Cells[i, 1].Text.ToUpper();
                                                var subjectId = ws.Cells[i, subjectCol].Text.ToUpper();
                                                var course = courseInSemester.Where(q => q.SubjectCode.ToUpper().Equals(subjectId)).FirstOrDefault();
                                                if (course == null)
                                                {
                                                    return null;
                                                }
                                                var studentCode = ws.Cells[i, studentCodeCol].Text.ToUpper();
                                                var student = getAllStudents.Where(q => q.RollNumber.ToUpper().Equals(studentCode)).FirstOrDefault();
                                                if (student == null)
                                                {
                                                    //Student stu = new Student();
                                                    //stu.RollNumber = studentCode;
                                                    //context2.Students.Add(stu);
                                                    //context2.SaveChanges();
                                                    //student = context2.Students.Where(q => q.RollNumber.ToUpper().Equals(studentCode)).FirstOrDefault();
                                                }

                                                var subjectMarkComp = averageMarkComp.Where(q => q.SubjectId.ToUpper().Equals(subjectId)).FirstOrDefault();
                                                if (subjectMarkComp == null)
                                                {
                                                    Console.WriteLine();
                                                }
                                                var oldMark = markList.Where(q => q.SubjectMarkComponentId == subjectMarkComp.Id && q.StudentId == student.Id && q.CourseId == course.Id).FirstOrDefault();
                                                if (oldMark == null)
                                                {
                                                    try
                                                    {
                                                        CapstoneProject.Mark newMark = new CapstoneProject.Mark();
                                                        if (ws.Cells[i, markCol].Text != null && !ws.Cells[i, markCol].Text.ToUpper().Equals("NULL"))
                                                        {
                                                            newMark.AverageMark = Double.Parse(ws.Cells[i, markCol].Text);
                                                        }
                                                        newMark.CourseId = course.Id;
                                                        newMark.IsActivated = false;
                                                        newMark.IsEnabled = false;
                                                        newMark.SemesterId = semesterId;
                                                        newMark.StudentId = student.Id;
                                                        newMark.SubjectMarkComponentId = subjectMarkComp.Id;
                                                        newMark.Status = null;
                                                        if (ws.Cells[i, isPassed].Text.Equals("1"))
                                                        {
                                                            newMark.Status = "Passed";
                                                        }
                                                        if (ws.Cells[i, isPassed].Text.Equals("0"))
                                                        {
                                                            newMark.Status = "Fail";
                                                        }

                                                        context2.Marks.Add(newMark);

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
                                                    }
                                                }
                                                else
                                                {
                                                    if (ws.Cells[i, markCol].Text != null && !ws.Cells[i, markCol].Text.ToUpper().Equals("NULL"))
                                                    {
                                                        oldMark.AverageMark = Double.Parse(ws.Cells[i, markCol].Text);
                                                    }

                                                }
                                                context2.SaveChanges();

                                            }
                                            context.SaveChanges();
                                        }
                                    }
                                    finally
                                    {
                                        context.Configuration.AutoDetectChangesEnabled = true;
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
                return Json(new { success = false, error = ex.Message, message = "Errors in uploaded file. Please recheck" });
            }
            return Json(new { success = true, message = "Successful!" });
        }

        //Chua sua lai, DO NOT USE
        public ActionResult CalculateAverageMarkBySemester(int semesterId)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var markList = context.Marks.Where(q => q.SemesterId == semesterId && !q.Subject_MarkComponent.MarkComponent.Name.Equals("AVERAGE")).ToList();
                var studentList = markList.GroupBy(q => q.Student).Select(q => q.Key).ToList();
                var subjectAverageComp = context.Subject_MarkComponent.Where(q => q.MarkComponent.Name.ToUpper().Equals("AVERAGE")).ToList();
                var capstoneSubjects = context.Subjects.Where(q => q.Type == 2).ToDictionary(q => q.Id);
                var averageList = context.Marks.Where(q => q.SemesterId == semesterId && q.Subject_MarkComponent.MarkComponent.Name.Equals("AVERAGE")).ToList();
                foreach (var student in studentList)
                {
                    var retake = false;
                    var studentMarks = markList.Where(q => q.StudentId == student.Id).ToList();
                    var courseList = studentMarks.GroupBy(q => q.Course).Select(q => q.Key).ToList();
                    foreach (var course in courseList)
                    {
                        var averageCompId = subjectAverageComp.Where(q => q.SubjectId.ToUpper().Equals(course.SubjectCode.ToUpper())).FirstOrDefault().Id;
                        double? average = 0;
                        Dictionary<String, String> finalList = new Dictionary<string, string>();
                        var passFinalCondition = false;
                        using (var context2 = new CapstoneProjectEntities())
                        {

                            var groupMark = new Dictionary<String, MarkGroupModel>();
                            var marks = studentMarks.Where(q => q.CourseId == course.Id).ToList();
                            foreach (var item in marks)
                            {
                                var markgroupComp = item.Subject_MarkComponent.MarkComponent;
                                if (!groupMark.ContainsKey(markgroupComp.Name))
                                {
                                    MarkGroupModel mgm = new MarkGroupModel();
                                    mgm.MarkGroupName = markgroupComp.Name;
                                    mgm.Mark = item.AverageMark;
                                    mgm.Weight = item.Subject_MarkComponent.PercentWeight;
                                    mgm.NumberOfTest = item.Subject_MarkComponent.NumberOfTests;
                                    groupMark.Add(markgroupComp.Name, mgm);
                                }
                                else
                                {
                                    groupMark[markgroupComp.Name].Weight += item.Subject_MarkComponent.PercentWeight;
                                    if (item.AverageMark != null)
                                    {
                                        groupMark[markgroupComp.Name].Mark += item.AverageMark;
                                    }
                                }
                            }
                            foreach (var group in groupMark)
                            {
                                group.Value.Mark = group.Value.Mark / group.Value.NumberOfTest;
                            }
                            var markGroup = groupMark.Values.ToDictionary(q => q.MarkGroupName);
                            foreach (var item in markGroup)
                            {
                                if (item.Key.ToUpper().Contains("RESIT"))
                                {
                                    finalList.Add(item.Key.ToUpper().Replace("RESIT", "").Trim(), item.Key.ToUpper().Replace("RESIT", "").Trim());
                                    if (item.Value.Mark != null)
                                    {
                                        retake = true;
                                    }
                                }
                            }
                            if (retake != true)
                            {
                                foreach (var item in markGroup)
                                {
                                    if (!item.Key.ToUpper().Contains("RESIT"))
                                    {
                                        if (item.Value.Mark != null)
                                        {
                                            average += item.Value.Mark * item.Value.Weight;
                                        }
                                        else
                                        {
                                            average += 0;
                                        }
                                    }
                                }
                                if (!capstoneSubjects.ContainsKey(course.SubjectCode)) //If not a capstone subject
                                {
                                    if (finalList.Count == 1)
                                    {
                                        if (markGroup[finalList.Last().Key].Mark >= 4)
                                        {
                                            passFinalCondition = true;
                                        }
                                    }
                                    else
                                    {
                                        double? averageFinal = 0;
                                        foreach (var item in finalList)
                                        {
                                            averageFinal += markGroup[item.Key].Mark * markGroup[item.Key].Weight;
                                        }
                                        if (averageFinal / 100 >= 4)
                                        {
                                            passFinalCondition = true;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var item in groupMark)
                                    {
                                        if (item.Key.ToUpper().Contains("FINAL"))
                                        {
                                            if (item.Value.Mark >= 4)
                                            {
                                                passFinalCondition = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var item in markGroup)
                                {
                                    if (!finalList.ContainsKey(item.Key.ToUpper()))
                                    {
                                        if (item.Value.Mark != null)
                                        {
                                            average += item.Value.Mark * item.Value.Weight;
                                        }
                                        else
                                        {
                                            average += 0;
                                        }
                                    }
                                }
                                if (!capstoneSubjects.ContainsKey(course.SubjectCode)) //If not a capstone subject
                                {
                                    if (finalList.Count == 1)
                                    {
                                        if (markGroup[finalList.Last().Key].Mark >= 4)
                                        {
                                            passFinalCondition = true;
                                        }
                                    }
                                    else
                                    {
                                        double? averageFinal = 0;
                                        foreach (var item in finalList)
                                        {
                                            averageFinal += markGroup[item.Key].Mark * markGroup[item.Key].Weight;
                                        }
                                        if (averageFinal / 100 >= 4)
                                        {
                                            passFinalCondition = true;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var item in groupMark)
                                    {
                                        if (item.Key.ToUpper().Contains("FINAL"))
                                        {
                                            if (item.Value.Mark >= 4)
                                            {
                                                passFinalCondition = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (average == null)
                            {
                                average = 0;
                            }
                            var tempAverage = Math.Round(average.Value, 0, MidpointRounding.AwayFromZero) / 100;
                            var result = Math.Round(tempAverage, 1, MidpointRounding.AwayFromZero);
                            //var oldAverageMark = averageList.Where(q => q.CourseId == course.Id && q.StudentId == student.Id && q.SubjectMarkComponentId == averageCompId).FirstOrDefault();
                            //if (oldAverageMark == null)
                            //{
                            CapstoneProject.Mark averageMark = new CapstoneProject.Mark();
                            averageMark.AverageMark = result;
                            averageMark.CourseId = course.Id;
                            averageMark.StudentId = student.Id;
                            averageMark.SubjectMarkComponentId = averageCompId;
                            averageMark.SemesterId = semesterId;
                            averageMark.Status = "Fail";
                            averageMark.IsActivated = true;
                            averageMark.IsEnabled = true;
                            if (result >= 5 && passFinalCondition == true)
                            {
                                averageMark.Status = "Passed";
                            }
                            context2.Marks.Add(averageMark);
                            //}
                            //else
                            //{
                            //    if (result >= 5 && passFinalCondition == true)
                            //    {
                            //        oldAverageMark.Status = "Passed";
                            //    }
                            //    else
                            //    {
                            //        oldAverageMark.Status = "Fail";
                            //    }
                            //    oldAverageMark.AverageMark = result;
                            //    oldAverageMark.IsActivated = true;
                            //    oldAverageMark.IsEnabled = true;
                            //}

                            context2.SaveChanges();
                        }
                    }
                }
            }
            return Json(new { success = true, message = "Successful!" });
        }

        public ActionResult CalculateAverageMarkBySemesterAndSubject(int semesterId, string subjectCode)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var semester = context.RealSemesters.Find(semesterId);
                var course = context.Courses.Where(q => q.Semester.ToUpper().Equals(semester.Semester.ToUpper()) && q.SubjectCode.ToUpper().Equals(subjectCode.ToUpper())).FirstOrDefault();
                var markList = context.Marks.Where(q => q.Course.Id == course.Id && !q.Subject_MarkComponent.MarkComponent.Name.Equals("AVERAGE")).ToList();
                var studentList = markList.GroupBy(q => q.Student).Select(q => q.Key).ToList();
                var subjectAverageComp = context.Subject_MarkComponent.Where(q => q.MarkComponent.Name.ToUpper().Equals("AVERAGE")).ToList();
                var capstoneSubjects = context.Subjects.Where(q => q.Type == 2).ToDictionary(q => q.Id);
                var averageList = context.Marks.Where(q => q.SemesterId == semesterId && q.Subject_MarkComponent.MarkComponent.Name.Equals("AVERAGE")).ToList();
                foreach (var student in studentList)
                {
                    var retake = false;
                    var studentMarks = markList.Where(q => q.StudentId == student.Id).ToList();
                    var averageComp = subjectAverageComp.Where(q => q.SubjectId.ToUpper().Equals(course.SubjectCode.ToUpper())).FirstOrDefault();
                    int averageCompId =0;
                    if (averageComp == null)
                    {
                        Subject_MarkComponent smc = new Subject_MarkComponent();
                        smc.MarkComponentId = context.MarkComponents.Where(q => q.Name.ToUpper().Equals("AVERAGE")).FirstOrDefault().Id;
                        smc.SubjectId = subjectCode.ToUpper();
                        smc.Name = subjectCode + "_AVERAGE";
                        smc.PercentWeight = 0;
                        context.Subject_MarkComponent.Add(smc);
                        context.SaveChanges();
                        subjectAverageComp = context.Subject_MarkComponent.Where(q => q.MarkComponent.Name.ToUpper().Equals("AVERAGE")).ToList();
                        averageComp = subjectAverageComp.Where(q => q.SubjectId.ToUpper().Equals(course.SubjectCode.ToUpper())).FirstOrDefault();
                        averageCompId = averageComp.Id;
                    }
                    else
                    {
                        averageCompId = averageComp.Id;
                    }
                    double? average = 0;
                    Dictionary<String, String> finalList = new Dictionary<string, string>();
                    var passFinalCondition = false;
                    using (var context2 = new CapstoneProjectEntities())
                    {

                        var groupMark = new Dictionary<String, MarkGroupModel>();
                        var marks = studentMarks.Where(q => q.CourseId == course.Id).ToList();

                       
                        foreach (var item in marks)
                        {
                            if (item.Subject_MarkComponent.MarkName.ToUpper().Contains("RESIT"))
                            {
                                finalList.Add(item.Subject_MarkComponent.MarkName.ToUpper().Replace("RESIT", "").Trim(), item.Subject_MarkComponent.MarkName.ToUpper().Replace("RESIT", "").Trim());
                            }
                        }
                        //Tinh cac thanh phan diem (co the kiem tra >0)
                        foreach (var item in marks)
                        {
                            var markgroupComp = item.Subject_MarkComponent.MarkComponent;
                            if (!groupMark.ContainsKey(markgroupComp.Name))
                            {
                                MarkGroupModel mgm = new MarkGroupModel();
                                mgm.MarkGroupName = markgroupComp.Name;
                                mgm.Mark = item.AverageMark;
                                mgm.Weight = item.Subject_MarkComponent.PercentWeight;
                                mgm.NumberOfTest = item.Subject_MarkComponent.NumberOfTests;
                                groupMark.Add(markgroupComp.Name, mgm);
                            }
                            else
                            {
                                groupMark[markgroupComp.Name].Weight += item.Subject_MarkComponent.PercentWeight;
                                if (item.AverageMark != null)
                                {
                                    groupMark[markgroupComp.Name].Mark += item.AverageMark;
                                }
                            }
                        }
                        foreach (var group in groupMark)
                        {
                            if (!finalList.ContainsKey(group.Key))
                            {
                                group.Value.Mark = group.Value.Mark / group.Value.NumberOfTest;
                            }
                        }
                        //Tinh xong cac thanh phan

                        var markGroup = groupMark.Values.ToDictionary(q => q.MarkGroupName);
                        foreach (var item in markGroup)
                        {
                            if (item.Key.ToUpper().Contains("RESIT"))
                            {
                                /*finalList.Add(item.Key.ToUpper().Replace("RESIT", "").Trim(), item.Key.ToUpper().Replace("RESIT", "").Trim());*/
                                if (item.Value.Mark != null)
                                {
                                    retake = true;
                                }
                            }
                        }
                        if (retake != true)
                        {
                            foreach (var item in markGroup)
                            {
                                if (!item.Key.ToUpper().Contains("RESIT"))
                                {
                                    if (item.Value.Mark != null)
                                    {
                                        average += item.Value.Mark * item.Value.Weight;
                                    }
                                    else
                                    {
                                        average += 0;
                                    }
                                }
                            }
                            if (!capstoneSubjects.ContainsKey(course.SubjectCode)) //If not a capstone subject
                            {
                                if (finalList.Count == 0)
                                {
                                    passFinalCondition = true;
                                }
                                if (finalList.Count == 1)
                                {
                                    if (markGroup[finalList.Last().Key].Mark >= 4)
                                    {
                                        passFinalCondition = true;
                                    }
                                }
                                if (finalList.Count > 1)
                                {
                                    if(student.Id== 68221)
                                    {
                                        Console.WriteLine();
                                    }
                                    double? averageFinal = 0;
                                    double? finalWeight = 0;
                                    foreach (var item in finalList)
                                    {
                                        averageFinal += markGroup[item.Key].Mark * markGroup[item.Key].Weight;
                                        finalWeight += markGroup[item.Key].Weight;
                                    }
                                    if (averageFinal / finalWeight >= 4)
                                    {
                                        passFinalCondition = true;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var item in groupMark)
                                {
                                    if (item.Key.ToUpper().Contains("FINAL"))
                                    {
                                        if (item.Value.Mark >= 4)
                                        {
                                            passFinalCondition = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in markGroup)
                            {
                                if (!finalList.ContainsKey(item.Key.ToUpper()))
                                {
                                    if (item.Value.Mark != null)
                                    {
                                        average += item.Value.Mark * item.Value.Weight;
                                    }
                                    else
                                    {
                                        average += 0;
                                    }
                                }
                            }
                            if (!capstoneSubjects.ContainsKey(course.SubjectCode)) //If not a capstone subject
                            {
                                if (finalList.Count == 0)
                                {
                                    passFinalCondition = true;
                                }
                                if (finalList.Count == 1)
                                {
                                    if (markGroup[finalList.Last().Key].Mark >= 4)
                                    {
                                        passFinalCondition = true;
                                    }
                                }
                                if (finalList.Count > 1)
                                {
                                    double? averageFinal = 0;
                                    foreach (var item in finalList)
                                    {
                                        averageFinal += markGroup[item.Key].Mark * markGroup[item.Key].Weight;
                                    }
                                    if (averageFinal / 100 >= 4)
                                    {
                                        passFinalCondition = true;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var item in groupMark)
                                {
                                    if (item.Key.ToUpper().Contains("FINAL"))
                                    {
                                        if (item.Value.Mark >= 4)
                                        {
                                            passFinalCondition = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (average == null)
                        {
                            average = 0;
                        }
                        var tempAverage = Math.Round(average.Value, 0, MidpointRounding.AwayFromZero) / 100;
                        var result = Math.Round(tempAverage, 1, MidpointRounding.AwayFromZero);
                        var oldAverageMark = averageList.Where(q => q.CourseId == course.Id && q.StudentId == student.Id && q.SubjectMarkComponentId == averageCompId).FirstOrDefault();
                        if (oldAverageMark == null)
                        {
                            CapstoneProject.Mark averageMark = new CapstoneProject.Mark();
                        averageMark.AverageMark = result;
                        averageMark.CourseId = course.Id;
                        averageMark.StudentId = student.Id;
                        averageMark.SubjectMarkComponentId = averageCompId;
                        averageMark.SemesterId = semesterId;
                        averageMark.Status = "Fail";
                        averageMark.IsActivated = true;
                        averageMark.IsEnabled = true;
                        if (result >= 5 && passFinalCondition == true)
                        {
                            averageMark.Status = "Passed";
                        }
                        context2.Marks.Add(averageMark);
                        }
                        else
                        {
                            if (result >= 5 && passFinalCondition == true)
                            {
                                oldAverageMark.Status = "Passed";
                            }
                            else
                            {
                                oldAverageMark.Status = "Fail";
                            }
                            oldAverageMark.AverageMark = result;
                            oldAverageMark.IsActivated = true;
                            oldAverageMark.IsEnabled = true;
                            context.SaveChanges();
                        }

                        context2.SaveChanges();
                    }

                }
            }
            return Json(new { success = true, message = "Successful!" });
        }

        public ActionResult DownloadExcelAverageMark(int semesterId)
        {
            MemoryStream ms = new MemoryStream();

            using (var context = new CapstoneProjectEntities())
            {
                var semester = context.RealSemesters.Find(semesterId);
                var fileName = semester.Semester + " AverageMarks";

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
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subject";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentRoll";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "AverageMark";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Status";

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
                    var count = 0;
                    var mark = context.Marks.Where(q => q.SemesterId == semesterId && q.Status != null).ToList()
                        .Select(q => new IConvertible[]
                    {
                        q.Subject_MarkComponent.SubjectId,
                       q.Student.RollNumber,
                       q.AverageMark==null?"0":q.AverageMark.Value.ToString(),
                       q.Status,
                    });
                    foreach (var item in mark)
                    {
                        ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[0];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[1];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[2];
                        ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item[3];
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