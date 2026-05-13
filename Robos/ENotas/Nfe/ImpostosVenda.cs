using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe
{
    public class ImpostosVenda
    {
        public Imposto_ICMS icms { get; set; }
        public Imposto_PIS pis { get; set; }
        public Imposto_COFINS cofins { get; set; }
        public Imposto_PercentualAproximado percentualAproximadoTributos { get; set; }
    }
}