using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe
{
    public class ItemNFP
    {
        public ItemNFP()
        {
            impostos = new Impostos();
        }
        public string cfop { get; set; }
        public string codigo { get; set; }
        public string descricao { get; set; }
        public string ncm { get; set; }
        public decimal quantidade { get; set; }
        public string unidadeMedida { get; set; }
        public decimal valorUnitario { get; set; }
        public decimal frete { get; set; }
        public decimal outrasDespesas { get; set; }
        public Impostos impostos { get; set; }
        public DeclaracaoImportacao declaracaoImportacao { get; set; }
    }
}