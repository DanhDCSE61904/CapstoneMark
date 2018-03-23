using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CapstoneProject.Models
{
    public class TempAttendance
    {
        public string RollNumber { get; set; }
        public bool Status { get; set; }
        public string SubjectCode { get; set; }
        public string Taker { get; set; }
        public Byte? NumberOfSlots { get; set; }
        public DateTime RecordTime { get; set; }
        public bool TakeAttendance { get; set; }
    }
}