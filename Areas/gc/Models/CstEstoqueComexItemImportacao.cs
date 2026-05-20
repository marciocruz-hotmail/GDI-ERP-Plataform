using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstEstoqueComexItemImportacao
    {
        public int IdInvoiceItem { get; set; }
        public string NomeProduto { get; set; }
        public string OsCliente { get; set; }
        public int QtdItemConferir { get; set; }
        public string StatusItemConferido { get; set; }
        public string IconeStatus { get; set; }
        public CstEstoqueComexItemImportacao()
        {
            IdInvoiceItem = 0;
            NomeProduto = string.Empty;
            OsCliente = string.Empty;
            QtdItemConferir = 0;
            StatusItemConferido = string.Empty;
            IconeStatus = string.Empty;
        }
    }
}