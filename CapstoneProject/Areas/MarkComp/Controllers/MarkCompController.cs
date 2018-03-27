using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CapstoneProject.Areas.MarkComp.Controllers
{
    public class MarkCompController : Controller
    {
        // GET: MarkComp/MarkComp
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ImportSyllabus()
        {
            return View();
        }

        public ActionResult DownloadTemplate()
        {
            MemoryStream ms = new MemoryStream();

            using (var context = new CapstoneProjectEntities())
            {
                //var course = context.Courses.Find(courseId);
                var fileName = "";

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
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentMajor ID";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "StudentMajor login";
                    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = "Name";


                    var EndHeaderChar = --StartHeaderChar;
                    var EndHeaderNumber = StartHeaderNumber;
                    StartHeaderChar = 'A';
                    StartHeaderNumber = 1;
                    #endregion
                    #region Header styling
                    ws.Cells["" + StartHeaderChar + StartHeaderNumber.ToString() +
                    ":" + EndHeaderChar + EndHeaderNumber.ToString()].Style.Font.Bold = true;


                    StartHeaderNumber++;
                    #endregion
                    #region Set values for available fields
                    var count = 1;
                    //foreach (var StudentMajor in course.StudentInCourses)
                    //{
                    //    ws.Cells["" + (StartHeaderChar++) + (++StartHeaderNumber)].Value = count++;
                    //    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = StudentMajor.StudentMajor.StudentCode;
                    //    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = StudentMajor.StudentMajor.LoginName;
                    //    ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = StudentMajor.StudentMajor.Student.Name;
                    //    foreach (var mark in StudentMajor.StudentCourseMarks)
                    //    {
                    //        if (!mark.CourseMark.IsFinal.HasValue || mark.CourseMark.IsFinal != true)
                    //        {
                    //            ws.Cells["" + (StartHeaderChar++) + (StartHeaderNumber)].Value = mark.Mark.HasValue && mark.Mark.Value != -1 ? mark.Mark.Value.ToString("#.##") : "";
                    //        }
                    //    }
                    //    StartHeaderChar = 'A';
                    //}
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


        public ActionResult UploadExcel()
        {
            var failRecordCount = 0;

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
                                    var studentCodeCol = 2;
                                    var titleRow = 1;
                                    var firstRecordRow = 3;

                                    Dictionary<String, Subject_MarkComponent> dic = new Dictionary<string, Subject_MarkComponent>();

                                    int tempNo = 0;
                                    for (int i = firstRecordRow; i < totalRow; i++)
                                    {
                                        //Add MarkComponent
                                        //    var markCompName = ws.Cells[i, 7].Text.ToUpper();
                                        //    if (!ws.Cells[i, 7].Text.ToUpper().Contains("PHASE"))
                                        //    {
                                        //        markCompName = new String(ws.Cells[i, 7].Text.Where(c => (c < '0' || c > '9')).ToArray()).ToUpper();
                                        //    }
                                        //    var markCompExist = context.MarkComponents.Where(q => q.Name.Equals(markCompName)).FirstOrDefault();
                                        //    if (markCompExist == null)
                                        //    {
                                        //        MarkComponent newMarkComp = new MarkComponent();
                                        //        newMarkComp.Name = markCompName;
                                        //        context.MarkComponents.Add(newMarkComp);
                                        //        context.SaveChanges();
                                        //    }
                                        //    else
                                        //    {
                                        //        Console.WriteLine();
                                        //    }
                                        //}

                                        //Add Syllabus
                                        String tempOldMarkName = "";

                                        //if (!ws.Cells[i, 7].Text.ToUpper().Contains("PHASE"))
                                        //{
                                        //    tempOldMarkName = new String(ws.Cells[i, 7].Text.Where(c => (c < '0' || c > '9')).ToArray());
                                        //}
                                        // 1: SubjectCode, 2: Syllabus Name, 3: MarkName
                                        String tempKey = ws.Cells[i, 1].Text.ToUpper() + "_" + ws.Cells[i, 2].Text.ToUpper() + "_" + ws.Cells[i, 7].Text.ToUpper() + "_" + tempOldMarkName;

                                        //if (ws.Cells[i, 3].Text.ToUpper().Contains("FINAL")|| ws.Cells[i, 3].Text.ToUpper().Contains("MIDTERM") || ws.Cells[i, 3].Text.ToUpper().Contains("MT"))
                                        //{
                                        //    tempKey = tempKey + "_" + ws.Cells[i, 7].Text;
                                        //}

                                        if (!dic.ContainsKey(tempKey))
                                        {
                                            var markCompName = ws.Cells[i, 7].Text.ToUpper();
                                            if (!ws.Cells[i, 7].Text.ToUpper().Contains("PHASE"))
                                            {
                                                markCompName = new String(ws.Cells[i, 7].Text.Where(c => (c < '0' || c > '9')).ToArray()).ToUpper();
                                            }
                                            bool tempGoing = false;
                                            if (ws.Cells[i, 4].Text.Equals("1"))
                                            {
                                                tempGoing = true;
                                            }
                                            bool tempActive = false;
                                            if (ws.Cells[i, 10].Text.Equals("1"))
                                            {
                                                tempActive = true;
                                            }
                                            Subject_MarkComponent subMark = new Subject_MarkComponent();
                                            var markCompExist = context.MarkComponents.Where(q => q.Name.ToUpper().Equals(markCompName)).FirstOrDefault();
                                            if (markCompExist == null)
                                            {
                                                Console.WriteLine();
                                            }
                                            subMark.MarkComponentId = markCompExist.Id;
                                            subMark.SyllabusName = ws.Cells[i, 2].Text;
                                            subMark.SubjectId = ws.Cells[i, 1].Text.ToUpper();
                                            subMark.NumberOfTests = int.Parse(ws.Cells[i, 5].Text.ToUpper());
                                            subMark.PercentWeight = double.Parse(ws.Cells[i, 8].Text.ToUpper());
                                            subMark.Name = ws.Cells[i, 1].Text.ToUpper() + "_" + markCompExist.Name.ToUpper();
                                            subMark.IsActive = tempActive;
                                            subMark.IsOngoing = tempGoing;

                                            subMark.MarkName = ws.Cells[i, 7].Text;

                                            dic.Add(tempKey, subMark);
                                        }

                                    }
                                    foreach (var item in dic)
                                    {
                                        context.Subject_MarkComponent.Add(item.Value);
                                    }
                                    context.SaveChanges();

                                }
                            }
                        }
                    }
                }
                else
                {
                    return Json(new { success = false, message = "No file has been uploaded" });
                }
            }
            catch (Exception e)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { error = e.Message, message = "Errors in uploaded file. Please recheck" });
            }

            return Json(new { success = true, message = "File uploaded successfully", failRecordCount = failRecordCount });
        }
    }
}