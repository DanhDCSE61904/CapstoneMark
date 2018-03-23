using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuGradeLib
{
    [Serializable]
    public class TeacherGrade
    {
        public string Version = "1.0";

        public string Semester;

        public string Login;

        public string Password;

        public List<SubjectClassGrade> SubjectClassGrades;
    }
}
