using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_requisicoesMetadata
    {
        [Display(Name = "Id.")]
        public int id_requisicao { get; set; }

        [Display(Name = "Solicitação")]
        public string descricao_solicitacao { get; set; }

        [Display(Name = "Solução")]
        public string descricao_solucao { get; set; }

        [Display(Name = "Data/Hora")]
        public System.DateTime datahora_requisicao { get; set; }

        [Display(Name = "Solicitante")]
        public int id_usuario_requisicao { get; set; }
    }
}