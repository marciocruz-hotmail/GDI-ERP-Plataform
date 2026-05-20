using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.SintegraWS.Models
{
    public class CstRetornoSintegraWS
    {
        public string sintegra_code { get; set; }
        public string sintegra_status { get; set; }
        public string sintegra_message { get; set; }
        public string sintegra_ie { get; set; }
        public string informacao_ie_como_destinatario { get; set; }
        public string sintegra_razaosocial { get; set; }
        public string sintegra_cep { get; set; }
        public string sintegra_uf { get; set; }
        public string sintegra_municipio { get; set; }
        public string sintegra_bairro { get; set; }
        public string sintegra_logradouro { get; set; }
        public string sintegra_numero { get; set; }
        public string sintegra_complemento { get; set; }
        public string situacao_ie { get; set; }
        public string data_inicio_atividade { get; set; }
        public string contribuinte_icms { get; set; }


    }
}