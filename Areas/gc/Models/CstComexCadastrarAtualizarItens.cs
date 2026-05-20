using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstComexCadastrarAtualizarItens
    {
        public bool ErroProcesamento { get; set; }
        public string MsgErroProcesamento { get; set; }

        public List<CstModelComexItemImportacao> ListaPlanilhaItens { get; set; }

        public List<gc_comex_importacoes_itens> ListaComexItens { get; set; }

        public CstComexCadastrarAtualizarItens()
        {
            ErroProcesamento = false;
            MsgErroProcesamento = string.Empty;
            ListaPlanilhaItens = new List<CstModelComexItemImportacao>();
            ListaComexItens = new List<gc_comex_importacoes_itens>();
        }
    }
}