using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Models
{
    public class NavbarItemAtividade
    {
        public int Id { get; set; }
        public string iconClass { get; set; }
        public string reference { get; set; }
        public string message { get; set; }
        public string href { get; set; }
    }
}