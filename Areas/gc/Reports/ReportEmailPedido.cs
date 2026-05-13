using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Reports
{
    public class ReportEmailPedido
    {
        private GdiPlataformEntities db;

        private string ClienteNome = string.Empty;
        private string ClienteEmail = string.Empty;

        public void setClienteNome(String nome)
        {
            ClienteNome = nome;
        }
        public void setClienteEmail(String email)
        {
            ClienteEmail = email;
        }
        public string getClienteNome()
        {
            return ClienteNome;
        }
        public string getClienteEmail()
        {
            return ClienteEmail;
        }

        public ReportEmailPedido()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public string getEmailOrcamentoPedido(int id_movimento)
        {
            var listMovimento = (from _m in db.gc_movimentos
                                 join _c in db.g_clientes on _m.id_cliente equals _c.id_cliente
                                 where _m.id_movimento == id_movimento
                                 select new { tableMovimento = _m, tableCliente = _c }).ToList();

            var listItens = (from _i in db.gc_movimentos_itens
                             join _p in db.g_produtos on _i.id_produto equals _p.id_produto
                             where _i.id_movimento == id_movimento
                             select new { tableItens = _i, nomeProduto = _p.nome }).ToList();

            String tipoMovimento = string.Empty;
            int idTipo = listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo;
            if (idTipo == 3) { tipoMovimento = "Orçamento"; }
            else if (idTipo == 4) { tipoMovimento = "Pedido"; };

            String statusMovimento = string.Empty;
            int idStatus = listMovimento.FirstOrDefault().tableMovimento.id_movimento_status;
            if (idStatus == 1) { statusMovimento = "Aberto"; }
            else if (idStatus == 2) { statusMovimento = "Fechado"; }
            else if (idStatus == 3) { statusMovimento = "Cancelado"; };

            string iconPosicaoMovimento = string.Empty;
            if ((listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 1) && (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 3)) { iconPosicaoMovimento = LibIcons.getIcon("fa-regular fa-file", "Orçamento Incluído", "", "fa-lg"); }
            else if ((listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 1) && (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 4)) { iconPosicaoMovimento = LibIcons.getIcon("fa-regular fa-file", "Pedido Incluído", "", "fa-lg"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 2) { iconPosicaoMovimento = LibIcons.getIcon("fa-solid fa-handshake", "Pagamento Aprovado", "", "fa-lg"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 3) { iconPosicaoMovimento = LibIcons.getIcon("fa-solid fa-cart-plus", "Pedido Baixado", "", "fa-lg"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 4) { iconPosicaoMovimento = LibIcons.getIcon("fa-solid fa-credit-card", "Pedido Faturado", "#008000", "fa-lg"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 5) { iconPosicaoMovimento = LibIcons.getIcon("fa-solid fa-truck", "Pedido Expedido", "", "fa-lg"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 6) { iconPosicaoMovimento = LibIcons.getIcon("fa-regular fa-thumbs-up", "Pedido Entregue", "#008000", "fa-lg"); }

            String htmlEmail = string.Empty;
            htmlEmail += "<!DOCTYPE html>";
            htmlEmail += "<html lang='pt-br'>";
            htmlEmail += "<head>";
            htmlEmail += "    <meta charset='utf - 8'>";
            htmlEmail += "    <meta http-equiv='X-UA-Compatible' content='IE=edge'>";
            htmlEmail += "    <meta name='viewport' content='width=device-width, initial-scale=1, shrink-to-fit=no'>";
            htmlEmail += "    <meta name='description' content=''>";
            htmlEmail += "    <meta name='author' content=''>";
            htmlEmail += "    <link rel='stylesheet' href='https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css' integrity='sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T' crossorigin='anonymous'>";
            htmlEmail += "    <link rel='stylesheet' href='https://stackpath.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css' rel='stylesheet' integrity='sha384-wvfXpqpZZVQGK6TAh5PVlGOfQNHSoD2xbE+QkPxCAFlNEevoEH3Sl0sibVcOQVnN' crossorigin='anonymous'>";
            htmlEmail += "    <title>" + tipoMovimento + "</title>";
            htmlEmail += "</head>";
            htmlEmail += "<body>";
            htmlEmail += "    <div class=\"card-title m-1 p-1\">";
            htmlEmail += "        <div class='panel - info'>";
            htmlEmail += "            <div class='col-md-2 text-center'>";
            htmlEmail += "                <img src='https://bucket-docs-publics.s3.sa-east-1.amazonaws.com/Asttter/FilesImages/logoGdi.jpg' style='height: 100px; width: 200px;'/>";
            htmlEmail += "            </div>";
            htmlEmail += "            <div class='col-md-10 text-center' style='margin-top:5px'>";
            htmlEmail += "                <h3>GDI Aviação - " + tipoMovimento + " nº 143</h3>";
            htmlEmail += "            </div>";
            htmlEmail += "        </div>";
            htmlEmail += "        <div class='panel-body' style='margin-top:30px'>";
            htmlEmail += "            <div class='row'>";
            htmlEmail += "                <div class='col-md-12'>";
            htmlEmail += "                    <table class='table borderless'>";
            htmlEmail += "                        <thead>";
            htmlEmail += "                            <tr class='borderless' style='margin-top:0px'>";
            htmlEmail += "                                <td width='100%'>Cliente:&nbsp;&nbsp;&nbsp;&nbsp;" + listMovimento.FirstOrDefault().tableCliente.nome.ToString() + "</td>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                        </thead>";
            htmlEmail += "                        <tbody>";
            htmlEmail += "                            <tr class='borderless' style='margin-top:0px'>";
            htmlEmail += "                                <td width='100%'>Dt. Orçamento:&nbsp;&nbsp;&nbsp;&nbsp;" + listMovimento.FirstOrDefault().tableMovimento.datahora_cadastro.ToString("dd/MM/yyyy") + "</td>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                            <tr class='borderless' style='margin-top:0px'>";
            htmlEmail += "                                <td width='100%'>Dt. Validade:&nbsp;&nbsp;&nbsp;&nbsp;" + listMovimento.FirstOrDefault().tableMovimento.datahora_cadastro.ToString("dd/MM/yyyy") + "</td>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                            <tr class='borderless' style='margin-top:0px'>";
            htmlEmail += "                                <td width='40%'>Status:&nbsp;&nbsp;&nbsp;&nbsp;" + statusMovimento + "</td>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                            <tr class='borderless' style='margin-top:0px'>";
            htmlEmail += "                                <td width='60%'>Posição:&nbsp;&nbsp;&nbsp;&nbsp;" + iconPosicaoMovimento + "</td>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                        </tbody>";
            htmlEmail += "                    </table>";
            htmlEmail += "                    <table class='table borderless'>";
            htmlEmail += "                        <thead>";
            htmlEmail += "                            <tr>";
            htmlEmail += "                                <th colspan='5'><b>Itens do " + tipoMovimento + "</b></th>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                            <tr>";
            htmlEmail += "                                <td width='20%' align='center'><b>Qtd</b></td>";
            htmlEmail += "                                <td width='40%' align='center'><b>Produto</b></td>";
            htmlEmail += "                                <td width='15%' align='center'><b>R$ Unit.</b></td>";
            htmlEmail += "                                <td width='15%' align='center'><b>R$ Valor</b></td>";
            htmlEmail += "                            </tr>";
            htmlEmail += "                        </thead>";

            foreach (var item in listItens)
            {
                htmlEmail += "                            <tr>";
                htmlEmail += "                                <td width='20%' align='center'>" + item.tableItens.quantidade.ToString() + "</td>";
                htmlEmail += "                                <td width='40%' align='center'>" + item.nomeProduto.ToString() + "</td>";
                htmlEmail += "                                <td width='15%' align='center'>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableItens.valor_unit) + "</td>";
                htmlEmail += "                                <td width='15%' align='center'>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableItens.valor_total) + "</td>";
                htmlEmail += "                            </tr>";
            }

            htmlEmail += "                            <tr>";
            htmlEmail += "                                <td width='20%' align='center'>&nbsp;</td>";
            htmlEmail += "                                <td width='40%' align='center'>&nbsp;</td>";
            htmlEmail += "                                <td width='15%' align='center'>&nbsp;</td>";
            htmlEmail += "                                <td width='15%' align='center'>&nbsp;</td>";
            htmlEmail += "                            </tr>";

            htmlEmail += "                            <tr>";
            htmlEmail += "                                <td width='20%' align='center'>&nbsp;</td>";
            htmlEmail += "                                <td width='40%' align='center'>&nbsp;</td>";
            htmlEmail += "                                <td width='15%' align='center'><b>R$ Total<b></td>";
            htmlEmail += "                                <td width='15%' align='center'><b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_bruto) + "</b></td>";
            htmlEmail += "                            </tr>";

            htmlEmail += "                    </table>";
            htmlEmail += "                    <div class='col-lg-8'>";
            htmlEmail += "                    </div>";
            htmlEmail += "                </div>";
            htmlEmail += "            </div>";
            htmlEmail += "        </div>";
            htmlEmail += "    </div>";
            htmlEmail += "<script>";
            htmlEmail += "<script src='https://code.jquery.com/jquery-3.3.1.slim.min.js' integrity='sha384-q8i/X+965DzO0rT7abK41JStQIAqVgRVzpbzo5smXKp4YfRvH+8abtTE1Pi6jizo' crossorigin='anonymous'></script>";
            htmlEmail += "<script src='https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js' integrity='sha384-UO2eT0CpHqdSJQ6hJty5KVphtPhzWj9WO1clHTMGa3JDZwrnQq4sF86dIHNDz0W1' crossorigin='anonymous'></script>";
            htmlEmail += "<script src='https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js' integrity='sha384-JjSmVgyd0p3pXB1rRibZUAYoIIy6OrQ6VrjIEaFf/nJGzIxFDsf4x0xIM+B07jRM' crossorigin='anonymous'></script>";
            htmlEmail += "</script>";
            htmlEmail += "</body>";
            return htmlEmail;
        }

        public string getTemplate(int id_movimento)
        {
            String htmlTemplate = string.Empty;
            String htmlTemplateItens = string.Empty;

            String htmlEmail = string.Empty;
            String htmlItens = string.Empty;

            String imagesS3 = "<img width='12' height='12' border='0' src='https://bucket-docs-publics.s3.sa-east-1.amazonaws.com/Asttter/FilesImages/[img]'/>";
            String iconeTipoMovimento = string.Empty;
            String iconeStatusMovimento = string.Empty;
            String iconePosicaoMovimento = string.Empty;

            g_templates record_g_templates = db.g_templates.Where(t => t.localizador == "GcMovimentosEmailCotacaoPedido").FirstOrDefault();
            if (record_g_templates != null) { htmlTemplate = record_g_templates.template.EmptyIfNull(); }

            var listMovimento = (from _m in db.gc_movimentos
                                 join _c in db.g_clientes on _m.id_cliente equals _c.id_cliente
                                 where _m.id_movimento == id_movimento
                                 select new { tableMovimento = _m, tableCliente = _c }).ToList();

            var listItens = (from _i in db.gc_movimentos_itens
                             join _p in db.g_produtos on _i.id_produto equals _p.id_produto
                             where _i.id_movimento == id_movimento
                             select new { tableItens = _i, tableProdutos = _p }).ToList();

            String tipoMovimento = string.Empty;
            int idTipo = listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo;
            if (idTipo == 3) { tipoMovimento = "Orçamento"; iconeTipoMovimento = imagesS3.Replace("[img]", "fa-clipboard.png"); }
            else if (idTipo == 4) { tipoMovimento = "Pedido"; iconeTipoMovimento = imagesS3.Replace("[img]", "fa-boxes.png"); };

            String statusMovimento = string.Empty;
            int idStatus = listMovimento.FirstOrDefault().tableMovimento.id_movimento_status;
            if (idStatus == 1) { statusMovimento = "Aberto"; iconeStatusMovimento = imagesS3.Replace("[img]", "fa-shopping-cart.png"); }
            else if (idStatus == 2) { statusMovimento = "Fechado"; iconeStatusMovimento = imagesS3.Replace("[img]", "fa-lock.png"); }
            else if (idStatus == 3) { statusMovimento = "Cancelado"; };

            string posicaoMovimento = string.Empty;
            if ((listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 1) && (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 3)) { posicaoMovimento = "Orçamento Incluído"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-file.png"); }
            else if ((listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 1) && (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 4)) { posicaoMovimento = "Pedido Incluído"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-file.png"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 2) { posicaoMovimento = "Pagamento Aprovado"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-handshake.png"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 3) { posicaoMovimento = "Pedido Baixado"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-cart-plus.png"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 4) { posicaoMovimento = "Pedido Faturado"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-credit-card.png"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 5) { posicaoMovimento = "Pedido Expedido"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-truck.png"); }
            else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_posicao == 6) { posicaoMovimento = "Pedido Entregue"; iconePosicaoMovimento = imagesS3.Replace("[img]", "fa-thumbs-up.png"); }
            htmlTemplate = htmlTemplate.Replace("[Titulo]", "GDI Aviação - " + tipoMovimento + " Nº " + listMovimento.FirstOrDefault().tableMovimento.id_movimento.ToString());
            htmlTemplate = htmlTemplate.Replace("[ClienteNome]", listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString());
            htmlTemplate = htmlTemplate.Replace("[ClienteEndereco]", listMovimento.FirstOrDefault().tableCliente.endereco_com.EmptyIfNull().ToString() + ",&nbsp;" + listMovimento.FirstOrDefault().tableCliente.endereco_com_numero.EmptyIfNull().ToString() + "&nbsp;" + listMovimento.FirstOrDefault().tableCliente.endereco_com_complemento.EmptyIfNull().ToString() + "&nbsp;" + listMovimento.FirstOrDefault().tableCliente.bairro_com.ToString());
            htmlTemplate = htmlTemplate.Replace("[ClienteEmail]", listMovimento.FirstOrDefault().tableCliente.email_principal.EmptyIfNull().ToString());
            htmlTemplate = htmlTemplate.Replace("[DataMovimento]", listMovimento.FirstOrDefault().tableMovimento.datahora_cadastro.ToString("dd/MM/yyyy"));
            htmlTemplate = htmlTemplate.Replace("[DataValidade]", listMovimento.FirstOrDefault().tableMovimento.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy"));
            htmlTemplate = htmlTemplate.Replace("[Status]", statusMovimento + "&nbsp;" + iconeStatusMovimento);
            htmlTemplate = htmlTemplate.Replace("[Posicao]", posicaoMovimento + "&nbsp;" + iconePosicaoMovimento);
            htmlTemplate = htmlTemplate.Replace("[TipoMovimento]", tipoMovimento);
            htmlTemplate = htmlTemplate.Replace("[ValorTotalMovimento]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_bruto));

            int posicao1 = htmlTemplate.IndexOf("<!--ItensHeader-->");
            int posicao2 = htmlTemplate.IndexOf("<!--ItensFooter-->");

            htmlTemplateItens = htmlTemplate.Substring(posicao1, posicao2 - posicao1);
            htmlTemplate = htmlTemplate.Substring(0, posicao1-1) + "<!--Itens-->" + htmlTemplate.Substring(posicao2);

            foreach (var item in listItens)
            {
                string htmlItem = htmlTemplateItens;
                htmlItem = htmlItem.Replace("[QtdItem]", item.tableItens.quantidade.EmptyIfNull().ToString());
                htmlItem = htmlItem.Replace("[ProdutoItem]", item.tableProdutos.nome.EmptyIfNull().ToString());
                htmlItem = htmlItem.Replace("[ProdutoCodigo]", item.tableProdutos.codigo.EmptyIfNull().ToString());
                htmlItem = htmlItem.Replace("[ValorUnitItem]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableItens.valor_unit));
                htmlItem = htmlItem.Replace("[ValorTotalItem]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableItens.valor_total));
                htmlItens += htmlItem;
            }

            htmlEmail = htmlTemplate;
            htmlEmail = htmlTemplate.Replace("<!--Itens-->", htmlItens);

            setClienteNome(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString());
            setClienteEmail(listMovimento.FirstOrDefault().tableCliente.email_principal.EmptyIfNull().ToString());

            return htmlEmail;
        }
    }
}