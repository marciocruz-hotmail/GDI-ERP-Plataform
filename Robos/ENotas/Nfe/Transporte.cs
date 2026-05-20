using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe
{
    public class Transporte
    {
        public Frete frete { get; set; }
        public Volume volume { get; set; }
        public Transportadora transportadora { get; set; }
    }

    public class Frete
    {
        public Frete()
        {
            modalidade = string.Empty;
            valor = 0;
        }
        public string modalidade { get; set; }
        public decimal valor { get; set; }
    }

    public class Volume
    {
        public Volume()
        {

        }
        public decimal quantidade { get; set; }
        public string especie { get; set; }
        public string marca { get; set; }
        public string numeracao { get; set; }
        public decimal pesoLiquido { get; set; }
        public decimal pesoBruto { get; set; }
    }

    public class Transportadora
    {
        public Transportadora()
        {
            usarDadosEmitente = false;
        }
        public bool usarDadosEmitente { get; set; }
        public string tipoPessoa { get; set; }
        public string cpfCnpj { get; set; }
        public string nome { get; set; }
        public string inscricaoEstadual { get; set; }
        public string enderecoCompleto { get; set; }
        public string cidade { get; set; }
        public string uf { get; set; }

    }


}