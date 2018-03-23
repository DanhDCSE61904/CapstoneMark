using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuGradeLib
{
    [Serializable]
    public class Student : IComparable<Student>
    {
        public string Roll { get; set; }

        public string Name { get; set; }

        public List<GradeComponent> Grades { get; set; }

        public string Comment { get; set; }

        public int CompareTo(Student compareStudent)
        {
            if (compareStudent == null)
            {
                return 1;
            }
            return this.Roll.CompareTo(compareStudent.Roll);
        }
    }
}
