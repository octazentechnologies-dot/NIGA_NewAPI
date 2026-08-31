using System;
using System.Collections.Generic;

namespace Niga_Domain.DTOs
{
    public class PatientStatsChartsResponseModel
    {
        public string Period { get; set; } = "ALL";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public PatientStatsPieChartModel PieChart { get; set; } = new PatientStatsPieChartModel();
        public PatientStatsBarChartModel BarChart { get; set; } = new PatientStatsBarChartModel();
    }

    public class PatientStatsPieChartModel
    {
        public int Waiting { get; set; }
        public int WalkIn { get; set; }
        public int NotArrived { get; set; }
        public int EConsult { get; set; }
        public int Remaining { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
        public List<int> Series { get; set; } = new List<int>();
    }

    public class PatientStatsBarChartModel
    {
        public List<string> Months { get; set; } = new List<string>();
        public List<int> Waiting { get; set; } = new List<int>();
        public List<int> WalkIn { get; set; } = new List<int>();
        public List<int> NotArrived { get; set; } = new List<int>();
        public List<int> EConsult { get; set; } = new List<int>();
        public List<int> Remaining { get; set; } = new List<int>();
        public List<int> Completed { get; set; } = new List<int>();
    }
}
