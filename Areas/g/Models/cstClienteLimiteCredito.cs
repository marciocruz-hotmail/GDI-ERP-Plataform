using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class CstClienteLimiteCredito
    {
        public int id_cliente { get; set; }
        public bool consulta_cadastral_realizada { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> consulta_cadastral_data { get; set; }
        public bool consulta_cadastral_restricoes { get; set; }
        public int consulta_cadastral_score { get; set; }
        public decimal novo_limite_credito { get; set; }
        public decimal limite_credito_atual { get; set; }
        public string justificativa { get; set; }
        public string aprovado_por { get; set; }

        public CstClienteLimiteCredito()
        {
            consulta_cadastral_realizada = false;
            consulta_cadastral_restricoes = false;
            consulta_cadastral_score = 0;
        }
    }
}