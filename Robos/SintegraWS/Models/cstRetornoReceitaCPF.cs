using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.SintegraWS.Models
{
    public class cstRetornoReceitaCPF
    {
        public string receita_code { get; set; }
        public string receita_status { get; set; }
        public string receita_message { get; set; }
        public string receita_cpf { get; set; }
        public string receita_nome { get; set; }
        public string receita_data_nasc { get; set; }
        public string receita_situacao_cadastral { get; set; }
        public string receita_data_inscricao { get; set; }
        public string receita_genero { get; set; }
        public string receita_uf { get; set; }
        public string receita_dv { get; set; }
        public string receita_comprovante { get; set; }
        public string receita_ano_obito { get; set; }
    }
}