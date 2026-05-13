using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstComexCadastrarAtualizarItens
    {
        public bool ErroProcesamento { get; set; }
        public string MsgErroProcesamento { get; set; }

        public List<cstModelComexItemImportacao> ListaPlanilhaItens { get; set; }

        public List<gc_comex_importacoes_itens> ListaComexItens { get; set; }

        public cstComexCadastrarAtualizarItens()
        {
            ErroProcesamento = false;
            MsgErroProcesamento = string.Empty;
            ListaPlanilhaItens = new List<cstModelComexItemImportacao>();
            ListaComexItens = new List<gc_comex_importacoes_itens>();
        }
    }
}