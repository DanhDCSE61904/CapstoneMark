﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CapstoneProject.Models
{
    public class AreaViewModel
    {
        public class SelectorViewModel
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }

        public class MarkGroupModel
        {
            public string MarkGroupName { get; set; }
            public double? Mark { get; set; }
            public double? Weight { get; set; }
            public int? NumberOfTest { get; set; }
        }

        public class SemesterViewModel
        {
            public int SemesterId { get; set; }
            public string Semester { get; set; }
        }
        public class MarkViewModel
        {
            public string RollNumber { get; set; }
            public string StudentName { get; set; }
            public string Subject { get; set; }
            public string Component { get; set; }
            public string Mark { get; set; }
        }

        public class JQueryDataTableParamModel
        {
            /// <summary>
            /// Request sequence number sent by DataTable,
            /// same value must be returned in response
            /// </summary>       
            public string sEcho { get; set; }

            /// <summary>
            /// Text used for filtering
            /// </summary>
            public string sSearch { get; set; }

            /// <summary>
            /// Number of records that should be shown in table
            /// </summary>
            public int iDisplayLength { get; set; }

            /// <summary>
            /// First record that should be shown(used for paging)
            /// </summary>
            public int iDisplayStart { get; set; }

            /// <summary>
            /// Number of columns in table
            /// </summary>
            public int iColumns { get; set; }

            /// <summary>
            /// Number of columns that are used in sorting
            /// </summary>
            public int iSortingCols { get; set; }

            /// <summary>
            /// Comma separated list of column names
            /// </summary>
            public string sColumns { get; set; }

            /// <summary>
            /// Sort column
            /// </summary>
            public int iSortCol_0 { get; set; }

            /// <summary>
            /// Asc or Desc
            /// </summary>
            public string sSortDir_0 { get; set; }
        }
    }
}