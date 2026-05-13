using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstInvoice
    {
        public string invoice_tipo { get; set; }
        public int invoice_qtd_paginas { get; set; }
        public string invoice_numero { get; set; }
        public string cliente_nome { get; set; }
        public string cliente_endereco1 { get; set; }
        public string contato_nome { get; set; }
        public string contato_telefone { get; set; }
        public string contato_email { get; set; }
        public string invoice_subtotal_value { get; set; }
        public string invoice_total_corecharges { get; set; }
        public string invoice_total_freight { get; set; }

        public string invoice_grantotal_value { get; set; }
        public string invoice_data_vencimento { get; set; }

        public string vendedor_nome { get; set; }
        public string vendedor_telefone { get; set; }
        public string vendedor_email { get; set; }

        public string invoice_aeronave_prefixo { get; set; }
        public string invoice_aeronave_modelo { get; set; }
        public string invoice_aeronave_serie { get; set; }
        public string invoice_aeronave_registro { get; set; }
        public string invoice_obs { get; set; }
        public string invoice_obs_general { get; set; }
        public string invoice_condicao_pagto { get; set; }

        public string invoice_moeda_nome { get; set; }
        public string invoice_moeda_flag { get; set; }

        public string cliente_endereco2 { get; set; }
        public string cliente_telefone { get; set; }
        public string cliente_email { get; set; }
        public string invoice_numero2 { get; set; }
        public string invoice_data { get; set; }
        public string invoice_conta_caixa { get; set; }
        public string invoice_tax_value { get; set; }
        public string invoice_shipping_value { get; set; }


        public List<cstInvoiceItem> AllItens { get; set; }
        public cstInvoice()
        {
            AllItens = new List<cstInvoiceItem>();
        }
    }
}