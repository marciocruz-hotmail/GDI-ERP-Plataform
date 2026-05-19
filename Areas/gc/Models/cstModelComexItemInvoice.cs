using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstModelComexItemInvoice
    {
        public int IndexRowSheet { get; set; }
        public string String_Item { get; set; }
        public string String_Qty { get; set; }
        public string String_Un { get; set; }
        public string String_PN { get; set; }
        public string String_PN_Auxiliar { get; set; }
        public string String_PN_Variacao1 { get; set; }
        public string String_PN_Variacao2 { get; set; }
        public string String_PN_Auxiliar_Curinga { get; set; }
        public string String_Description { get; set; }
        public string String_CD { get; set; }
        public string String_Manufacturer { get; set; }
        public string String_UnitPrice { get; set; }
        public string String_TotalPrice { get; set; }
        public string String_TotalWeigth { get; set; }
        public string String_Box { get; set; }
        public string String_SerialNumber { get; set; }
        public string String_Note { get; set; }
        public string String_Customer { get; set; }

        public int Int_Item { get; set; }
        public int Int_Qty { get; set; }
        public decimal Decimal_UnitPrice { get; set; }
        public decimal Decimal_TotalPrice { get; set; }
        public decimal Decimal_TotalWeigth { get; set; }
        public decimal Decimal_UnitWeigth { get; set; }
        public bool ItemProcessado { get; set; }
        public bool ItemValido { get; set; }
        public int ItemIndex { get; set; }

        public CstModelComexItemInvoice()
        {
            IndexRowSheet = 0;
            String_Item = string.Empty;
            String_Qty = string.Empty;
            String_Un = string.Empty;
            String_PN = string.Empty;
            String_PN_Auxiliar = string.Empty;
            String_PN_Variacao1 = string.Empty;
            String_PN_Variacao2 = string.Empty;
            String_Description = string.Empty;
            String_CD = string.Empty;
            String_Manufacturer = string.Empty;
            String_UnitPrice = string.Empty;
            String_TotalPrice = string.Empty;
            String_TotalWeigth = string.Empty;
            String_Box = string.Empty;
            String_SerialNumber = string.Empty;
            String_Note = string.Empty;
            String_Customer = string.Empty;
            Int_Item = 0;
            Int_Qty = 0;
            Decimal_UnitPrice = 0;
            Decimal_TotalPrice = 0;
            Decimal_TotalWeigth = 0;
            Decimal_UnitWeigth = 0;
            ItemProcessado = false;
            ItemValido = false;
        }

        public bool IsDetailItem() // Item Detalhe sem número do item
        {
            bool isValid = false;
            if ((this.String_Item.EmptyIfNull().ToString().Length == 0)
            && (this.String_Qty.EmptyIfNull().ToString().Length > 0)
            && (this.String_Un.EmptyIfNull().ToString().Length > 0)
            && (this.String_PN.EmptyIfNull().ToString().Length > 0)
            && (this.String_Description.EmptyIfNull().ToString().Length > 0)
            && (this.String_Manufacturer.EmptyIfNull().ToString().Length > 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalWeigth.EmptyIfNull().ToString().Length > 0)
            && (this.String_Note.EmptyIfNull().ToString().Length > 0)
            && (this.String_Customer.EmptyIfNull().ToString().Length > 0)) { isValid = true; };
            return isValid;
        }


        public bool IsAkitItem() // Grupo Totalizador
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_Item.EmptyIfNull().ToString().Length > 0)
            && (this.String_Qty.EmptyIfNull().ToString().Length > 0)
            && (this.String_Un.EmptyIfNull().ToString().Length > 0)
            && (this.String_PN.EmptyIfNull().ToString().Length > 0)
            && (this.String_Description.EmptyIfNull().ToString().Length > 0)
            && (this.String_Manufacturer.EmptyIfNull().ToString().Length > 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalWeigth.EmptyIfNull().ToString().Length > 0)
            && (this.String_Note.EmptyIfNull().ToString().Equals("AKIT")))
            {
                isValid = true;
            }
            return isValid;
        }

        public bool IsValidItem()
        {
            bool isValid = false;

            if (this.String_Note.EmptyIfNull().ToString().Length == 0) { this.String_Note = "0"; };
            if (this.String_Customer.EmptyIfNull().ToString().Length == 0) { this.String_Customer = "GDI"; };

            // Validações de Preenchimento dos Campos
            if ((this.String_Item.EmptyIfNull().ToString().Length > 0)
            && (this.String_Qty.EmptyIfNull().ToString().Length > 0)
            && (this.String_Un.EmptyIfNull().ToString().Length > 0)
            && (this.String_PN.EmptyIfNull().ToString().Length > 0)
            && (this.String_Description.EmptyIfNull().ToString().Length > 0)
            && (this.String_Manufacturer.EmptyIfNull().ToString().Length > 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalPrice.EmptyIfNull().ToString().Length > 0)
            && (this.String_TotalWeigth.EmptyIfNull().ToString().Length > 0)
            && (this.String_Note.EmptyIfNull().ToString().Length > 0))
            {
                isValid = true;
            }
            if ((isValid == true) && (this.String_Item.EmptyIfNull().ToString().ToUpperInvariant() == "ITEM")) { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidInteger(this.String_Item) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidInteger(this.String_Qty) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(this.String_UnitPrice) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(this.String_TotalPrice) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(this.String_TotalWeigth) == true)) { isValid = true; } else { isValid = false; }
            if (isValid == true)
            {
                this.Int_Item = int.Parse(this.String_Item);
                this.Int_Qty = int.Parse(this.String_Qty);
                this.String_UnitPrice = this.String_UnitPrice.Replace(".", "").Replace(",", "");
                this.String_TotalPrice = this.String_TotalPrice.Replace(".", "").Replace(",", "");
                this.String_TotalWeigth = this.String_TotalWeigth.Replace(".", "").Replace(",", "");
                this.Decimal_UnitPrice = Decimal.Parse(this.String_UnitPrice) / 100;
                this.Decimal_TotalPrice = Decimal.Parse(this.String_TotalPrice) / 100;
                this.Decimal_TotalWeigth = Decimal.Parse(this.String_TotalWeigth) / 1000;
                this.Decimal_UnitWeigth = (this.Decimal_TotalWeigth / this.Int_Qty);
                this.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(this.String_PN);
                this.String_PN_Auxiliar_Curinga = this.String_PN_Auxiliar.Replace("0", "_").Replace("O", "_").Replace("o", "_");
                this.String_PN_Variacao1 = this.String_PN_Auxiliar.Replace("0", "O");
                this.String_PN_Variacao2 = this.String_PN_Auxiliar.Replace("O", "0");
            }
            return isValid;
        }

        public bool IsValidItemTemp()
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_Item.EmptyIfNull().ToString().Length > 0)
            && (this.String_PN.EmptyIfNull().ToString().Length > 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length > 0))
            {
                isValid = true;
            }
            if ((isValid == true) && (this.String_Item.EmptyIfNull().ToString().ToUpperInvariant() == "ITEM")) { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidInteger(this.String_Item) == true)) { isValid = true; } else { isValid = false; }
            if ((isValid == true) && (LibNumbers.IsValidDecimal(this.String_UnitPrice) == true)) { isValid = true; } else { isValid = false; }
            if (isValid == true)
            {
                this.Int_Item = int.Parse(this.String_Item);
                this.String_UnitPrice = this.String_UnitPrice.Replace(".", "").Replace(",", "");
                this.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(this.String_PN);
                this.String_PN_Variacao1 = this.String_PN_Auxiliar.Replace("0", "O");
                this.String_PN_Variacao2 = this.String_PN_Auxiliar.Replace("O", "0");
            }
            return isValid;
        }


        public bool IsSerialNumber()
        {
            bool resultado = false;
            if ((this.String_Item.EmptyIfNull().ToString().Length == 0)
            && (this.String_Qty.EmptyIfNull().ToString().Length == 0)
            && (this.String_Un.EmptyIfNull().ToString().Length == 0)
            && (this.String_PN.EmptyIfNull().ToString().Length > 0)
            && (this.String_Description.EmptyIfNull().ToString().Length == 0)
            && (this.String_Manufacturer.EmptyIfNull().ToString().Length == 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length == 0)
            && (this.String_TotalPrice.EmptyIfNull().ToString().Length == 0)
            && (this.String_TotalWeigth.EmptyIfNull().ToString().Length == 0))
            {
                resultado = true;
            }
            return resultado;
        }

        public bool IsRowEmpty()
        {
            bool resultado = false;
            if ((this.String_Item.EmptyIfNull().ToString().Length == 0)
            && (this.String_Qty.EmptyIfNull().ToString().Length == 0)
            && (this.String_Un.EmptyIfNull().ToString().Length == 0)
            && (this.String_PN.EmptyIfNull().ToString().Length <= 3)
            && (this.String_Description.EmptyIfNull().ToString().Length == 0)
            && (this.String_Manufacturer.EmptyIfNull().ToString().Length == 0)
            && (this.String_UnitPrice.EmptyIfNull().ToString().Length == 0)
            && (this.String_TotalPrice.EmptyIfNull().ToString().Length == 0)
            && (this.String_TotalWeigth.EmptyIfNull().ToString().Length == 0))
            {
                resultado = true;
            }
            return resultado;
        }

        public bool IsValidHeader()
        {
            bool isValid = false;
            // Validações de Preenchimento dos Campos
            if ((this.String_Item.EmptyIfNull().ToString() == "ITEM")
            && (this.String_Qty.EmptyIfNull().ToString() == "QTY")
            && (this.String_Un.EmptyIfNull().ToString() == "UN")
            && (this.String_PN.EmptyIfNull().ToString() == "PN")
            && ((this.String_Description.EmptyIfNull().ToString() == "DESCRIPTION") || (this.String_Description.EmptyIfNull().ToString() == "PN:DESCRIPTION"))
            && (this.String_CD.EmptyIfNull().ToString() == "CD")
            && (this.String_Manufacturer.EmptyIfNull().ToString() == "MANUFACTURER")
            && (this.String_UnitPrice.EmptyIfNull().ToString() == "U.PRICE")
            && (this.String_TotalPrice.EmptyIfNull().ToString() == "E.PRICE")
            && (this.String_TotalWeigth.EmptyIfNull().ToString() == "E.WEIGHT")
            && (this.String_Note.EmptyIfNull().ToString() == "NOTE")
            && (this.String_Customer.EmptyIfNull().ToString() == "CUSTOMER"))
            {
                isValid = true;
            }
            return isValid;
        }
    }
}