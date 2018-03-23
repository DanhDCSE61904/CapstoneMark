using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuGradeLib
{
    [Serializable]
    public class SubjectClassGrade
    {
        public string Subject;

        public string Class;

        public List<Student> Students;

        public List<string> Components;
    }
}
