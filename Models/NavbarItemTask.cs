using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Models
{
    public class NavbarItemTask
    {
        public int Id { get; set; }
        public String Name { get; set; }
        public string Message { get; set; }
        public string progressBarType { get; set; }
        public int valueMin { get; set; }
        public int valueMax { get; set; }
        public int ValueNow { get; set; }
        public string href { get; set; }
    }
}