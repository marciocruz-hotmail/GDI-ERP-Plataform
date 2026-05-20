using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe
{
    public class Impostos
    {
        public Imposto_ICMS icms { get; set; }
        public Imposto_PIS pis { get; set; }
        public Imposto_COFINS cofins { get; set; }
        public Imposto_IPI ipi { get; set; }
        public Imposto_II ii { get; set; }
        public Imposto_PercentualAproximado percentualAproximadoTributos { get; set; }
    }

    public class Imposto_ICMS
    {
        public Imposto_ICMS()
        {
            situacaoTributaria = string.Empty;
            origem = 0;
            aliquota = 0;
            baseCalculo = 0;
            modalidadeBaseCalculo = 0;
            percentualReducaoBaseCalculo = 0;
            valor = 0;

            // Difal
            baseCalculoUFDestinoDifal = 0;
            aliquotaUFDestinoDifal = 0;
            valorUFDestinoDifal = 0;
            valorUFOrigemDifal = 0;
            aliquotaInterestadualDifal = 0;
            percentualPartilhaInterestadualDifal = 0;
            baseCalculoFundoCombatePobrezaDifal = 0;
            percentualFCPDifal = 0;
            valorFCPDifal = 0;
        }

        public string situacaoTributaria { get; set; }
        public int origem { get; set; }
        public decimal aliquota { get; set; }
        public decimal baseCalculo { get; set; }
        public int modalidadeBaseCalculo { get; set; }
        public decimal percentualReducaoBaseCalculo { get; set; }
        public decimal valor { get; set; }



        // Difal
        public decimal baseCalculoUFDestinoDifal { get; set; }
        public decimal aliquotaUFDestinoDifal { get; set; }
        public decimal valorUFDestinoDifal { get; set; }
        public decimal valorUFOrigemDifal { get; set; }
        public decimal aliquotaInterestadualDifal { get; set; }
        public decimal percentualPartilhaInterestadualDifal { get; set; }
        public decimal baseCalculoFundoCombatePobrezaDifal { get; set; }
        public decimal percentualFCPDifal { get; set; }
        public decimal valorFCPDifal { get; set; }
    }

    public class Imposto_PIS
    {
        public string situacaoTributaria { get; set; }
        public int origem { get; set; }
        public Imposto_PercentualPorAliquota porAliquota { get; set; }
    }

    public class Imposto_COFINS
    {
        public string situacaoTributaria { get; set; }
        public int origem { get; set; }
        public decimal valor { get; set; }
        public Imposto_PercentualPorAliquota porAliquota { get; set; }
    }

    public class Imposto_IPI
    {
        public string situacaoTributaria { get; set; }
        public int origem { get; set; }
        public Imposto_PercentualPorAliquota porAliquota { get; set; }
    }

    public class Imposto_II
    {
        public decimal despesasAduaneiras { get; set; }
        public decimal valor { get; set; }
        public decimal iof { get; set; }
    }

    public class Imposto_PercentualAproximado
    {
        public Imposto_PercentualAproximado()
        {
            Imposto_PercentualAproximado_Simplificado simplificado = new Imposto_PercentualAproximado_Simplificado();
        }
        public Imposto_PercentualAproximado_Simplificado simplificado { get; set; }
        public string fonte { get; set; }
    }

    public class Imposto_PercentualAproximado_Simplificado
    {
        public decimal percentual { get; set; }
    }
    public class Imposto_PercentualPorAliquota
    {
        public decimal aliquota { get; set; }
    }
}