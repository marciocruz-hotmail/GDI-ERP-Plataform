using System;
using System.Linq;
using GdiPlataform.Db;

namespace GdiPlataform.Lib
{
    /// <summary>COUNT + OFFSET/FETCH para DataTables server-side (padrão PERF-006/007).</summary>
    public static class LibDataTableSqlPaging
    {
        public static int SqlCount(GdiPlataformEntities db, string sqlSelectWithOptionalOrder)
        {
            if (db == null) return 0;
            SplitOrder(sqlSelectWithOptionalOrder, out string body, out _);
            if (string.IsNullOrWhiteSpace(body)) return 0;
            string countSql = "SELECT COUNT(*) FROM (" + body + ") AS _cnt";
            return db.Database.SqlQuery<int>(countSql).FirstOrDefault();
        }

        public static string SqlPage(string sqlSelectWithOptionalOrder, int start, int length, string defaultOrderBy = null)
        {
            SplitOrder(sqlSelectWithOptionalOrder, out string body, out string order);
            if (string.IsNullOrWhiteSpace(order) && !string.IsNullOrWhiteSpace(defaultOrderBy))
                order = " ORDER BY " + defaultOrderBy;
            if (start < 0) start = 0;
            if (length <= 0) length = 20;
            return body + order + " OFFSET " + start + " ROWS FETCH NEXT " + length + " ROWS ONLY";
        }

        public static void SplitOrder(string sql, out string bodyWithoutOrder, out string orderClause)
        {
            orderClause = string.Empty;
            bodyWithoutOrder = (sql ?? string.Empty).Trim();
            int idx = bodyWithoutOrder.LastIndexOf(" order by ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return;
            orderClause = bodyWithoutOrder.Substring(idx);
            bodyWithoutOrder = bodyWithoutOrder.Substring(0, idx).Trim();
        }
    }
}
