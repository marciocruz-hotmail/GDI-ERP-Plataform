using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstPedido
    {
        public Nullable<System.DateTime> data_vencimento { get; set; }
        public int edit_local_estoque { get; set; }
        public int id_cliente { get; set; }
        public string descricao { get; set; }
        public string tipo_movimento { get; set; }
        public string[][] registros { get; set; }
    }
}