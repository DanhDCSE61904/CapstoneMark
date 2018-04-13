using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static CapstoneProject.Models.AreaViewModel;

namespace CapstoneProject.Areas.Students.Controllers
{
    public class StudentsController : Controller
    {
        // GET: Students/Students
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult StudentForFinal()
        {
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
                return Json(new { result = semesters, }, JsonRequestBehavior.AllowGet);
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

        public List<MarkGroupModel> CalculateStudentMarkComponent(int studentId, int courseId)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var groupMark = new Dictionary<String, MarkGroupModel>();
                var marks = context.Marks.Where(q => q.CourseId == courseId && q.StudentId == studentId && !q.Subject_MarkComponent.MarkComponent.Name.Equals("AVERAGE")).ToList();
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
                return groupMark.Values.ToList();
            }
        }

        public bool AnyZeroInGroupMark(List<MarkGroupModel> groupmark)
        {
            if (groupmark.Count() == 0)
            {
                return true;
            }
            foreach (var item in groupmark)
            {
                if (item.Mark == 0 || !item.Mark.HasValue)
                {
                    return true;
                }

            }
            return false;
        }

        public JsonResult LoadStudentForFinal(JQueryDataTableParamModel param, int semesterId, string subjectCode)
        {
            using (var context = new CapstoneProjectEntities())
            {
                var test = 0;
                var semester = context.RealSemesters.Find(semesterId).Semester.ToUpper();
                var course = context.Courses.Where(q => q.Semester.ToUpper().Equals(semester) && q.SubjectCode.Equals(subjectCode)).FirstOrDefault();
                var studentList = context.Attendances.Where(q => q.CourseId == course.Id).GroupBy(q => q.Student).Select(q => q.Key).ToList();
                //var stur = studentList.Where(q => q.RollNumber.Equals("SE62849"));
                var subjectSlots = context.Subjects.Where(q => q.Id.ToUpper().Equals(course.SubjectCode.ToUpper())).FirstOrDefault();
                int? slots = 0;
                if (subjectSlots!=null)
                {
                    slots = subjectSlots.NumberOfSlots;
                }
                else
                {
                    Console.WriteLine();
                }
                
                var studentResult = new List<Student>();
                var attendanceList = context.Attendances.Where(q => q.CourseId == course.Id).ToList();
                var exemptList = context.Marks.Where(q => q.IsExempt == true&&q.CourseId == course.Id).GroupBy(q => q.Student).Select(q => q.Key).ToDictionary(q=>q.RollNumber);
                foreach (var exemptStudent in exemptList)
                {
                    studentResult.Add(exemptStudent.Value);
                }
                foreach (var student in studentList)
                {
                    var attendance = attendanceList.Where(q => q.StudentId == student.Id && q.Status == true).Count();
                    if (attendance != 0)
                    {

                        //if ((attendance / slots) >= 0.8 && AnyZeroInGroupMark(CalculateStudentMarkComponent(student.Id, course.Id)) == false)
                        //{
                        //    studentResult.Add(student);
                        //}
                        double rate = double.Parse(attendance.ToString()) / double.Parse(slots.Value.ToString());
                        if (rate >= 0.8 && !exemptList.ContainsKey((student.RollNumber)))
                        {
                            studentResult.Add(student);
                        }
                    }

                }
                var count = 1;

                var result = studentResult.Select(q => new IConvertible[] {
                    count++,
                    q.RollNumber,
                    q.FullName,
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

        public ActionResult DownloadExcelStudentForFinal(int semesterId)
        {
            MemoryStream ms = new MemoryStream();

            using (var context = new CapstoneProjectEntities())
            {
                var semester = context.RealSemesters.Find(semesterId);
                var fileName = semester.Semester + " Final";
                var courseList = context.Courses.Where(q => q.Semester.ToUpper().Equals(semester.Semester)).ToList();
                var studentList = context.Attendances.Where(q => q.Course.Semester.ToUpper().Equals(semester.Semester.ToUpper())).GroupBy(q => q.Student).Select(q => q.FirstOrDefault().Student).ToList();
                var attendanceList = context.Attendances.Where(q => q.Course.Semester.ToUpper().Equals(semester.Semester)).ToList();
                var subjectList = context.Subjects.ToList();
                var markList = context.Marks.Where(q => q.SemesterId == semesterId).ToList();
                int clear = 0;
                
                using (ExcelPackage package = new ExcelPackage(ms))
                {
                    #region Excel format
                    ExcelWorkbook wb = package.Workbook;
                    foreach (var course in courseList)
                    {
                        clear++;
                        var attendanceStudentList = attendanceList.Where(q => q.CourseId == course.Id).ToList();
                        if (attendanceStudentList.Count != 0)
                        {
                            var subjectSlots = subjectList.Where(q => q.Id.ToUpper().Equals(course.SubjectCode.ToUpper())).FirstOrDefault();
                            int? slots = 0;
                            if (subjectSlots != null)
                            {
                                slots = subjectSlots.NumberOfSlots;
                            }
                            else
                            {
                                Console.WriteLine();
                            }
                            var studentResult = new List<Student>();
                            var exemptList = markList.Where(q => q.IsExempt == true && q.CourseId == course.Id).GroupBy(q => q.Student).Select(q => q.Key).ToDictionary(q => q.RollNumber);
                            foreach (var exemptStudent in exemptList)
                            {
                                studentResult.Add(exemptStudent.Value);
                            }
                            foreach (var student in studentList)
                            {
                                var attendance = attendanceStudentList.Where(q => q.StudentId == student.Id && q.Status == true).Count();
                                if (attendance != 0)
                                {

                                    //if ((attendance / slots) >= 0.8 && AnyZeroInGroupMark(CalculateStudentMarkComponent(student.Id, course.Id)) == false)
                                    //{
                                    //    studentResult.Add(student);
                                    //}
                                    double rate = double.Parse(attendance.ToString()) / double.Parse(slots.Value.ToString());
                                    if (rate >= 0.8 && !exemptList.ContainsKey((student.RollNumber)))
                                    {
                                        studentResult.Add(student);
                                    }
                                }

                            }
                            if (studentResult != null)
                            {
                                var studentCount = 0;
                                var sheetCourseNum = 1;
                                ExcelWorksheet ws = wb.Worksheets.Add(course.SubjectCode + "_" + sheetCourseNum);
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
                                foreach (var item in studentResult)
                                {
                                    ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.RollNumber;
                                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.FullName;
                                    if (studentCount != 19)
                                    {
                                        studentCount++;
                                        
                                        StartHeaderChar = 'A';
                                    }
                                    else
                                    {
                                        studentCount = 0;
                                        sheetCourseNum++;
                                        ws = wb.Worksheets.Add(course.SubjectCode + "_" + sheetCourseNum);
                                        StartHeaderChar = 'A';
                                        StartHeaderNumber = 1;
                                        count = 1;
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


                                        EndHeaderChar = --StartHeaderChar;
                                        EndHeaderNumber = StartHeaderNumber;
                                        StartHeaderChar = 'A';
                                        StartHeaderNumber = 1;
                                        #endregion
                                    }
                                }


                                StartHeaderNumber = 1;
                                ws.Cells.AutoFitColumns();
                                //ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                                //":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                #endregion

                                #endregion


                            }
                        }
                        if (clear == 10)
                        {
                            GC.Collect();
                            clear = 0;
                        }

                    }
                    fileName += ".xlsx";
                    package.SaveAs(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return this.File(ms, contentType, fileName);
                }
            }
        }
    }
}