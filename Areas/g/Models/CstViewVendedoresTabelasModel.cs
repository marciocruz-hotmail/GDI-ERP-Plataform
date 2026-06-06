using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class CstViewVendedoresTabelasModel
    {
        public g_vendedores g_vendedores { get; set; }
        public CstViewVendedoresTabelasModel()
        {
            g_vendedores = new Db.g_vendedores();
        }
    }
}