using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe
{
    public class NFPResponse
    {
        public string id { get; set; }
        public string ambienteEmissao { get; set; }
        public string naturezaOperacao { get; set; }
        public string tipoOperacao { get; set; }
        public string finalidade { get; set; }
        public string dataCriacao { get; set; }
        public string dataEmissao { get; set; }
        public string dataAutorizacao { get; set; }
        public string dataUltimaAlteracao { get; set; }
        public int numero { get; set; }
        public string serie { get; set; }
        public string indicadorPresencaConsumidor { get; set; }
        public bool consumidorFinal { get; set; }
        public bool enviarPorEmail { get; set; }
        public Transporte transporte { get; set; }
        public Cliente cliente { get; set; }
        public List<ItemNFP> itens;
        public string informacoesAdicionais { get; set; }
        public string chaveAcesso { get; set; }
        public string linkDanfe { get; set; }
        public string linkDownloadXML { get; set; }
        public string status { get; set; }
        public string motivoStatus { get; set; }
    }
}