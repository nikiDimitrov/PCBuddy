using PCBuddy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCBuddy.Models.ComputerInfo.SpecInfos
{
    public class SmartInfo
    {
        public SMARTStatusCode StatusCode { get; set; }

        public int PowerOnHours { get; set; }

        public int BadSectors { get; set; }
    }
}
