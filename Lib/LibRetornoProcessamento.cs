using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public class LibRetornoProcessamento
    {
        public Boolean Sucesso { get; set; }
        public String MsgProcessamento { get; set; }
        public LibRetornoProcessamento()
        {
            Sucesso = false;
            MsgProcessamento = string.Empty;
        }
    }
}