using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Models
{
    public class ModelControlTableUpdate
    {
        public string TableName { get; set; }
        public string ProcessName { get; set; }
        /// <summary>Último MAX(datahora_*) observado na tabela (invalidação quando sobe).</summary>
        public DateTime DateTimeUpdate { get; set; }
        /// <summary>Última verificação IsTableUpdate (PERF-015 — evita MAX repetido dentro do TTL).</summary>
        public DateTime DateTimeLastVerified { get; set; }
    }
}