using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstFinanceiroLancamentoBoletoFebraban
    {
        public int idFinanceiro;
        public bool printPDF = false;

        // Cabeçalho
        public string ECedenteLogo { get; set; }
        public string EDadosCabecalho1 { get; set; }
        public string EDadosCabecalho2 { get; set; }
        public string EDadosCabecalho3 { get; set; }
        public string EDadosCabecalho4 { get; set; }

        // Cliente
        public string EClienteNome { get; set; }
        public string EClienteDocumento { get; set; }
        public string EClienteCodigo { get; set; }
        public string EClienteEndereco { get; set; }

        public string EClienteEnderecoCidadeUF { get; set; }

        // Cedente
        public string ECedenteNome { get; set; }
        public string ECedenteComplemento1 { get; set; }
        public string ECedenteComplemento2 { get; set; }

        // SubTotal
        public string EClienteMensagem { get; set; }
        public string EValorLiquido { get; set; }
        public string EDescontos { get; set; }
        public string EDespesas { get; set; }
        public string EValorBruto { get; set; }
        public string EImpostos { get; set; }

        // Boleto
        public string EDataProcessamento { get; set; }
        public string EDataDocumento { get; set; }
        public string ENumeroDocumento { get; set; }
        public string ECodBanco { get; set; }
        public string ELocalPagamento { get; set; }
        public string EDataVencimento { get; set; }
        public string EEspecieDoc { get; set; }
        public string EAceite { get; set; }
        public string EValorTotal { get; set; }
        public string ELogoBanco { get; set; }
        public string ECarteira { get; set; }
        public string EAgenciaCodCedente { get; set; }
        public string ELinhaDigitavel { get; set; }
        public string ENossoNumeroDV { get; set; }
        public string EEspecieMoeda { get; set; }
        public string EMensagemCaixa { get; set; }
        public string ENomeSacado { get; set; }
        public string EEnderecoSacado { get; set; }
        public string ECidadeSacado { get; set; }
        public string ECepSacado { get; set; }
        public string EUFSacado { get; set; }
        public string EDocSacado { get; set; }
        public string ECodigoBarras { get; set; }
        public string EImgBarCode { get; set; }
        public string EImgPixQrCode { get; set; }

        public string EPixEMV { get; set; }
        public bool HasPix { get; set; }
        public CstFinanceiroLancamentoBoletoFebraban()
        {
            HasPix = false;
        }
    }
}