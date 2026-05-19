using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstPedidoSeparacao
    {
        public gc_movimentos RecordPedido { get; set; }
        public List<gc_movimentos_itens> ListaItensPedido { get; set; }

        public CstPedidoSeparacao()
        {
            RecordPedido = new Db.gc_movimentos();
            ListaItensPedido = new List<gc_movimentos_itens>();
        }
    }
}