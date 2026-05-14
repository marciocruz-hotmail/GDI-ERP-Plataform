using System.Collections.Generic;

namespace GdiPlataform.Areas.crm.Models
{
    public class cstListaPedidosPortal
    {
        public List<cstDadosPedidoPortal> ListaPedidos { get; set; }

        public cstListaPedidosPortal()
        {
            ListaPedidos = new List<cstDadosPedidoPortal>();
        }
    }
}
