using GdiPlataform.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstComexEntradaMateriaisItens
    {
        public int id_invoice_item { get; set; }
        public int quantidade { get; set; }
        public string descricao { get; set; }
        public string serial { get; set; }
        public string cliente { get; set; }
        public string os { get; set; }
        public string obs { get; set; }
        public string lotes { get; set; }
        public int qtd_recebido { get; set; }
        public bool recebido { get; set; }
        public gc_comex_importacoes_itens_lotes lista_lotes { get; set; }

        public CstComexEntradaMateriaisItens()
        {
            id_invoice_item = 0;
            quantidade = 0;
            descricao = string.Empty;
            serial = string.Empty;
            cliente = string.Empty;
            os = string.Empty;
            obs = string.Empty;
            lotes = string.Empty;
            qtd_recebido = 0;
            recebido = false;
            lista_lotes = new Db.gc_comex_importacoes_itens_lotes();
        }
    }
}