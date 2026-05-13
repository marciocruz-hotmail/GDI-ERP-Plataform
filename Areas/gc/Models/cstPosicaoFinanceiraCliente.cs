using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstPosicaoFinanceiraCliente
    {
        public int IdCliente { get; set; }
        public bool PedidoBloqueado { get; set; }
        public string MotivoBloqueio { get; set; }
        public decimal LimiteCreditoTotal { get; set; }
        public decimal LimiteCreditoUtilizado { get; set; }
        public decimal LimiteCreditoRestante { get; set; }
        public decimal TitulosAVencerQtd { get; set; }
        public decimal TitulosAVencerValor { get; set; }
        public decimal TitulosVencidosQtd { get; set; }
        public decimal TitulosVencidosValor { get; set; }
        public decimal TitulosNegociacaoQtd { get; set; }
        public decimal TitulosNegociacaoValor { get; set; }
    }
}