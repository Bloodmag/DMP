using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class ScheduleEntry
    {
        public int Priority { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string Path { get; set; }
    }
}
