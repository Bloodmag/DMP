using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Core.Controllers
{
    static class ScheduleParser
    {
        private static Models.ScheduleEntry ParseLine(string line)
        {
            var ret = new Models.ScheduleEntry();
            line = line.ToLower().Trim();
            if (line.StartsWith("background"))
            {
                ret.Priority = 1;
                var words = line.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                TimeSpan t;
                if (!TimeSpan.TryParse(words[1], out t)) return null;
                ret.Start = t;
                if (!TimeSpan.TryParse(words[2], out t)) return null;
                ret.End = t;
                ret.Path = words[3].Trim().Replace("\"","");
                return ret;
            }
            if(line.Contains("interrupt"))
            {
                ret.Priority = 0;
                var words = line.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                TimeSpan t;
                if (!TimeSpan.TryParse(words[1], out t)) return null;
                ret.Start = t;
                ret.Path = words[2].Trim().Replace("\"", "");
                return ret;
            }
            return null;
        }
        public static List<Models.ScheduleEntry> ParseTxt(FileInfo file)
        {
            var ret = new List<Models.ScheduleEntry>();
            try
            {
                //fill list from string
                string line;
                var reader = new StringReader(File.ReadAllText(file.FullName));
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        var entry = ParseLine(line);
                        if (entry != null)
                            ret.Add(entry);
                    }
                }
                while (line != null);
                //Check list
                if (!ret.Any()) return null;
                ret = ret.OrderBy(x => x.Start).ToList();
                var inter = ret.Where(x => x.Priority == 0).ToList();
                var back = ret.Where(x => x.Priority == 1).ToList();
                for(int i =1;i<back.Count;i++)
                {
                    if (back[i - 1].End > back[i].Start)
                        return null;
                }
                for (int i = 1; i < inter.Count; i++)
                {
                    if (inter[i - 1].Start == inter[i].Start)
                        return null;
                }
                return ret;
            }
            catch
            {
                return null;
            }
        }
    }
}
