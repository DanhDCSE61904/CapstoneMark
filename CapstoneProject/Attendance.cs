//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CapstoneProject
{
    using System;
    using System.Collections.Generic;
    
    public partial class Attendance
    {
        public int Id { get; set; }
        public Nullable<int> StudentId { get; set; }
        public Nullable<int> CourseId { get; set; }
        public string Taker { get; set; }
        public Nullable<bool> Status { get; set; }
        public Nullable<System.DateTime> RecordTime { get; set; }
        public Nullable<bool> TakeAttendance { get; set; }
        public Nullable<int> NumberOfSlots { get; set; }
    
        public virtual Course Course { get; set; }
        public virtual Student Student { get; set; }
    }
}