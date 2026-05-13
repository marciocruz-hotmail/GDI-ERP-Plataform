using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstModelComexItemImportacao
    {
        public string String_NfNumero { get; set; }
        public string String_DiNumero { get; set; }
        public string String_LiNumero { get; set; }
        public string String_DiData { get; set; }
        public string String_PN { get; set; }
        public string String_PN_Auxiliar { get; set; }
        public string String_PN_Variacao1 { get; set; }
        public string String_PN_Variacao2 { get; set; }
        public string String_Descricao { get; set; }
        public string String_DiAdicaoNumero { get; set; }
        public string String_DiAdicaoSequencial { get; set; }
        public string String_NcmCodigo { get; set; }
        public string String_ValorFob { get; set; }
        public string String_ValorFrete { get; set; }
        public string String_Quantidade { get; set; }
        public string String_UnidadeMedida { get; set; }
        public string String_ValorUnit { get; set; }
        public string String_ValorTotal { get; set; }
        public string String_PesoLiquido { get; set; }
        public string String_PesoBruto { get; set; }
        public string String_IiPercentual { get; set; }
        public string String_IiValor { get; set; }
        public string String_IpiBaseCalculo { get; set; }
        public string String_IpiPercentual { get; set; }
        public string String_IpiValor { get; set; }
        public string String_IcmsBaseCalculo { get; set; }
        public string String_IcmsBaseReduzida { get; set; }
        public string String_IcmsPercentual { get; set; }
        public string String_IcmsValor { get; set; }
        public string String_PisBaseCalculo { get; set; }
        public string String_PisPercentual { get; set; }
        public string String_PisValor { get; set; }
        public string String_CofinsBaseCalculo { get; set; }
        public string String_CofinsPercentual { get; set; }
        public string String_CofinsValor { get; set; }
        public string String_SiscomexValor { get; set; }
        public string String_SdaValor { get; set; }
        public string String_IbsCbsCst { get; set; }
        public string String_cClassTrib { get; set; }
        public string String_IbsCbsBaseCalculo { get; set; }
        public string String_IbsPercentual { get; set; }
        public string String_IbsValor { get; set; }
        public string String_CbsPercentual { get; set; }
        public string String_CbsValor { get; set; }
        public string String_MarinhaValor { get; set; }
        public int Int_DiAdicaoNumero { get; set; }
        public int Int_DiAdicaoSequencial { get; set; }
        public decimal Decimal_ValorFob { get; set; }
        public decimal Decimal_ValorFrete { get; set; }
        public decimal Decimal_Quantidade { get; set; }
        public decimal Decimal_ValorUnit { get; set; }
        public decimal Decimal_ValorTotal { get; set; }
        public decimal Decimal_PesoLiquido { get; set; }
        public decimal Decimal_PesoBruto { get; set; }
        public decimal Decimal_IiPercentual { get; set; }
        public decimal Decimal_IiValor { get; set; }
        public decimal Decimal_IpiBaseCalculo { get; set; }
        public decimal Decimal_IpiPercentual { get; set; }
        public decimal Decimal_IpiValor { get; set; }
        public decimal Decimal_IcmsBaseCalculo { get; set; }
        public decimal Decimal_IcmsBaseReduzida { get; set; }
        public decimal Decimal_IcmsPercentual { get; set; }
        public decimal Decimal_IcmsValor { get; set; }
        public decimal Decimal_PisBaseCalculo { get; set; }
        public decimal Decimal_PisPercentual { get; set; }
        public decimal Decimal_PisValor { get; set; }
        public decimal Decimal_CofinsBaseCalculo { get; set; }
        public decimal Decimal_CofinsPercentual { get; set; }
        public decimal Decimal_CofinsValor { get; set; }
        public decimal Decimal_SiscomexValor { get; set; }
        public decimal Decimal_SdaValor { get; set; }
        public decimal Decimal_IbsCbsBaseCalculo { get; set; }
        public decimal Decimal_IbsPercentual { get; set; }
        public decimal Decimal_IbsValor { get; set; }
        public decimal Decimal_CbsPercentual { get; set; }
        public decimal Decimal_CbsValor { get; set; }
        public decimal Decimal_MarinhaValor { get; set; }
        public int IdProdutoERP { get; set; }
        public int IdComexProdutoERP { get; set; }
        public int IdInvoiceItemERP { get; set; }
        public bool DescricaoDivergente { get; set; }
        public bool AtualizarDescricao { get; set; }
        public bool IsGDI { get; set; }
        public bool IsSC { get; set; }
        public cstModelComexItemImportacao()
        {
            String_NfNumero = string.Empty;
            String_DiNumero = string.Empty;
            String_LiNumero = string.Empty;
            String_DiData = string.Empty;
            String_PN = string.Empty;
            String_PN_Auxiliar = string.Empty;
            String_PN_Variacao1 = string.Empty;
            String_PN_Variacao2 = string.Empty;
            String_Descricao = string.Empty;
            String_DiAdicaoNumero = string.Empty;
            String_DiAdicaoSequencial = string.Empty;
            String_NcmCodigo = string.Empty;
            String_ValorFob = string.Empty;
            String_ValorFrete = string.Empty;
            String_Quantidade = string.Empty;
            String_UnidadeMedida = string.Empty;
            String_ValorUnit = string.Empty;
            String_ValorTotal = string.Empty;
            String_PesoLiquido = string.Empty;
            String_PesoBruto = string.Empty;

            String_IiPercentual = string.Empty;
            String_IiValor = string.Empty;
            String_IpiBaseCalculo = string.Empty;
            String_IpiPercentual = string.Empty;
            String_IpiValor = string.Empty;
            String_IcmsBaseCalculo = string.Empty;
            String_IcmsBaseReduzida = string.Empty;
            String_IcmsPercentual = string.Empty;
            String_IcmsValor = string.Empty;
            String_PisBaseCalculo = string.Empty;
            String_PisPercentual = string.Empty;
            String_PisValor = string.Empty;
            String_CofinsBaseCalculo = string.Empty;
            String_CofinsPercentual = string.Empty;
            String_CofinsValor = string.Empty;
            String_SiscomexValor = string.Empty;
            String_SdaValor = string.Empty;
            String_IbsCbsCst = string.Empty;
            String_cClassTrib = string.Empty;
            String_IbsCbsBaseCalculo = string.Empty;
            String_IbsPercentual = string.Empty;
            String_IbsValor = string.Empty;
            String_CbsPercentual = string.Empty;
            String_CbsValor = string.Empty;
            String_MarinhaValor = string.Empty;

            Int_DiAdicaoNumero = 0;
            Int_DiAdicaoSequencial = 0;
            Decimal_ValorFob = 0;
            Decimal_ValorFrete = 0;
            Decimal_Quantidade = 0;
            Decimal_ValorUnit = 0;
            Decimal_ValorTotal = 0;
            Decimal_PesoLiquido = 0;
            Decimal_PesoBruto = 0;
            Decimal_IiPercentual = 0;
            Decimal_IiValor = 0;
            Decimal_IpiBaseCalculo = 0;
            Decimal_IpiPercentual = 0;
            Decimal_IpiValor = 0;
            Decimal_IcmsBaseCalculo = 0;
            Decimal_IcmsBaseReduzida = 0;
            Decimal_IcmsPercentual = 0;
            Decimal_IcmsValor = 0;
            Decimal_PisBaseCalculo = 0;
            Decimal_PisPercentual = 0;
            Decimal_PisValor = 0;
            Decimal_CofinsBaseCalculo = 0;
            Decimal_CofinsPercentual = 0;
            Decimal_CofinsValor = 0;
            Decimal_SiscomexValor = 0;
            Decimal_SdaValor = 0;
            Decimal_IbsCbsBaseCalculo = 0;
            Decimal_IbsPercentual = 0;
            Decimal_IbsValor = 0;
            Decimal_CbsPercentual = 0;
            Decimal_CbsValor = 0;
            Decimal_MarinhaValor = 0;
            IdProdutoERP = 0;
            IdComexProdutoERP = 0;
            IdInvoiceItemERP = 0;
            DescricaoDivergente = false;
            AtualizarDescricao = false;
            IsGDI = false;
            IsSC = false;
        }

        public bool IsValidItem()
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_NfNumero.EmptyIfNull().ToString().Length > 0)
            && (this.String_DiNumero.EmptyIfNull().ToString().Length > 0)
            && (this.String_DiData.EmptyIfNull().ToString().Length > 0)
            && (this.String_PN.EmptyIfNull().ToString().Length > 0)
            && (this.String_Descricao.EmptyIfNull().ToString().Length > 0)
            && (this.String_DiAdicaoNumero.EmptyIfNull().ToString().Length > 0)
            && (this.String_DiAdicaoSequencial.EmptyIfNull().ToString().Length > 0)
            && (this.String_NcmCodigo.EmptyIfNull().ToString().Length > 0)
            && (this.String_ValorFob.EmptyIfNull().ToString().Length > 0)
            && (this.String_ValorFrete.EmptyIfNull().ToString().Length > 0)
            && (this.String_Quantidade.EmptyIfNull().ToString().Length > 0)
            && (this.String_UnidadeMedida.EmptyIfNull().ToString().Length > 0)
            && (this.String_ValorUnit.EmptyIfNull().ToString().Length > 0)
            && (this.String_ValorTotal.EmptyIfNull().ToString().Length > 0)
            && (this.String_PesoLiquido.EmptyIfNull().ToString().Length > 0)
            && (this.String_PesoBruto.EmptyIfNull().ToString().Length > 0))
            {
                isValid = true;
            };

            if ((this.String_Descricao.IndexOf("SERIAL") > 0) || (this.String_Descricao.IndexOf("PREFIXO") > 0)) { isValid = true; };
            if ((isValid == true) && (LibNumbers.IsValidInteger(String_DiAdicaoNumero) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidInteger(String_DiAdicaoSequencial) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(String_ValorFob) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidInteger(String_Quantidade) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(String_ValorUnit) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(String_ValorTotal) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(String_PesoLiquido) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(String_PesoBruto) == true)) { isValid = true; } else { isValid = false; }
            if (isValid == true)
            {
                this.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(this.String_PN);
                this.String_PN_Variacao1 = this.String_PN_Auxiliar.Replace("0", "O");
                this.String_PN_Variacao2 = this.String_PN_Auxiliar.Replace("O", "0");
                this.Int_DiAdicaoNumero = int.Parse(String_DiAdicaoNumero);
                this.Int_DiAdicaoSequencial = int.Parse(String_DiAdicaoSequencial);
                this.Decimal_ValorFob = Decimal.Round(Decimal.Parse(String_ValorFob),6);
                this.Decimal_ValorFrete = Decimal.Round(Decimal.Parse(String_ValorFrete),6);
                this.Decimal_Quantidade = Decimal.Round(Decimal.Parse(String_Quantidade),0);
                this.Decimal_ValorUnit = Decimal.Round(Decimal.Parse(String_ValorUnit), 6);
                this.Decimal_ValorTotal = Decimal.Round(Decimal.Parse(String_ValorTotal), 6);
                this.Decimal_PesoLiquido = Decimal.Round(Decimal.Parse(String_PesoLiquido), 5);
                this.Decimal_PesoBruto = Decimal.Round(Decimal.Parse(String_PesoBruto), 5);

                if (LibNumbers.IsValidDecimal(String_IiPercentual) == true) { this.Decimal_IiPercentual = Decimal.Parse(String_IiPercentual); };
                if (LibNumbers.IsValidDecimal(String_IiValor) == true) { this.Decimal_IiValor = Decimal.Parse(String_IiValor); };
                if (LibNumbers.IsValidDecimal(String_IpiBaseCalculo) == true) { this.Decimal_IpiBaseCalculo = Decimal.Parse(String_IpiBaseCalculo); };
                if (LibNumbers.IsValidDecimal(String_IpiPercentual) == true) { this.Decimal_IpiPercentual = Decimal.Parse(String_IpiPercentual); };
                if (LibNumbers.IsValidDecimal(String_IpiValor) == true) { this.Decimal_IpiValor = Decimal.Parse(String_IpiValor); };
                if (LibNumbers.IsValidDecimal(String_IpiBaseCalculo) == true) { this.Decimal_IpiBaseCalculo = Decimal.Parse(String_IpiBaseCalculo); };
                if (LibNumbers.IsValidDecimal(String_IpiPercentual) == true) { this.Decimal_IpiPercentual = Decimal.Parse(String_IpiPercentual); };
                if (LibNumbers.IsValidDecimal(String_IpiValor) == true) { this.Decimal_IpiValor = Decimal.Parse(String_IpiValor); };
                if (LibNumbers.IsValidDecimal(String_IcmsBaseCalculo) == true) { this.Decimal_IcmsBaseCalculo = Decimal.Parse(String_IcmsBaseCalculo); };
                if (LibNumbers.IsValidDecimal(String_IcmsBaseReduzida) == true) { this.Decimal_IcmsBaseReduzida = Decimal.Parse(String_IcmsBaseReduzida); };
                if (LibNumbers.IsValidDecimal(String_IcmsPercentual) == true) { this.Decimal_IcmsPercentual = Decimal.Parse(String_IcmsPercentual); };
                if (LibNumbers.IsValidDecimal(String_IcmsValor) == true) { this.Decimal_IcmsValor = Decimal.Parse(String_IcmsValor); };
                if (LibNumbers.IsValidDecimal(String_PisBaseCalculo) == true) { this.Decimal_PisBaseCalculo = Decimal.Parse(String_PisBaseCalculo); };
                if (LibNumbers.IsValidDecimal(String_PisPercentual) == true) { this.Decimal_PisPercentual = Decimal.Parse(String_PisPercentual); };
                if (LibNumbers.IsValidDecimal(String_PisValor) == true) { this.Decimal_PisValor = Decimal.Parse(String_PisValor); };
                if (LibNumbers.IsValidDecimal(String_CofinsBaseCalculo) == true) { this.Decimal_CofinsBaseCalculo = Decimal.Parse(String_CofinsBaseCalculo); };
                if (LibNumbers.IsValidDecimal(String_CofinsPercentual) == true) { this.Decimal_CofinsPercentual = Decimal.Parse(String_CofinsPercentual); };
                if (LibNumbers.IsValidDecimal(String_CofinsValor) == true) { this.Decimal_CofinsValor = Decimal.Parse(String_CofinsValor); };
                if (LibNumbers.IsValidDecimal(String_SiscomexValor) == true) { this.Decimal_SiscomexValor = Decimal.Parse(String_SiscomexValor); };
                if (LibNumbers.IsValidDecimal(String_SdaValor) == true) { this.Decimal_SdaValor = Decimal.Parse(String_SdaValor); };
                if (LibNumbers.IsValidDecimal(String_IbsCbsBaseCalculo) == true) { this.Decimal_IbsCbsBaseCalculo = Decimal.Parse(String_IbsCbsBaseCalculo); };
                if (LibNumbers.IsValidDecimal(String_IbsPercentual) == true) { this.Decimal_IbsPercentual = Decimal.Parse(String_IbsPercentual); };
                if (LibNumbers.IsValidDecimal(String_IbsValor) == true) { this.Decimal_IbsValor = Decimal.Parse(String_IbsValor); };
                if (LibNumbers.IsValidDecimal(String_CbsPercentual) == true) { this.Decimal_CbsPercentual = Decimal.Parse(String_CbsPercentual); };
                if (LibNumbers.IsValidDecimal(String_CbsValor) == true) { this.Decimal_CbsValor = Decimal.Parse(String_CbsValor); };
                if (LibNumbers.IsValidDecimal(String_MarinhaValor) == true) { this.Decimal_MarinhaValor = Decimal.Parse(String_MarinhaValor); };
                }
            return isValid;
        }

        public bool IsValidHeader()
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_NfNumero.EmptyIfNull().ToString() == "NOTA")
            && ((this.String_DiNumero.EmptyIfNull().ToString() == "NR.DI") || (this.String_DiNumero.EmptyIfNull().ToString() == "NR.DI/DUIMP"))
            && ((this.String_DiData.EmptyIfNull().ToString() == "DATA DI") || (this.String_DiData.EmptyIfNull().ToString() == "DATA DI/DUIMP"))
            && ((this.String_PN.EmptyIfNull().ToString() == "PRODUTO") || (this.String_PN.EmptyIfNull().ToString() == "PN:PRODUTO"))
            && ((this.String_Descricao.EmptyIfNull().ToString() == "DESCRICAO") || (this.String_Descricao.EmptyIfNull().ToString() == "PN:DESCRICAO"))
            && (this.String_DiAdicaoNumero.EmptyIfNull().ToString() == "ADICAO")
            && (this.String_DiAdicaoSequencial.EmptyIfNull().ToString() == "SEQ.ITEM DI")
            && (this.String_NcmCodigo.EmptyIfNull().ToString() == "NCM")
            && (this.String_ValorFob.EmptyIfNull().ToString() == "FOB")
            && (this.String_ValorFrete.EmptyIfNull().ToString() == "FRETE")
            && (this.String_Quantidade.EmptyIfNull().ToString() == "QTDE")
            && (this.String_UnidadeMedida.EmptyIfNull().ToString() == "UNIDADE")
            && (this.String_ValorUnit.EmptyIfNull().ToString() == "CIF UNITARIO")
            && (this.String_ValorTotal.EmptyIfNull().ToString() == "CIF TOTAL")
            && (this.String_PesoLiquido.EmptyIfNull().ToString() == "PESO LIQUIDO")
            && (this.String_PesoBruto.EmptyIfNull().ToString() == "PESO BRUTO"))
            {
                isValid = true;
            }
            return isValid;
        }


        public bool IsRowEmpty()
        {
            bool resultado = false;
            if ((this.String_NfNumero.EmptyIfNull().ToString().Length == 0)
            && (this.String_DiNumero.EmptyIfNull().ToString().Length == 0)
            && (this.String_DiData.EmptyIfNull().ToString().Length == 0)
            && (this.String_PN.EmptyIfNull().ToString().Length == 0)
            && (this.String_Descricao.EmptyIfNull().ToString().Length == 0)
            && (this.String_DiAdicaoNumero.EmptyIfNull().ToString().Length == 0)
            && (this.String_DiAdicaoSequencial.EmptyIfNull().ToString().Length == 0)
            && (this.String_NcmCodigo.EmptyIfNull().ToString().Length == 0)
            && (this.String_ValorFob.EmptyIfNull().ToString().Length == 0)
            && (this.String_ValorFrete.EmptyIfNull().ToString().Length == 0)
            && (this.String_Quantidade.EmptyIfNull().ToString().Length == 0)
            && (this.String_UnidadeMedida.EmptyIfNull().ToString().Length == 0)
            && (this.String_ValorUnit.EmptyIfNull().ToString().Length == 0)
            && (this.String_ValorTotal.EmptyIfNull().ToString().Length == 0)
            && (this.String_PesoLiquido.EmptyIfNull().ToString().Length == 0)
            && (this.String_PesoBruto.EmptyIfNull().ToString().Length == 0))
            {
                resultado = true;
            }
            return resultado;
        }



    }
}