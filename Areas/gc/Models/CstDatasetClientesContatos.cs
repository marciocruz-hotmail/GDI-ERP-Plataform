using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstDatasetClientesContatos
    {
        public int id_cliente_contato { get; set; }
        public int id_cliente { get; set; }
        public string contato { get; set; }
        public string telefone { get; set; }
        public string email { get; set; }
    }
}