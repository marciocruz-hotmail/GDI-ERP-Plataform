using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstDatasetProdutosServicos
    {
        public int id_produto_servico { get; set; }
        public decimal preco_venda { get; set; }
        public decimal fob1_dollar { get; set; }
        public int fob1_id_importacao { get; set; }
        public decimal fob2_dollar { get; set; }
        public int fob2_id_importacao { get; set; }
        public decimal fob3_dollar { get; set; }
        public int fob3_id_importacao { get; set; }
        public string codigo { get; set; }
        public string descricao_longa { get; set; }
        public bool has_corecharge { get; set; }
        public int id_produto_ncm { get; set; }
        public int id_unidade_medida_venda { get; set; }
        public string unidade_medida { get; set; }
        public string ncm { get; set; }
        public decimal saldo_01_disponivel { get; set; }
        public decimal saldo_03_disponivel { get; set; }
    }
}