using System.Collections.Generic;

namespace GdiPlataform.Areas.crm.Models
{
    public class CstListaPedidosPortal
    {
        public List<CstDadosPedidoPortal> ListaPedidos { get; set; }

        public CstListaPedidosPortal()
        {
            ListaPedidos = new List<CstDadosPedidoPortal>();
        }
    }
}
