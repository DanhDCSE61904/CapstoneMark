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

        public ActionResult SchedulingForFinal()
        {
            return View();
        }

        public ActionResult EditSchedulingForFinal()
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
                var subjectList = context.Courses.Where(q => q.Semester.Equals(semester) && !q.SubjectCode.ToUpper().Contains("LAB") && !q.SubjectCode.ToUpper().Contains("VOV")).Select(q => new SelectorViewModel
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
                var markExemptList = context.Marks.Where(q => q.SemesterId == semesterId && q.IsExempt != null).ToList();
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

        public ActionResult SimpleDownloadExcelStudentForFinal(int semesterId)
        {
            MemoryStream ms = new MemoryStream();
            using (ExcelPackage package = new ExcelPackage(ms))
            {
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
                //ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "No";
                ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentRoll";
                ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentName";
                ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subjects";
                ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "NumberOfSubject";

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
                using (var context = new CapstoneProjectEntities())
                {
                    var semester = context.RealSemesters.Find(semesterId);
                    var fileName = semester.Semester + " FinalSubjects";
                    var courseList = context.Courses.Where(q => q.Semester.ToUpper().Equals(semester.Semester)).ToList();
                    var attendanceList = context.Attendances.Where(q => q.Course.Semester.ToUpper().Equals(semester.Semester)).ToList();
                    var studentList = attendanceList.GroupBy(q => q.Student).Select(q => q.FirstOrDefault().Student).ToList();

                    var subjectList = context.Subjects.ToDictionary(q => q.Id);
                    var markExemptList = context.Marks.Where(q => q.SemesterId == semesterId && q.IsExempt != null).ToList();
                    foreach (var stu in studentList)
                    {
                        var exemptList = markExemptList.Where(q => q.IsExempt == true && q.StudentId == stu.Id && !q.Course.SubjectCode.ToUpper().Contains("LAB") && !q.Course.SubjectCode.ToUpper().Contains("VOV")).GroupBy(q => q.Course).Select(q => q.Key).ToDictionary(q => q.SubjectCode);
                        var courseResult = new List<Course>();
                        foreach (var exemptCourse in exemptList)
                        {
                            courseResult.Add(exemptCourse.Value);
                        }
                        var attendanceStudentList = attendanceList.Where(q => q.StudentId == stu.Id).ToList();
                        if (attendanceStudentList.Count != 0)
                        {
                            var courseStudy = attendanceStudentList.Where(q => !q.Course.SubjectCode.ToUpper().Contains("LAB") && !q.Course.SubjectCode.ToUpper().Contains("VOV")).GroupBy(q => q.Course).Select(q => q.FirstOrDefault().Course).ToList();
                            foreach (var course in courseStudy)
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
                                var attendance = attendanceStudentList.Where(q => q.CourseId == course.Id && q.Status == true).Count();
                                if (attendance != 0)
                                {
                                    double rate = double.Parse(attendance.ToString()) / double.Parse(slots.Value.ToString());
                                    if (rate >= 0.8 && !exemptList.ContainsKey(course.SubjectCode))
                                    {
                                        courseResult.Add(course);
                                    }
                                }
                            }
                        }

                        if (courseResult.Count != 0)
                        {
                            //ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                            ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = stu.RollNumber;
                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = stu.FullName;
                            var subjectsFinal = "";
                            foreach (var item in courseResult)
                            {
                                if (subjectsFinal != "")
                                {
                                    subjectsFinal = subjectsFinal + ", " + item.SubjectCode;
                                }
                                else
                                {
                                    subjectsFinal = subjectsFinal + item.SubjectCode;
                                }
                            }
                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = subjectsFinal;
                            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = courseResult.Count();

                            StartHeaderChar = 'A';
                        }

                    }
                    fileName += ".xlsx";

                    StartHeaderNumber = 1;
                    ws.Cells.AutoFitColumns();


                    package.SaveAs(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return this.File(ms, contentType, fileName);
                }
            }
        }

        public ProcessedScheduleFile SlotScheldule(Dictionary<string, List<string>> dayList,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots, Dictionary<string, int> subjectWithStudentCount, List<StudentSorted> studentList)
        {
            var slotsConfict = false;
            var numberOfDay = dayList.Keys.Count();
            var numberOfSlots = dayListWithSlots["DAY 1"].Keys.Count();
            Dictionary<string, List<string>> dayListModified = new Dictionary<string, List<string>>();
            try
            {
                for (int i = 1; i <= numberOfDay; i++)
                {
                    var subjectPerSlots = Math.Ceiling((double)dayList["DAY " + i].Count() / (double)numberOfSlots);
                    //int studentsPerSlots = 0;
                    int studentInDay = 0;
                    foreach (var item in dayList["DAY " + i])
                    {
                        studentInDay += subjectWithStudentCount[item];
                    }
                    //studentsPerSlots = studentInDay / numberOfSlots;
                    var numOfSubInSlots = 0;
                    var m = 1;
                    foreach (var item in dayList["DAY " + i])
                    {
                        var contained = false;
                        Dictionary<int, int> slotCantUsed = new Dictionary<int, int>();
                        if (m > numberOfSlots)
                        {
                            m = 1;
                        }
                        for (int k = 1; k <= numberOfSlots; k++)
                        {
                            if (dayListWithSlots["DAY " + i]["SLOT " + k].ContainsKey(item))
                            {
                                contained = true;
                            }
                        }
                        if (contained == false)
                        {
                            //if (numOfSubInSlots >= subjectPerSlots)
                            //{
                            //    numOfSubInSlots = 1;
                            //    m++;
                            //}
                            //else
                            //{
                            //    numOfSubInSlots++;
                            //}
                            //Kiem tra slot co the su dung
                            foreach (var stu in studentList)
                            {
                                if (stu.RollNumber.Equals("SE62779") && (item.Equals("JPD121") || item.Equals("CHN132")))
                                {
                                    Console.WriteLine("");
                                }
                                var studyThisSubject = false;
                                foreach (var subStu in stu.Subjects)
                                {
                                    if (subStu.Equals(item))
                                    {
                                        studyThisSubject = true;
                                    }
                                }
                                if (studyThisSubject)
                                {
                                    foreach (var subStu in stu.Subjects)
                                    {
                                        for (int n = 1; n <= numberOfSlots; n++)
                                        {
                                            if (dayListWithSlots["DAY " + i]["SLOT " + n].ContainsKey(subStu))
                                            {
                                                if (!slotCantUsed.ContainsKey(n))
                                                {
                                                    slotCantUsed.Add(n, n);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (slotCantUsed.Count != numberOfSlots)
                            {
                                List<Slot> slotList = new List<Slot>();
                                foreach (var slot in dayListWithSlots["DAY " + i])
                                {
                                    var numberStu = 0;
                                    foreach (var subject in slot.Value)
                                    {
                                        numberStu += subjectWithStudentCount[subject.Key];
                                    }
                                    Slot s = new Slot();
                                    s.SlotNumber = int.Parse(slot.Key.Replace("SLOT ", ""));
                                    s.NumberOfStudent = numberStu;
                                    slotList.Add(s);
                                }
                                var slotList2 = slotList.OrderBy(q => q.NumberOfStudent).ToList();

                                foreach (var slot in slotList)
                                {
                                    foreach (var s in slotCantUsed)
                                    {
                                        if (slot.SlotNumber == s.Key)
                                        {
                                            slotList2.Remove(slot);
                                        }
                                    }
                                }
                                int num = subjectWithStudentCount[item];
                                dayListWithSlots["DAY " + i]["SLOT " + (slotList2[0].SlotNumber)].Add(item, num);
                            }
                            else
                            {

                                foreach (var day in dayList)
                                {
                                    List<string> subjectList = new List<string>();
                                    foreach (var subject in dayList[day.Key])
                                    {
                                        if (!subject.Equals(item))
                                        {
                                            subjectList.Add(subject);
                                        }
                                    }
                                    dayListModified.Add(day.Key, subjectList);
                                }
                                var dayToTransfer = i + 1;
                                if (i == numberOfDay)
                                {
                                    dayToTransfer = 1;
                                }
                                dayListModified["DAY " + dayToTransfer].Add(item);
                                slotsConfict = true;
                                break;
                            }
                        }
                        if (slotsConfict == true) { break; }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }
            ProcessedScheduleFile file = new ProcessedScheduleFile();
            file.confict = slotsConfict;
            if (slotsConfict == true)
            {
                file.dayList = dayListModified;
            }
            else
            {
                file.dayList = dayList;
                file.dayListWithSlots = dayListWithSlots;
            }
            return file;
        }

        public ActionResult SchelduleDayAndSlotsForFinal(int numberOfDay, int numberOfSlots)
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
                                var studentCodeCol = 1;
                                var subjectCol = 3;
                                var numSubjectCol = 4;
                                var titleRow = 1;
                                var firstRecordRow = 2;

                                Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
                                Dictionary<string, List<string>> dayList = new Dictionary<string, List<string>>();
                                Dictionary<string, int> subjectWithStudentCount = new Dictionary<string, int>();
                                List<StudentSorted> studentList = new List<StudentSorted>();

                                for (int i = 1; i <= numberOfDay; i++)
                                {
                                    List<string> newSubject = new List<string>();
                                    List<SubjectForScheduling> newSubject2 = new List<SubjectForScheduling>();
                                    Dictionary<string, Dictionary<string, int>> slotList = new Dictionary<string, Dictionary<string, int>>();
                                    for (int k = 1; k <= numberOfSlots; k++)
                                    {
                                        slotList.Add("SLOT " + k, new Dictionary<string, int>());
                                    }
                                    dayList.Add("DAY " + i, newSubject);
                                    dayListWithSlots.Add("DAY " + i, slotList);
                                }
                                for (int i = firstRecordRow; i <= totalRow; i++)
                                {
                                    var studentRollNumber = ws.Cells[i, studentCodeCol].Text.ToUpper();
                                    var studentSubjects = ws.Cells[i, subjectCol].Text.Replace(" ", "").Split(',').ToList();
                                    foreach (var item in studentSubjects)
                                    {
                                        if (!subjectWithStudentCount.ContainsKey(item))
                                        {
                                            subjectWithStudentCount.Add(item, 1);
                                        }
                                        else
                                        {
                                            subjectWithStudentCount[item] = subjectWithStudentCount[item] + 1;
                                        }
                                    }
                                    var numberOfSubjects = int.Parse(ws.Cells[i, numSubjectCol].Text);
                                    StudentSorted newStu = new StudentSorted();
                                    newStu.RollNumber = studentRollNumber;
                                    newStu.Subjects = studentSubjects;
                                    newStu.NumberOfSubjects = numberOfSubjects;
                                    studentList.Add(newStu);
                                }
                                studentList = studentList.OrderByDescending(q => q.NumberOfSubjects).ToList();

                                //Xep ngay thi
                                var index = 0;
                                foreach (var student in studentList)
                                {
                                    index++;
                                    if (index % 2 == 0)
                                    {
                                        int i = 1;
                                        foreach (var sub in student.Subjects)
                                        {
                                            var contained = false;
                                            if (i > numberOfDay)
                                            {
                                                i = 1;
                                            }
                                            for (int k = 1; k <= numberOfDay; k++)
                                            {
                                                if (dayList["DAY " + k].Contains(sub))
                                                {
                                                    contained = true;
                                                }
                                            }
                                            if (contained == false)
                                            {
                                                dayList["DAY " + i].Add(sub);
                                            }
                                            i++;
                                        }
                                    }
                                    else
                                    {
                                        int i = numberOfDay;
                                        foreach (var sub in student.Subjects)
                                        {
                                            var contained = false;
                                            if (i < 1)
                                            {
                                                i = numberOfDay;
                                            }
                                            for (int k = 1; k <= numberOfDay; k++)
                                            {
                                                if (dayList["DAY " + k].Contains(sub))
                                                {
                                                    contained = true;
                                                }
                                            }
                                            if (contained == false)
                                            {
                                                dayList["DAY " + i].Add(sub);
                                            }
                                            i--;
                                        }
                                    }
                                }
                                //for (int i = 1; i <= numberOfDay; i++)
                                //{
                                //    foreach(var item in dayList["DAY " + i])
                                //    {
                                //        SubjectForScheduling sub = new SubjectForScheduling();
                                //        sub.NumberOfStudent = subjectWithStudentCount[item];
                                //        sub.Subject = item;
                                //        dayListWithNumbers["DAY " + i].Add(sub);
                                //    }

                                //}


                                //Xep ca thi
                                var error = false;
                                var numberOfTimes = 0;
                                ProcessedScheduleFile result = null;
                                
                                result = SlotScheldule(dayList, dayListWithSlots, subjectWithStudentCount, studentList);
                                do
                                {
                                    if (result.confict == true)
                                    {
                                        result = SlotScheldule(result.dayList, dayListWithSlots, subjectWithStudentCount, studentList);
                                        error = true;
                                        numberOfTimes++;
                                    }
                                    else
                                    {
                                        error = false;
                                    }
                                }
                                while (error == true);
                                if (result.confict == true)
                                {
                                    Console.WriteLine("RIP");
                                }
                                dayList = result.dayList;
                                dayListWithSlots = result.dayListWithSlots;


                                //In file excel
                                MemoryStream ms = new MemoryStream();
                                var fileName = "LichThi";
                                using (ExcelPackage packageExport = new ExcelPackage(ms))
                                {
                                    #region Excel format
                                    ExcelWorksheet ws2 = packageExport.Workbook.Worksheets.Add("All days");
                                    char StartHeaderChar = 'A';
                                    int StartHeaderNumber = 1;
                                    #region Headers
                                    //ws.Cells[0, 0].Style.WrapText = true;
                                    //Image img = CaptstoneProject.Properties.Resources.img_logo_fe;
                                    //ExcelPicture pic = ws.Drawings.AddPicture("FPTLogo", img);
                                    //pic.From.Column = 0;
                                    //pic.From.Row = 0;
                                    //pic.SetSize(320, 240);
                                    //ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "No";
                                    ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Day";
                                    ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subjects";
                                    ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "NumberOfStudent";

                                    var EndHeaderChar = --StartHeaderChar;
                                    var EndHeaderNumber = StartHeaderNumber;
                                    StartHeaderChar = 'A';
                                    StartHeaderNumber = 1;
                                    #endregion
                                    #region Header styling
                                    ws2.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                                    ":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Font.Bold = true;


                                    //StartHeaderNumber++;
                                    #endregion
                                    #region Set values for available fields
                                    foreach (var item in dayList)
                                    {
                                        ws2.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = item.Key;
                                        var subjects = "";
                                        var num = 0;
                                        foreach (var sub in item.Value)
                                        {
                                            if (subjects == "")
                                            {
                                                subjects += sub;
                                            }
                                            else
                                            {
                                                subjects += ", " + sub;
                                            }
                                            num += subjectWithStudentCount[sub];
                                        }
                                        ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = subjects;
                                        ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = num;
                                        StartHeaderChar = 'A';
                                    }
                                    foreach (var item in dayListWithSlots)
                                    {
                                        ExcelWorksheet ws3 = packageExport.Workbook.Worksheets.Add(item.Key);
                                        StartHeaderChar = 'A';
                                        StartHeaderNumber = 1;
                                        #region Headers
                                        ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Slot";
                                        ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subjects";
                                        ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "NumberOfStudent";
                                        EndHeaderChar = --StartHeaderChar;
                                        EndHeaderNumber = StartHeaderNumber;
                                        StartHeaderChar = 'A';
                                        StartHeaderNumber = 1;
                                        #endregion
                                        #region Header styling
                                        ws3.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                                        ":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Font.Bold = true;


                                        //StartHeaderNumber++;
                                        #endregion
                                        foreach (var slot in item.Value)
                                        {
                                            //ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                                            ws3.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = slot.Key;
                                            var num = 0;
                                            string subjects = "";
                                            foreach (var sub in slot.Value)
                                            {
                                                if (subjects == "")
                                                {
                                                    subjects += sub.Key;
                                                    //subjects += sub.Key + " (" + sub.Value + ")";
                                                }
                                                else
                                                {
                                                    subjects += ", " + sub.Key;
                                                    //subjects += ", " + sub.Key + " (" + sub.Value + ")";
                                                }
                                                num += subjectWithStudentCount[sub.Key];
                                            }
                                            ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = subjects;
                                            ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = num;
                                            StartHeaderChar = 'A';
                                            ws3.Cells.AutoFitColumns();
                                        }
                                    }
                                    fileName += ".xlsx";

                                    StartHeaderNumber = 1;
                                    ws2.Cells.AutoFitColumns();
                                    //ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                                    //":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                                    #endregion

                                    #endregion

                                    packageExport.SaveAs(ms);
                                    ms.Seek(0, SeekOrigin.Begin);
                                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                                    return this.File(ms, contentType, fileName);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public ProcessedStudentFile ProcessStudentFile(HttpPostedFileBase studentFile)
        {
            List<StudentSorted> studentList = new List<StudentSorted>();
            Dictionary<string, int> subjectWithStudentCount = new Dictionary<string, int>();
            if (studentFile != null && studentFile.ContentLength > 0)
            {
                var stream = studentFile.InputStream;

                using (ExcelPackage package = new ExcelPackage(stream))
                {
                    var ws = package.Workbook.Worksheets.First();
                    var totalCol = ws.Dimension.Columns;
                    var totalRow = ws.Dimension.Rows;
                    var studentCodeCol = 1;
                    var subjectCol = 3;
                    var numSubjectCol = 4;
                    var titleRow = 1;
                    var firstRecordRow = 2;

                    for (int i = firstRecordRow; i <= totalRow; i++)
                    {
                        var studentRollNumber = ws.Cells[i, studentCodeCol].Text.ToUpper();
                        var studentSubjects = ws.Cells[i, subjectCol].Text.Replace(" ", "").Split(',').ToList();
                        foreach (var item in studentSubjects)
                        {
                            if (!subjectWithStudentCount.ContainsKey(item))
                            {
                                subjectWithStudentCount.Add(item, 1);
                            }
                            else
                            {
                                subjectWithStudentCount[item] = subjectWithStudentCount[item] + 1;
                            }
                        }
                        var numberOfSubjects = int.Parse(ws.Cells[i, numSubjectCol].Text);
                        StudentSorted newStu = new StudentSorted();
                        newStu.RollNumber = studentRollNumber;
                        newStu.Subjects = studentSubjects;
                        newStu.NumberOfSubjects = numberOfSubjects;
                        studentList.Add(newStu);
                    }
                    studentList = studentList.OrderByDescending(q => q.NumberOfSubjects).ToList();
                }
            }
            ProcessedStudentFile file = new ProcessedStudentFile();
            file.studentList = studentList;
            file.subjectWithStudentCount = subjectWithStudentCount;
            return file;
        }

        public ProcessedScheduleFile ProcessScheduleFile(HttpPostedFileBase scheduleFile, Dictionary<string, int> subjectWithStudentCount)
        {
            Dictionary<string, List<string>> dayList = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            if (scheduleFile != null && scheduleFile.ContentLength > 0)
            {
                var stream = scheduleFile.InputStream;

                using (ExcelPackage package = new ExcelPackage(stream))
                {
                    var fws = package.Workbook.Worksheets.First();
                    var firstTotalCol = fws.Dimension.Columns;
                    var firstTotalRow = fws.Dimension.Rows;
                    var dayCol = 1;
                    var subjectsCol = 2;
                    var titleRow = 1;
                    var firstRecordRow = 2;
                    for (int i = firstRecordRow; i <= firstTotalRow; i++)
                    {
                        dayList.Add(fws.Cells[i, dayCol].Text, fws.Cells[i, subjectsCol].Text.Replace(" ", "").Split(',').ToList());
                    }

                    for (int i = 2; i <= package.Workbook.Worksheets.Count(); i++)
                    {
                        Dictionary<string, Dictionary<string, int>> slot = new Dictionary<string, Dictionary<string, int>>();
                        dayListWithSlots.Add("DAY " + (i - 1), slot);
                        var ws = package.Workbook.Worksheets[i];
                        var totalCol = ws.Dimension.Columns;
                        var totalRow = ws.Dimension.Rows;
                        for (int k = firstRecordRow; k <= totalRow; k++)
                        {
                            Dictionary<string, int> subjectList = new Dictionary<string, int>();
                            dayListWithSlots["DAY " + (i - 1)].Add("SLOT " + (k - 1), subjectList);
                            foreach (var sub in ws.Cells[k, subjectsCol].Text.Replace(" ", "").Split(',').ToList())
                            {
                                var num = subjectWithStudentCount[sub];
                                dayListWithSlots["DAY " + (i - 1)]["SLOT " + (k - 1)].Add(sub, num);
                            }
                        }
                    }

                }
            }
            ProcessedScheduleFile file = new ProcessedScheduleFile();
            file.dayList = dayList;
            file.dayListWithSlots = dayListWithSlots;
            return file;
        }

        public ActionResult GetFilesToEditFinal()
        {
            try
            {
                if (Request.Files.Count > 1)
                {
                    var studentFile = Request.Files["student-file"];
                    var scheduldeFile = Request.Files["schedule-file"];

                    Dictionary<string, int> subjectWithStudentCount = new Dictionary<string, int>();
                    List<StudentSorted> studentList = new List<StudentSorted>();
                    Dictionary<string, List<string>> dayList = new Dictionary<string, List<string>>();
                    Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

                    ProcessedStudentFile file = ProcessStudentFile(studentFile);
                    studentList = file.studentList;
                    subjectWithStudentCount = file.subjectWithStudentCount;

                    ProcessedScheduleFile file2 = ProcessScheduleFile(scheduldeFile, subjectWithStudentCount);
                    dayList = file2.dayList;
                    dayListWithSlots = file2.dayListWithSlots;

                    return Json(new { success = true, dayList = dayList.Keys.ToList(), slotList = dayListWithSlots["DAY 1"].Keys.ToList() });

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
            return Json(new { success = false });
        }

        public ActionResult GetSubjectsToChange(string changeList, string dayChoose, string slotChoose)
        {
            try
            {
                if (Request.Files.Count > 1)
                {
                    var studentFile = Request.Files["student-file"];
                    var scheduldeFile = Request.Files["schedule-file"];

                    Dictionary<string, int> subjectWithStudentCount = new Dictionary<string, int>();
                    List<StudentSorted> studentList = new List<StudentSorted>();
                    Dictionary<string, List<string>> dayList = new Dictionary<string, List<string>>();
                    Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

                    ProcessedStudentFile file = ProcessStudentFile(studentFile);
                    studentList = file.studentList;
                    subjectWithStudentCount = file.subjectWithStudentCount;

                    ProcessedScheduleFile file2 = ProcessScheduleFile(scheduldeFile, subjectWithStudentCount);
                    dayList = file2.dayList;
                    dayListWithSlots = file2.dayListWithSlots;

                    if (changeList != "")
                    {
                        ProcessedScheduleFile applied = ApplyChangeToList(dayList, dayListWithSlots, subjectWithStudentCount, changeList);
                        dayList = applied.dayList;
                        dayListWithSlots = applied.dayListWithSlots;
                    }

                    return Json(new { success = true, subList = dayListWithSlots[dayChoose][slotChoose].Keys.ToList(), });

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
            return Json(new { success = false });
        }

        public ProcessedScheduleFile ApplyChangeToList(Dictionary<string, List<string>> dayList,
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots, Dictionary<string, int> subjectWithStudentCount, string changeList)
        {
            Dictionary<string, List<string>> dayListModified = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlotsModified = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            if (changeList != "")
            {
                foreach (var day in dayList)
                {
                    List<string> subjectList = new List<string>();
                    foreach (var subject in dayList[day.Key])
                    {
                        subjectList.Add(subject);
                    }
                    dayListModified.Add(day.Key, subjectList);

                    //Create dayListWithSlotsModified
                    Dictionary<string, Dictionary<string, int>> slotList = new Dictionary<string, Dictionary<string, int>>();
                    foreach (var slot in dayListWithSlots[day.Key])
                    {
                        Dictionary<string, int> subjects = new Dictionary<string, int>();
                        slotList.Add(slot.Key, subjects);
                        foreach (var sub in dayListWithSlots[day.Key][slot.Key])
                        {
                            var num = subjectWithStudentCount[sub.Key];
                            slotList[slot.Key].Add(sub.Key, num);
                        }
                    }
                    dayListWithSlotsModified.Add(day.Key, slotList);
                }
                //dayListModified = dayList;
                //dayListWithSlotsModified = dayListWithSlots;
                var change = changeList.Split(',');
                for (int i = 0; i < change.Length; i++)
                {
                    var detail = change[i].Split('-');
                    var dayModify = detail[0];
                    var slotModify = detail[1];
                    var subjectModify = detail[2];

                    foreach (var day in dayList)
                    {
                        foreach (var sub in day.Value)
                        {
                            if (sub.Equals(subjectModify))
                            {
                                dayListModified[day.Key].Remove(sub);
                            }
                        }
                    }
                }
                for (int i = 0; i < change.Length; i++)
                {
                    var detail = change[i].Split('-');
                    var dayModify = detail[0];
                    var slotModify = detail[1];
                    var subjectModify = detail[2];

                    foreach (var day in dayListWithSlots)
                    {
                        foreach (var slot in day.Value)
                        {
                            foreach (var sub in slot.Value)
                            {
                                if (sub.Key.Equals(subjectModify))
                                {
                                    dayListWithSlotsModified[day.Key][slot.Key].Remove(subjectModify.ToString());
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < change.Length; i++)
                {
                    var detail = change[i].Split('-');
                    var dayModify = detail[0];
                    var slotModify = detail[1];
                    var subjectModify = detail[2];

                    dayListModified[dayModify.ToString()].Add(subjectModify.ToString());
                    var num = subjectWithStudentCount[subjectModify.ToString()];
                    dayListWithSlotsModified[dayModify.ToString()][slotModify.ToString()].Add(subjectModify.ToString(), num);
                }
                ProcessedScheduleFile result = new ProcessedScheduleFile();
                result.dayListWithSlots = dayListWithSlotsModified;
                result.dayList = dayListModified;
                return result;
            }
            ProcessedScheduleFile nothing = new ProcessedScheduleFile();
            nothing.dayListWithSlots = dayListWithSlots;
            nothing.dayList = dayList;
            return nothing;

        }

        public ActionResult DaysSlotsAvailableToChange(string changeList, string dayChange, string slotChange, string subjectChoose)
        {
            if (Request.Files.Count > 1)
            {
                var studentFile = Request.Files["student-file"];
                var scheduldeFile = Request.Files["schedule-file"];

                Dictionary<string, int> subjectWithStudentCount = new Dictionary<string, int>();
                List<StudentSorted> studentList = new List<StudentSorted>();
                Dictionary<string, List<string>> dayList = new Dictionary<string, List<string>>();
                Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

                ProcessedStudentFile file = ProcessStudentFile(studentFile);
                studentList = file.studentList;
                subjectWithStudentCount = file.subjectWithStudentCount;

                ProcessedScheduleFile file2 = ProcessScheduleFile(scheduldeFile, subjectWithStudentCount);
                dayList = file2.dayList;
                dayListWithSlots = file2.dayListWithSlots;

                //ApplyChange
                if (changeList != "")
                {
                    ProcessedScheduleFile applied = ApplyChangeToList(dayList, dayListWithSlots, subjectWithStudentCount, changeList);
                    dayList = applied.dayList;
                    dayListWithSlots = applied.dayListWithSlots;
                }

                var cantChange = false;

                foreach (var stu in studentList)
                {
                    var numberOfSub = 0;
                    foreach (var subStu in stu.Subjects)
                    {
                        if (dayListWithSlots[dayChange][slotChange].ContainsKey(subStu))
                        {
                            numberOfSub = numberOfSub + 1;
                        }
                        if (subStu.Equals(subjectChoose))
                        {
                            numberOfSub = numberOfSub + 1;
                        }
                    }
                    if (numberOfSub >= 2)
                    {
                        cantChange = true;
                        break;
                    }
                }
                if (cantChange == false)
                {
                    return Json(new { success = true, changeAvailable = true, });
                }
                return Json(new { success = true, changeAvailable = false, });
            }
            return Json(new { success = false });
        }

        public ActionResult ExportModifyExcel(string changeList)
        {
            if (Request.Files.Count > 1)
            {
                var studentFile = Request.Files["student-file"];
                var scheduldeFile = Request.Files["schedule-file"];

                Dictionary<string, int> subjectWithStudentCount = new Dictionary<string, int>();
                List<StudentSorted> studentList = new List<StudentSorted>();
                Dictionary<string, List<string>> dayList = new Dictionary<string, List<string>>();
                Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

                ProcessedStudentFile file = ProcessStudentFile(studentFile);
                studentList = file.studentList;
                subjectWithStudentCount = file.subjectWithStudentCount;

                ProcessedScheduleFile file2 = ProcessScheduleFile(scheduldeFile, subjectWithStudentCount);
                dayList = file2.dayList;
                dayListWithSlots = file2.dayListWithSlots;

                //ApplyChange
                if (changeList != "")
                {
                    ProcessedScheduleFile applied = ApplyChangeToList(dayList, dayListWithSlots, subjectWithStudentCount, changeList);
                    dayList = applied.dayList;
                    dayListWithSlots = applied.dayListWithSlots;
                }

                MemoryStream ms = new MemoryStream();
                var fileName = "LichThiMoi";
                using (ExcelPackage packageExport = new ExcelPackage(ms))
                {
                    #region Excel format
                    ExcelWorksheet ws2 = packageExport.Workbook.Worksheets.Add("All days");
                    char StartHeaderChar = 'A';
                    int StartHeaderNumber = 1;
                    #region Headers
                    //ws.Cells[0, 0].Style.WrapText = true;
                    //Image img = CaptstoneProject.Properties.Resources.img_logo_fe;
                    //ExcelPicture pic = ws.Drawings.AddPicture("FPTLogo", img);
                    //pic.From.Column = 0;
                    //pic.From.Row = 0;
                    //pic.SetSize(320, 240);
                    //ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "No";
                    ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Day";
                    ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subjects";
                    ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "NumberOfStudent";

                    var EndHeaderChar = --StartHeaderChar;
                    var EndHeaderNumber = StartHeaderNumber;
                    StartHeaderChar = 'A';
                    StartHeaderNumber = 1;
                    #endregion
                    #region Header styling
                    ws2.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                    ":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Font.Bold = true;


                    //StartHeaderNumber++;
                    #endregion
                    #region Set values for available fields
                    foreach (var item in dayList)
                    {
                        ws2.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = item.Key;
                        var subjects = "";
                        var num = 0;
                        foreach (var sub in item.Value)
                        {
                            if (subjects == "")
                            {
                                subjects += sub;
                            }
                            else
                            {
                                subjects += ", " + sub;
                            }
                            num += subjectWithStudentCount[sub];
                        }
                        ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = subjects;
                        ws2.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = num;
                        StartHeaderChar = 'A';
                    }
                    foreach (var item in dayListWithSlots)
                    {
                        ExcelWorksheet ws3 = packageExport.Workbook.Worksheets.Add(item.Key);
                        StartHeaderChar = 'A';
                        StartHeaderNumber = 1;
                        #region Headers
                        ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Slot";
                        ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Subjects";
                        ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "NumberOfStudent";
                        EndHeaderChar = --StartHeaderChar;
                        EndHeaderNumber = StartHeaderNumber;
                        StartHeaderChar = 'A';
                        StartHeaderNumber = 1;
                        #endregion
                        #region Header styling
                        ws3.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                        ":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Font.Bold = true;


                        //StartHeaderNumber++;
                        #endregion
                        foreach (var slot in item.Value)
                        {
                            //ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                            ws3.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = slot.Key;
                            var num = 0;
                            string subjects = "";
                            foreach (var sub in slot.Value)
                            {
                                if (subjects == "")
                                {
                                    subjects += sub.Key;
                                    //subjects += sub.Key + " (" + sub.Value + ")";
                                }
                                else
                                {
                                    subjects += ", " + sub.Key;
                                    //subjects += ", " + sub.Key + " (" + sub.Value + ")";
                                }
                                num += subjectWithStudentCount[sub.Key];
                            }
                            ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = subjects;
                            ws3.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = num;
                            StartHeaderChar = 'A';
                            ws3.Cells.AutoFitColumns();
                        }
                    }
                    fileName += ".xlsx";

                    StartHeaderNumber = 1;
                    ws2.Cells.AutoFitColumns();
                    //ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                    //":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    #endregion

                    #endregion

                    packageExport.SaveAs(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return this.File(ms, contentType, fileName);
                }

            }
            return null;
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

    public class SubjectForScheduling
    {
        public string Subject { get; set; }
        public int NumberOfStudent { get; set; }
    }

    public class StudentSorted
    {
        public string RollNumber { get; set; }
        public List<String> Subjects { get; set; }
        public int NumberOfSubjects { get; set; }
    }

    public class Slot
    {
        public int NumberOfStudent { get; set; }
        public int SlotNumber { get; set; }
    }

    public class ProcessedStudentFile
    {
        public List<StudentSorted> studentList { get; set; }
        public Dictionary<string, int> subjectWithStudentCount { get; set; }
    }

    public class ProcessedScheduleFile
    {
        public Dictionary<string, List<string>> dayList { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> dayListWithSlots { get; set; }
        public bool confict { get; set; }
    }
}