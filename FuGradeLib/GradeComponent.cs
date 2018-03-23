using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuGradeLib
{
    [Serializable]
    public class GradeComponent
    {
        public string Component { get; set; }

        public float? Grade { get; set; }
    }
}
