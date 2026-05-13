using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe
{
    public class DeclaracaoImportacao
    {
        public string numero { get; set; }
        public string data { get; set; }
        public string localDesembaraco { get; set; }
        public string ufDesembaraco { get; set; }
        public string dataDesembaraco { get; set; }
        public string tipoViaTransporte { get; set; }
        public decimal valorAFRMM { get; set; }
        public string tipoIntermedio { get; set; }
        public string cnpj { get; set; }
        public string ufTerceiro { get; set; }
        public string codigoExportador { get; set; }
        public List<DeclaracaoImportacaoAdicoes> adicoes { get; set; }
    }

    public class DeclaracaoImportacaoAdicoes
    {
        public int numero { get; set; }
        public string codigoFabricante { get; set; }
        public string numeroDrawback { get; set; }
        public decimal descontos { get; set; }
    }
}