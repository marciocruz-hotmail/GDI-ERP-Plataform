using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe.CartaCorrecao
{
    public class CartaCorrecaoRetorno
    {
        public string id { get; set; }
        public string ambienteEmissao { get; set; }
        public string status { get; set; }
        public string motivoStatus { get; set; }
        public int numero { get; set; }
        public string correcao { get; set; }

        public string condicoesUso { get; set; }
        public NfeRetorno nfe { get; set; }

        public string protocoloAutorizacao { get; set; }
        public string dataCriacao { get; set; }
        public string dataUltimaAlteracao { get; set; }
        public CartaCorrecaoRetorno()
        {
            nfe = new NfeRetorno();
        }
    }

    public class NfeRetorno
    {
        public string id { get; set; }
        public string chaveAcesso { get; set; }
    }
}