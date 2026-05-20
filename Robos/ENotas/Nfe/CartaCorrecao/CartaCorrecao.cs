using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe.CartaCorrecao
{
    public class CartaCorrecao
    {
        public string id { get; set; }
        public string ambienteEmissao { get; set; }
        public int numero { get; set; }
        public string correcao { get; set; }
        public Nfe nfe { get; set; }

        public CartaCorrecao()
        {
            nfe = new Nfe();
        }
    }

    public class Nfe
    {
        public string id { get; set; }
    }

}