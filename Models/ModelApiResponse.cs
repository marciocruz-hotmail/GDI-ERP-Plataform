using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Models
{
    public class ModelApiResponse
    {
        public bool SucessoRobo { get; set; }
        public string MsgErro { get; set; }
        public string RetornoRobo { get; set; }
        public ModelApiResponse()
        {
            SucessoRobo = false;
            MsgErro = string.Empty;
            RetornoRobo = string.Empty;
        }
    }
}