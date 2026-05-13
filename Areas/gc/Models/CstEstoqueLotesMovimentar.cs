using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstEstoqueLotesMovimentar
    {
        public int id_estoque_lote { get; set; }
        public decimal saldo_movimentar { get; set; }
        public CstEstoqueLotesMovimentar()
        {
            id_estoque_lote = 0;
            saldo_movimentar = 0;
        }
    }
}