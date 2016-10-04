using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace John.FCULab.Bot.Sample.AttendanceSystem.Models
{
    [Serializable]
    public class UserModel
    {
        public string name { get; set; }
        public string id { get; set; }
        public string gendar { get; set; }
        public int PersonalLeave { get; set; }
        public int SickLeave { get; set; }
        public int FuneralLeave { get; set; }
        public int MenstrualLeave { get; set; }
    }
}