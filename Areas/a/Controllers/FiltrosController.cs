using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.a.Controllers
{
    [CustomAuthorize(Roles = "*")]
    public class FiltrosController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public string PreencherComboFiltro(String id)
        {
            var comboFiltroCampos = new List<SelectListItem>();
            var TitleView = String.Empty;
            try
            {
                if (id.ToString().Equals("g_Cidades_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_cidade", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Cidade" });
                    TitleView = "Filtro de Cidades";
                }
                else if (id.ToString().Equals("g_Clientes_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_cliente", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "razao_social", Text = "Razão Social" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cpf", Text = "CPF" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cnpj", Text = "CNPJ" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome_contato", Text = "Nome do Contato" });
                    TitleView = "Filtro de Clientes";
                }
                else if (id.ToString().Equals("g_Vendedores_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_vendedor", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    TitleView = "Filtro de Vendedores";
                }
                else if (id.ToString().Equals("g_Logons_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_logon", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "logon", Text = "Logon" });
                    TitleView = "Filtro de Logons";
                }
                else if (id.ToString().Equals("g_Nfe_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_nfe", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cnpj", Text = "CNPJ" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cpf", Text = "CPF" });
                    TitleView = "Filtro de Notas Fiscais";
                }
                else if (id.ToString().Equals("g_Produtos_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_produto", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "descricao", Text = "Descrição" });
                    TitleView = "Filtro de Produtos";
                }
                else if (id.ToString().Equals("g_Produtos_Tipos_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_produto_tipo", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    TitleView = "Filtro de Tipos de Produtos";
                }
                else if (id.ToString().Equals("gdc_Pefin_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_gdcpefin", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "razao_social", Text = "Razão Social" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome_fantasia", Text = "Nome Fantasia" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cpf", Text = "CPF" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cnpj", Text = "CNPJ" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "contrato_numero", Text = "Contrato" });
                    TitleView = "Filtro de Pendências Financeiras";
                }
                else if (id.ToString().Equals("gts_Curriculos_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_curriculo", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "registro", Text = "Registro" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cpf", Text = "CPF" });
                    TitleView = "Filtro de Currículos";
                }
                else if (id.ToString().Equals("gts_Internos_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_interno", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "registro", Text = "Registro" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "certidao_nascimento", Text = "Certidão" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "documento", Text = "Documento" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "cpf", Text = "CPF" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nit", Text = "NIT" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "titulo", Text = "Título Eleitoral" });
                    TitleView = "Filtro de Internos";
                }
                else if (id.ToString().Equals("g_Revendas_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_revenda", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Revenda" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "resp_revenda", Text = "Resp. Revenda" });
                    TitleView = "Filtro de Revendas";
                }
                else if (id.ToString().Equals("gdc_Consultas_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_consulta", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome Consulta" });
                    TitleView = "Filtro de Consultas";
                }
                else if (id.ToString().Equals("gdc_Tabelas_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_consulta_tabela", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome Tabela" });
                    TitleView = "Filtro de Tabelas";
                }
                else if (id.ToString().Equals("g_UF_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_uf", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "sigla", Text = "Sigla" });
                    comboFiltroCampos.Add(new SelectListItem { Value = "nome", Text = "Nome" });
                    TitleView = "Filtro de UF";
                }
                else if (id.ToString().Equals("g_ProdutosNcm_Index"))
                {
                    comboFiltroCampos.Add(new SelectListItem { Value = "id_produto_ncm", Text = "Id." });
                    comboFiltroCampos.Add(new SelectListItem { Value = "codigo_ncm", Text = "Código NCM" });
                    TitleView = "Filtro de NCM";
                }
            }
            finally { }
            ViewBag.comboFiltroCampos = comboFiltroCampos;
            return TitleView;
        }

        public ActionResult ModalFiltroGenericoView(String id)
        {
            ViewBag.Title = PreencherComboFiltro(id);
            return View();
        }
    }
}