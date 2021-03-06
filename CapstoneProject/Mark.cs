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
    
    public partial class Mark
    {
        public int Id { get; set; }
        public Nullable<int> SubjectMarkComponentId { get; set; }
        public Nullable<int> StudentId { get; set; }
        public Nullable<int> SemesterId { get; set; }
        public Nullable<int> CourseId { get; set; }
        public Nullable<double> AverageMark { get; set; }
        public string Status { get; set; }
        public Nullable<bool> IsActivated { get; set; }
        public Nullable<bool> IsEnabled { get; set; }
        public string Comment { get; set; }
        public Nullable<bool> IsExempt { get; set; }
    
        public virtual Course Course { get; set; }
        public virtual RealSemester RealSemester { get; set; }
        public virtual Student Student { get; set; }
        public virtual Subject_MarkComponent Subject_MarkComponent { get; set; }
    }
}
