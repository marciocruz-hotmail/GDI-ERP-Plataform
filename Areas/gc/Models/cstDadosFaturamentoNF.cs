using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstDadosFaturamentoNF
    {
        public bool FaturamentoLiberado { get; set; }
        public bool CancelamentoLiberado { get; set; }
        public bool FaturamentoExecutado { get; set; }
        public bool NfeGerada { get; set; }
        public bool NfeNaoAutorizada { get; set; }
        public string DadosIE { get; set; }
        public string chaveAcesso { get; set; }
        public string linkDanfe { get; set; }
        public string linkDownloadXML { get; set; }
        public string MsgInfo { get; set; }
        public string MsgBloqueio { get; set; }
        public int id_movimento { get; set; }
        public int id_frete_responsavel { get; set; }
        public int id_ambiente_sefaz { get; set; }
        public string frete_esp { get; set; }
        public string frete_marca { get; set; }
        public string frete_nvol { get; set; }
        public decimal frete_valor { get; set; }
        public decimal frete_qvol { get; set; }
        public decimal frete_pesol { get; set; }
        public decimal frete_pesob { get; set; }
        public string informacoes_adicionais { get; set; }
        public cstDadosFaturamentoNF()
        {
            FaturamentoLiberado = false;
            CancelamentoLiberado = false;
            FaturamentoExecutado = false;
            NfeGerada = false;
            NfeNaoAutorizada = false;
            DadosIE = string.Empty;
            chaveAcesso = string.Empty;
            linkDanfe = string.Empty;
            linkDownloadXML = string.Empty;
            MsgInfo = string.Empty;
            MsgBloqueio = string.Empty;
            id_movimento = 0;
            id_frete_responsavel = 0;
            id_ambiente_sefaz = 0;
            frete_esp = string.Empty;
            frete_marca = string.Empty;
            frete_nvol = string.Empty;
            frete_valor = 0;
            frete_qvol = 0;
            frete_pesol = 0;
            frete_pesob = 0;
            informacoes_adicionais = string.Empty;
        }
    }
}