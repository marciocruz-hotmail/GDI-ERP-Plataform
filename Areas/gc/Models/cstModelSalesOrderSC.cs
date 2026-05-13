using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstModelSalesOrderSC
    {
        public string String_Item { get; set; }
        public int Int_Item { get; set; }
        public string String_Description { get; set; }
        public string String_DescriptionObs { get; set; }
        public string String_Ordered { get; set; }
        public string String_PN { get; set; }
        public string String_PN_Auxiliar { get; set; }
        public string String_PN_Variacao1 { get; set; }
        public string String_PN_Variacao2 { get; set; }
        public string String_PN_Auxiliar_Curinga { get; set; }
        public int Int_Ordered { get; set; }
        public string String_CD { get; set; }
        public string String_Qty { get; set; }
        public int Int_Qty { get; set; }
        public string String_T { get; set; }
        public string String_UOM { get; set; }
        public string String_UnitPrice { get; set; }
        public decimal Decimal_UnitPrice { get; set; }
        public string String_TotalPrice { get; set; }
        public decimal Decimal_TotalPrice { get; set; }

        public cstModelSalesOrderSC()
        {
            String_PN = string.Empty;
            String_PN_Auxiliar = string.Empty;
            String_PN_Variacao1 = string.Empty;
            String_PN_Variacao2 = string.Empty;
            String_PN_Auxiliar_Curinga = string.Empty;
            String_Item = string.Empty;
            Int_Item = 0;
            String_Description = string.Empty;
            String_DescriptionObs = string.Empty;
            String_Ordered = string.Empty;
            Int_Ordered = 0;
            String_CD = string.Empty;
            String_Qty = string.Empty;
            Int_Qty = 0;
            String_T = string.Empty;
            String_UOM = string.Empty;
            String_UnitPrice = string.Empty;
            String_TotalPrice = string.Empty;
            Decimal_UnitPrice = 0;
            Decimal_TotalPrice = 0;
        }

        public bool IsValidItem()
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_Item.EmptyIfNull().ToString().Length > 0)
            && (this.String_Description.EmptyIfNull().ToString().Length > 0)
            && (this.String_CD.EmptyIfNull().ToString().Length > 0)
            && (this.String_T.EmptyIfNull().ToString().Length > 0)
            && (this.String_UOM.EmptyIfNull().ToString().Length > 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalPrice.EmptyIfNull().ToString().Length > 0))
            {
                isValid = true;
            }


            if (this.String_Item.EmptyIfNull().IndexOf(".") > 0)
            {
                this.String_Item = this.String_Item.Substring(0, this.String_Item.EmptyIfNull().IndexOf("."));
            }
            if (this.String_Description.EmptyIfNull().ToString().Length > 0)
            {
                if (this.String_Item.IndexOf("\r") > 0) { this.String_Description = this.String_Description.Substring(0, this.String_Description.IndexOf("\r")); };
                if (this.String_Description.IndexOf("\n") > 0) { this.String_Description = this.String_Description.Substring(0, this.String_Description.IndexOf("\n")); };
            }

            if (this.String_Item.EmptyIfNull().ToString().Length > 0)
            {
                if (this.String_Item.IndexOf(".") > 0) { this.String_Item = this.String_Item.Substring(0, this.String_Item.IndexOf(".")); };
                if (this.String_Item.IndexOf(",") > 0) { this.String_Item = this.String_Item.Substring(0, this.String_Item.IndexOf(",")); };
            }

            if (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)
            {
                this.String_UnitPrice = LibNumbers.ConvertMoney(this.String_UnitPrice).ToString();
            }

            if (this.String_TotalPrice.EmptyIfNull().ToString().Length > 0)
            {
                this.String_TotalPrice = LibNumbers.ConvertMoney(this.String_TotalPrice).ToString();
            }

            if (this.String_Item.Trim().ToUpperInvariant() == "ITEM") { isValid = false; }
            if (isValid == true) { if (LibNumbers.IsValidInteger(String_Item) == false) { isValid = false; }; };
            if ((isValid == true) && (String_Ordered.EmptyIfNull().ToString().Length > 0)) { if (LibNumbers.IsValidInteger(String_Ordered) == false) { isValid = false; }; };
            if ((isValid == true) && (String_Qty.EmptyIfNull().ToString().Length > 0)) { if (LibNumbers.IsValidInteger(String_Qty) == false) { isValid = false; }; };
            if (isValid == true) { if (LibNumbers.IsValidDecimal(String_UnitPrice) == false) { isValid = false; }; };
            if (isValid == true) { if (LibNumbers.IsValidDecimal(String_TotalPrice) == false) { isValid = false; }; };
            if (isValid == true)
            {
                this.Int_Item = int.Parse(String_Item);
                if (String_Ordered.EmptyIfNull().ToString().Length > 0) { this.Int_Ordered = int.Parse(String_Ordered); }
                this.Decimal_UnitPrice = Decimal.Round(Decimal.Parse(String_UnitPrice), 6);
                this.Decimal_TotalPrice = Decimal.Round(Decimal.Parse(String_TotalPrice), 6);
                if (String_Qty.EmptyIfNull().ToString().Length > 0) { this.Int_Qty = int.Parse(String_Qty); }
                int PosEnter = String_Description.IndexOf("\r");
                if (PosEnter > 0)
                {
                    String FullDescription = String_Description;
                    String_Description = FullDescription.Substring(0, PosEnter).Replace("\r", " ").Replace("\n", "").Trim();
                    String_DescriptionObs = FullDescription.Substring(PosEnter).Replace("\r", " ").Replace("\n", "").Trim();
                }
                String_Description = String_Description.Trim();
                if (String_Description.EndsWith(".")) { String_Description = String_Description.Substring(0, String_Description.Length - 1); };
                this.String_Description = LibStringFormat.GDIFormatarDescricaoProdutoImportadoSemPN(this.String_Description);
                this.String_PN = LibStringFormat.ClienteGDIGetPartNumber(this.String_Description);
                this.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(this.String_PN);
                this.String_PN_Auxiliar_Curinga = this.String_PN_Auxiliar.Replace("0", "_").Replace("O", "_").Replace("o", "_");
                this.String_PN_Variacao1 = this.String_PN_Auxiliar.Replace("0", "O");
                this.String_PN_Variacao2 = this.String_PN_Auxiliar.Replace("O", "0");

            }
            return isValid;
        }

        public bool IsValidItemSimpleLista()
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_Description.EmptyIfNull().ToString().Length > 0) && (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)) { isValid = true; };

            if (this.String_Description.EmptyIfNull().ToString().Length > 0)
            {
                this.String_Description = this.String_Description.Replace("\r", "").Replace("\n", "");
                if (this.String_Description.EndsWith(".")) { this.String_Description = this.String_Description.Substring(0, this.String_Description.Length - 1); };
                this.String_Description = LibStringFormat.GDIFormatarDescricaoProdutoImportadoSemPN(this.String_Description);
                this.String_PN = LibStringFormat.ClienteGDIGetPartNumber(this.String_Description);
                this.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(this.String_PN);
                this.String_PN_Auxiliar_Curinga = this.String_PN_Auxiliar.Replace("0", "_").Replace("O", "_").Replace("o", "_");
                this.String_PN_Variacao1 = this.String_PN_Auxiliar.Replace("0", "O");
                this.String_PN_Variacao2 = this.String_PN_Auxiliar.Replace("O", "0");
            }

            if (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)
            {
                this.String_UnitPrice = LibNumbers.ConvertMoney(this.String_UnitPrice).ToString();
            }

            if (isValid == true)
            {
                this.Decimal_UnitPrice = Decimal.Round(Decimal.Parse(this.String_UnitPrice), 6);
                if (this.String_Qty.EmptyIfNull().ToString().Length > 0) { this.Int_Qty = int.Parse(this.String_Qty); } else { this.Int_Qty = 0; }
                this.Decimal_TotalPrice = Decimal_UnitPrice * 2 * this.Int_Qty;
            }
            return isValid;
        }

    }
}