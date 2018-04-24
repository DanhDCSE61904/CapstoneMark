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
                if (subjectSlots != null)
                {
                    slots = subjectSlots.NumberOfSlots;
                }
                else
                {
                    Console.WriteLine();
                }

                var studentResult = new List<Student>();
                var attendanceList = context.Attendances.Where(q => q.CourseId == course.Id).ToList();
                var exemptList = context.Marks.Where(q => q.IsExempt == true && q.CourseId == course.Id).GroupBy(q => q.Student).Select(q => q.Key).ToDictionary(q => q.RollNumber);
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
                var attendanceList = context.Attendances.Where(q => q.Course.Semester.ToUpper().Equals(semester.Semester)).ToList();
                var studentList = attendanceList.GroupBy(q => q.Student).Select(q => q.FirstOrDefault().Student).ToList();

                var subjectList = context.Subjects.ToDictionary(q => q.Id);
                var markExemptList = context.Marks.Where(q => q.SemesterId == semesterId && q.IsExempt!=null).ToList();
                Dictionary<string, string> foreignLanguageSubject = new Dictionary<string, string>();
                var fileContent = new FileInfo(System.Web.Hosting.HostingEnvironment.MapPath("/PropertiesFiles/DS mon ngoai ngu.xlsx"));

                using (ExcelPackage package = new ExcelPackage(fileContent))
                {
                    ExcelWorkbook wb = package.Workbook;
                    ExcelWorksheet ws = wb.Worksheets.First();
                    var totalCol = ws.Dimension.Columns;
                    var totalRow = ws.Dimension.Rows;
                    var subjectCol = 2;
                    var titleRow = 1;
                    var firstRecordRow = 2;

                    for (int i = firstRecordRow; i <= totalRow; i++)
                    {
                        if (!foreignLanguageSubject.ContainsKey(ws.Cells[i, subjectCol].Text.ToUpper()))
                        {
                            foreignLanguageSubject.Add(ws.Cells[i, subjectCol].Text.ToUpper(), ws.Cells[i, subjectCol + 1].Text);
                        }
                    }
                }
                Dictionary<string, StatisticFinal> statisticList = new Dictionary<string, StatisticFinal>();

                List<LeftOverStudent> leftOverStudentList = new List<LeftOverStudent>();

                int clear = 0;

                using (ExcelPackage package = new ExcelPackage(ms))
                {
                    #region Excel format
                    ExcelWorkbook wb = package.Workbook;
                    ExcelWorksheet firstWs = wb.Worksheets.Add("Thống kê");
                    foreach (var course in courseList)
                    {
                        if (course.SubjectCode.ToUpper().Contains("VOV") || course.SubjectCode.ToUpper().Contains("LAB"))
                        {
                            continue;
                        }

                        if (!statisticList.ContainsKey(course.SubjectCode))
                        {
                            StatisticFinal sta = new StatisticFinal();
                            sta.Subject = course.SubjectCode;
                            sta.NumberOfRoom = 1;
                            sta.NumberOfStudent = 0;
                            statisticList.Add(course.SubjectCode, sta);
                        }
                        clear++;
                        var attendanceStudentList = attendanceList.Where(q => q.CourseId == course.Id).ToList();
                        if (attendanceStudentList.Count != 0)
                        {
                            var subjectSlots = subjectList.Values.Where(q => q.Id.ToUpper().Equals(course.SubjectCode.ToUpper())).FirstOrDefault();
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
                            var exemptList = markExemptList.Where(q => q.IsExempt == true && q.CourseId == course.Id).GroupBy(q => q.Student).Select(q => q.Key).ToDictionary(q => q.RollNumber);
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
                            if (studentResult != null && studentResult.Count != 0)
                            {
                                statisticList[course.SubjectCode].NumberOfStudent = studentResult.Count();
                                var studentCount = -1;
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
                                var leftOutCount = studentResult.Count();

                                foreach (var item in studentResult)
                                {
                                    if (foreignLanguageSubject.ContainsKey(course.SubjectCode))
                                    {
                                        if (studentCount < 19)
                                        {
                                            StartHeaderChar = 'A';
                                            studentCount++;
                                            ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.RollNumber;
                                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.FullName;
                                            StartHeaderChar = 'A';
                                        }
                                        else
                                        {
                                            statisticList[course.SubjectCode].NumberOfRoom++;
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
                                            ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.RollNumber;
                                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.FullName;
                                            #endregion

                                        }
                                    }
                                    else
                                    {
                                        if (studentResult.Count < 15)
                                        {
                                            StartHeaderChar = 'A';
                                            StartHeaderNumber = 1;
                                            LeftOverStudent stu = new LeftOverStudent();
                                            stu.Subject = course.SubjectCode;
                                            stu.RollNumber = item.RollNumber;
                                            stu.FullName = item.FullName;
                                            leftOverStudentList.Add(stu);
                                            if (wb.Worksheets[course.SubjectCode + "_" + sheetCourseNum] != null)
                                            {
                                                statisticList[course.SubjectCode].NumberOfRoom = 0;
                                                wb.Worksheets.Delete(course.SubjectCode + "_" + sheetCourseNum);
                                            }
                                        }
                                        else
                                        {
                                            leftOutCount--;
                                            if (studentCount < 19)
                                            {
                                                StartHeaderChar = 'A';
                                                ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                                                ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.RollNumber;
                                                ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.FullName;
                                                studentCount++;

                                                StartHeaderChar = 'A';
                                            }
                                            else
                                            {
                                                //Bo vao phong tong hop
                                                if (leftOutCount < 15)
                                                {
                                                    StartHeaderChar = 'A';
                                                    StartHeaderNumber = 1;
                                                    LeftOverStudent stu = new LeftOverStudent();
                                                    stu.Subject = course.SubjectCode;
                                                    stu.RollNumber = item.RollNumber;
                                                    stu.FullName = item.FullName;
                                                    leftOverStudentList.Add(stu);
                                                }

                                                else
                                                {
                                                    statisticList[course.SubjectCode].NumberOfRoom++;
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
                                                    ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                                                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.RollNumber;
                                                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = item.FullName;

                                                }

                                            }
                                        }
                                    }
                                }


                                StartHeaderNumber = 1;
                                ws.Cells.AutoFitColumns();
                                //ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                                //":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                #endregion

                                #endregion



                            }
                            else
                            {
                                statisticList.Remove(course.SubjectCode);
                            }
                        }
                        else
                        {
                            statisticList.Remove(course.SubjectCode);
                        }
                        if (clear == 10)
                        {
                            GC.Collect();
                            clear = 0;
                        }

                    }
                    if (leftOverStudentList.Count != 0)
                    {
                        StatisticFinal staTH = new StatisticFinal();
                        staTH.Subject = "Tong hop";
                        staTH.NumberOfRoom = 1;
                        statisticList.Add("Tong hop", staTH);
                        var studentCountTH = -1;
                        var sheetCourseNumTH = 1;
                        ExcelWorksheet wsTH = wb.Worksheets.Add("Tong hop_1");
                        char StartHeaderCharTH = 'A';
                        int StartHeaderNumberTH = 1;
                        var countTH = 1;
                        wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "No";
                        wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "StudentRoll";
                        wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "StudentName";
                        wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "Subbject";
                        var EndHeaderCharTH = --StartHeaderCharTH;
                        var EndHeaderNumberTH = StartHeaderNumberTH;
                        StartHeaderCharTH = 'A';
                        StartHeaderNumberTH = 1;
                        foreach (var item in leftOverStudentList)
                        {
                            if (studentCountTH < 19)
                            {
                                StartHeaderCharTH = 'A';
                                studentCountTH++;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (++StartHeaderNumberTH)].Value = countTH++;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = item.RollNumber;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = item.FullName;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = item.Subject;
                                StartHeaderCharTH = 'A';
                            }
                            else
                            {
                                statisticList["Tong hop"].NumberOfRoom++;
                                studentCountTH = 0;
                                sheetCourseNumTH++;
                                wsTH = wb.Worksheets.Add("Tong hop_" + sheetCourseNumTH);
                                StartHeaderCharTH = 'A';
                                StartHeaderNumberTH = 1;
                                countTH = 1;
                                #region Headers
                                //ws.Cells[0, 0].Style.WrapText = true;
                                //Image img = CaptstoneProject.Properties.Resources.img_logo_fe;
                                //ExcelPicture pic = ws.Drawings.AddPicture("FPTLogo", img);
                                //pic.From.Column = 0;
                                //pic.From.Row = 0;
                                //pic.SetSize(320, 240);
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "No";
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "StudentRoll";
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "StudentName";
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = "Subject";


                                EndHeaderCharTH = --StartHeaderCharTH;
                                EndHeaderNumberTH = StartHeaderNumberTH;
                                StartHeaderCharTH = 'A';
                                StartHeaderNumberTH = 1;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (++StartHeaderNumberTH)].Value = countTH++;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = item.RollNumber;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = item.FullName;
                                wsTH.Cells["" + (StartHeaderCharTH++) + (StartHeaderNumberTH)].Value = item.Subject;
                                #endregion

                            }
                        }
                    }
                    char StartHeaderCharTK = 'A';
                    int StartHeaderNumberTK = 1;
                    //ws.Cells[0, 0].Style.WrapText = true;
                    //Image img = CaptstoneProject.Properties.Resources.img_logo_fe;
                    //ExcelPicture pic = ws.Drawings.AddPicture("FPTLogo", img);
                    //pic.From.Column = 0;
                    //pic.From.Row = 0;
                    //pic.SetSize(320, 240);
                    firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = "Subject";
                    firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = "NumberOfRoom";
                    firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = "NumberOfStudent";


                    var EndHeaderCharTK = --StartHeaderCharTK;
                    var EndHeaderNumberTK = StartHeaderNumberTK;
                    StartHeaderCharTK = 'A';
                    StartHeaderNumberTK = 1;
                    foreach (var stat in statisticList)
                    {
                        if (stat.Key != "Tong hop")
                        {
                            firstWs.Cells["" + (StartHeaderCharTK++) + (++StartHeaderNumberTK)].Value = stat.Value.Subject;
                            firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = stat.Value.NumberOfRoom;
                            firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = stat.Value.NumberOfStudent;
                            StartHeaderCharTK = 'A';
                        }
                        else
                        {
                            firstWs.Cells["" + (StartHeaderCharTK++) + (++StartHeaderNumberTK)].Value = stat.Value.Subject;
                            firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = stat.Value.NumberOfRoom;
                            firstWs.Cells["" + (StartHeaderCharTK++) + (StartHeaderNumberTK)].Value = leftOverStudentList.Count();
                            StartHeaderCharTK = 'A';
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
    public class LeftOverStudent
    {
        public string Subject { get; set; }
        public string RollNumber { get; set; }
        public string FullName { get; set; }
    }
    public class StatisticFinal
    {
        public string Subject { get; set; }
        public int NumberOfRoom { get; set; }
        public int NumberOfStudent { get; set; }
    }
}