using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_financeiro_faturamentosMetadata
    {
        [Display(Name = "Id.")]
        public int id_financeiro_faturamento { get; set; }

        [Display(Name = "Descrição")]
        public string descricao { get; set; }

        [Display(Name = "Data")]
        public System.DateTime datahora_cadastro { get; set; }
    }
}