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

        public static List<SelectListItem> ComboPlaceholderProduto()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "[ INFORME O PRODUTO ]" }
            };
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0) return DefaultLimit;
            return Math.Min(limit, MaxLimit);
        }

        private static string FormatClienteText(string nome, string cpf, string cnpj, int idCliente, int displayWidth)
        {
            var n = TruncateClienteNome(nome, displayWidth);
            var doc = cpf.EmptyIfNull().ToString().Trim();
            if (doc.Length == 0) doc = cnpj.EmptyIfNull().ToString().Trim();
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
