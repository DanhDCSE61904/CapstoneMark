using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CapstoneProject.Areas.Mark.Controllers
{
    public class MarkAverageController : Controller
    {
        // GET: Mark/MarkAverage
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CalculateAverage()
        {
            using (var context = new CapstoneProjectEntities())
            {
                var semester = context.RealSemesters.Where(q => q.Semester.Equals("FALL2017"))
        .FirstOrDefault();
                var marks = context.Marks.Where(q => !q.Subject_MarkComponent.MarkComponent.Name.ToUpper().Equals("AVERAGE") && q.SemesterId == semester.Id).ToList();
                Dictionary<String, StudentInCourse> studentInCourses = new Dictionary<string, StudentInCourse>();

                foreach (var mark in marks)
                {
                    var key = mark.StudentId.ToString() + "_" + mark.CourseId.ToString();
                    if (!studentInCourses.ContainsKey(key))
                    {
                        StudentInCourse newSic = new StudentInCourse();
                        MarkWithComp mwc = new MarkWithComp();
                        mwc.mark = mark.AverageMark;
                        mwc.smc = mark.Subject_MarkComponent;
                        List<MarkWithComp> markList = new List<MarkWithComp>();
                        newSic.studentE = mark.Student;
                        newSic.courseE = mark.Course;
                        markList.Add(mwc);
                        newSic.markList = markList;



                    }
                    else
                    {

                    }

                }

            }
            return null;

        }

        //Class for student to keep mark
        public class StudentInCourse
        {
            public Student studentE { get; set; }
            public Course courseE { get; set; }
            public List<MarkWithComp> markList { get; set; }
            public double average { get; set; }

        }
        public class MarkWithComp
        {
            public double? mark { get; set; }
            public Subject_MarkComponent smc { get; set; }

        }
    }
}