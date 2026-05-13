using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Models
{
    public class NavbarItemMessage
    {
        public int Id { get; set; }
        public string nameHeader { get; set; }
        public string reference { get; set; }
        public string message { get; set; }
        public string href { get; set; }
    }
}