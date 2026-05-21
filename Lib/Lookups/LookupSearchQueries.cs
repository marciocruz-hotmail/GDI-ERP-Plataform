using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Leituras EF6 sem cache para typeahead Ajax (pedidos — 1.6).</summary>
    internal static class LookupSearchQueries
    {
        private const int DefaultLimit = 30;
        private const int MaxLimit = 50;

        public static List<LookupAjaxItemDto> SearchClientes(GdiPlataformEntities db, string q, int? id, int limit)
        {
            limit = NormalizeLimit(limit);
            if (id.HasValue && id.Value > 0)
            {
                var one = GetClienteItem(db, id.Value);
                return one != null ? new List<LookupAjaxItemDto> { one } : new List<LookupAjaxItemDto>();
            }

            var term = (q ?? string.Empty).Trim();
            if (term.Length < 2)
                return new List<LookupAjaxItemDto>();

            // Pedidos: apenas cadastros ativos marcados como cliente (sem filtro por vendedor/perfil).
            var query = db.g_clientes.AsNoTracking().Where(c => c.ativo && c.is_cliente);

            int idParsed;
            if (int.TryParse(term, out idParsed) && idParsed > 0)
            {
                query = query.Where(c =>
                    c.id_cliente == idParsed
                    || c.nome.Contains(term)
                    || c.cpf.Contains(term)
                    || c.cnpj.Contains(term));
            }
            else
            {
                query = query.Where(c =>
                    c.nome.Contains(term)
                    || c.cpf.Contains(term)
                    || c.cnpj.Contains(term));
            }

            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);

            return query
                .OrderBy(c => c.nome)
                .Take(limit)
                .Select(c => new { c.id_cliente, c.nome, c.cpf, c.cnpj })
                .ToList()
                .Select(c => new LookupAjaxItemDto
                {
                    id = c.id_cliente.ToString(),
                    text = FormatClienteText(c.nome, c.cpf, c.cnpj, c.id_cliente, displayWidth)
                })
                .ToList();
        }

        public static LookupAjaxItemDto GetClienteItem(GdiPlataformEntities db, int idCliente)
        {
            if (idCliente <= 0) return null;
            var c = db.g_clientes.AsNoTracking()
                .Where(x => x.id_cliente == idCliente && x.ativo && x.is_cliente)
                .Select(x => new { x.id_cliente, x.nome, x.cpf, x.cnpj })
                .FirstOrDefault();
            if (c == null) return null;
            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
            return new LookupAjaxItemDto
            {
                id = c.id_cliente.ToString(),
                text = FormatClienteText(c.nome, c.cpf, c.cnpj, c.id_cliente, displayWidth)
            };
        }

        /// <summary>Clientes e fornecedores ativos (substitui GetComboGClientesFornecedores* no HTML).</summary>
        public static List<LookupAjaxItemDto> SearchClientesFornecedores(GdiPlataformEntities db, string q, int? id, int limit, bool comDoc)
        {
            limit = NormalizeLimit(limit);
            if (id.HasValue && id.Value > 0)
            {
                var one = GetClienteFornecedorItem(db, id.Value, comDoc);
                return one != null ? new List<LookupAjaxItemDto> { one } : new List<LookupAjaxItemDto>();
            }

            var term = (q ?? string.Empty).Trim();
            if (term.Length < 2)
                return new List<LookupAjaxItemDto>();

            var query = db.g_clientes.AsNoTracking().Where(c => c.ativo);
            int idParsed;
            if (int.TryParse(term, out idParsed) && idParsed > 0)
            {
                query = query.Where(c =>
                    c.id_cliente == idParsed
                    || c.nome.Contains(term)
                    || c.cpf.Contains(term)
                    || c.cnpj.Contains(term));
            }
            else
            {
                query = query.Where(c =>
                    c.nome.Contains(term)
                    || c.cpf.Contains(term)
                    || c.cnpj.Contains(term));
            }

            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);

            return query
                .OrderBy(c => c.nome)
                .Take(limit)
                .Select(c => new { c.id_cliente, c.nome, c.cpf, c.cnpj })
                .ToList()
                .Select(c => new LookupAjaxItemDto
                {
                    id = c.id_cliente.ToString(),
                    text = FormatClienteFornecedorText(c.nome, c.cpf, c.cnpj, c.id_cliente, displayWidth, comDoc)
                })
                .ToList();
        }

        public static LookupAjaxItemDto GetClienteFornecedorItem(GdiPlataformEntities db, int idCliente, bool comDoc)
        {
            if (idCliente <= 0) return null;
            var c = db.g_clientes.AsNoTracking()
                .Where(x => x.id_cliente == idCliente && x.ativo)
                .Select(x => new { x.id_cliente, x.nome, x.cpf, x.cnpj })
                .FirstOrDefault();
            if (c == null) return null;
            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
            return new LookupAjaxItemDto
            {
                id = c.id_cliente.ToString(),
                text = FormatClienteFornecedorText(c.nome, c.cpf, c.cnpj, c.id_cliente, displayWidth, comDoc)
            };
        }

        public static List<LookupAjaxItemDto> SearchProdutos(GdiPlataformEntities db, string q, int? id, int limit)
        {
            limit = NormalizeLimit(limit);
            if (id.HasValue && id.Value > 0)
            {
                var one = GetProdutoItem(db, id.Value);
                return one != null ? new List<LookupAjaxItemDto> { one } : new List<LookupAjaxItemDto>();
            }

            var term = (q ?? string.Empty).Trim();
            if (term.Length < 2)
                return new List<LookupAjaxItemDto>();

            var query = db.g_produtos.AsNoTracking().Where(p => p.ativo);
            int idParsed;
            if (int.TryParse(term, out idParsed) && idParsed > 0)
            {
                query = query.Where(p =>
                    p.id_produto == idParsed
                    || p.nome.Contains(term)
                    || p.codigo.Contains(term));
            }
            else
            {
                query = query.Where(p => p.nome.Contains(term) || p.codigo.Contains(term));
            }

            int sizeNome = ResolveSizeNomeProduto();

            return query
                .OrderBy(p => p.nome)
                .Take(limit)
                .Select(p => new { p.id_produto, p.nome })
                .ToList()
                .Select(p => new LookupAjaxItemDto
                {
                    id = p.id_produto.ToString(),
                    text = TruncateNome(p.nome, sizeNome)
                })
                .ToList();
        }

        public static LookupAjaxItemDto GetProdutoItem(GdiPlataformEntities db, int idProduto)
        {
            if (idProduto <= 0) return null;
            var p = db.g_produtos.AsNoTracking()
                .Where(x => x.id_produto == idProduto && x.ativo)
                .Select(x => new { x.id_produto, x.nome })
                .FirstOrDefault();
            if (p == null) return null;
            return new LookupAjaxItemDto
            {
                id = p.id_produto.ToString(),
                text = TruncateNome(p.nome, ResolveSizeNomeProduto())
            };
        }

        public static List<SelectListItem> ComboPlaceholderCliente()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" }
            };
        }

        /// <summary>Filtro Index/Painel pedidos — -1 = todos (sem GetComboSomenteGClientes no HTML).</summary>
        public static List<SelectListItem> ComboFiltroClienteTodosAtivos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]", Selected = true }
            };
        }

        /// <summary>Modal consulta pedidos — -1 = ainda não escolheu cliente (ver GetRelatorioConsultaPedidos).</summary>
        public static List<SelectListItem> ComboFiltroClienteSelecione()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ SELECIONE O CLIENTE ]", Selected = true }
            };
        }

        /// <summary>Atendimentos Create/Edit — 0 = não selecionado (validação param_id_cliente).</summary>
        public static List<SelectListItem> ComboPlaceholderAtendimentoCliente()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ Selecione o Cliente ]", Selected = true }
            };
        }

        /// <summary>Combo cliente atendimento: placeholder + item em edição (typeahead Ajax).</summary>
        public static List<SelectListItem> BuildComboClienteAtendimento(GdiPlataformEntities db, int idCliente)
        {
            var combo = ComboPlaceholderAtendimentoCliente();
            if (idCliente > 0)
            {
                var item = GetClienteItem(db, idCliente);
                if (item != null)
                    combo.Add(new SelectListItem { Value = item.id, Text = item.text, Selected = true });
            }
            return combo;
        }

        public static List<SelectListItem> ComboPlaceholderProduto()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ INFORME O PRODUTO ]" }
            };
        }

        /// <summary>Modal consulta pedidos — produto via Ajax (PERF/CACHE-2d).</summary>
        public static List<SelectListItem> ComboFiltroProdutoConsultaPedidos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ SELECIONE O PRODUTO ]", Selected = true },
                new SelectListItem { Value = "0", Text = "[ TODOS OS PRODUTOS ]" }
            };
        }

        /// <summary>gc/Estoque/Index — opções fixas 0/-1 + typeahead Ajax (PROD-002a; sem GetComboGcProdutosPosicaoEstoqueIndex no HTML).</summary>
        public static List<SelectListItem> ComboFiltroProdutoPosicaoEstoqueIndex()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ TODOS OS PRODUTOS ]", Selected = true },
                new SelectListItem { Value = "-1", Text = "[ PRODUTOS COM SALDO ]" }
            };
        }

        public static List<SelectListItem> ComboFiltroClienteFornecedorTodos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]", Selected = true }
            };
        }

        public static List<SelectListItem> ComboFiltroClienteFornecedorInforme()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]", Selected = true }
            };
        }

        public static List<SelectListItem> ComboFiltroClienteFornecedorSelecione()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ SELECIONE ]", Selected = true }
            };
        }

        /// <summary>g/Financeiro Index — 0 = todos.</summary>
        public static List<SelectListItem> ComboFiltroClienteFinanceiroTodos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ TODOS ]", Selected = true }
            };
        }

        /// <summary>g/Clientes Index — 0 = não selecionado.</summary>
        public static List<SelectListItem> ComboFiltroClienteCadastroSelecione()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ SELECIONE O CLIENTE ]", Selected = true }
            };
        }

        public static List<SelectListItem> BuildComboClienteFornecedor(GdiPlataformEntities db, int idCliente, bool comDoc)
        {
            var combo = comDoc
                ? new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]", Selected = true } }
                : ComboFiltroClienteFornecedorSelecione();
            if (idCliente > 0)
            {
                var item = GetClienteFornecedorItem(db, idCliente, comDoc);
                if (item != null)
                    combo.Add(new SelectListItem { Value = item.id, Text = item.text, Selected = true });
            }
            return combo;
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0) return DefaultLimit;
            return Math.Min(limit, MaxLimit);
        }

        private static string FormatClienteText(string nome, string cpf, string cnpj, int idCliente, int displayWidth)
        {
            return FormatClienteFornecedorText(nome, cpf, cnpj, idCliente, displayWidth, comDoc: false);
        }

        private static string FormatClienteFornecedorText(string nome, string cpf, string cnpj, int idCliente, int displayWidth, bool comDoc)
        {
            var n = TruncateClienteNome(nome, displayWidth);
            var doc = cpf.EmptyIfNull().ToString().Trim();
            if (doc.Length == 0) doc = cnpj.EmptyIfNull().ToString().Trim();
            if (comDoc)
                return n + "\xA0\xA0\xA0\xA0\xA0" + (doc.Length > 0 ? "[ " + doc + " ]" : "[Id: " + idCliente + "]");
            return n + "\xA0\xA0\xA0\xA0\xA0" + "[Id: " + idCliente + "]" + (doc.Length > 0 ? " [ " + doc + " ]" : string.Empty);
        }

        private static string TruncateClienteNome(string nome, int displayWidth)
        {
            var n = nome.EmptyIfNull().ToString().Trim();
            if (n.Length > 50 && displayWidth > 0 && displayWidth < 500) return n.Substring(0, 50);
            if (n.Length > 100) return n.Substring(0, 100);
            return n;
        }

        private static int ResolveSizeNomeProduto()
        {
            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
            int sizeNome = (displayWidth / 100) * 8;
            if (displayWidth > 0 && displayWidth < 500) sizeNome = (displayWidth / 100) * 10;
            if (sizeNome <= 0) sizeNome = 80;
            return sizeNome;
        }

        private static string TruncateNome(string nome, int sizeNome)
        {
            var n = nome.EmptyIfNull().ToString().Trim();
            if (n.Length == 0) return n;
            if (n.Length > sizeNome) return n.Substring(0, sizeNome) + "...";
            return n;
        }
    }
}
