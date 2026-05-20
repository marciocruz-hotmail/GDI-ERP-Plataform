using GdiPlataform.Models;
using System.Collections.Generic;
using GdiPlataform.Db;

namespace GdiPlataform.Domain
{
    /// <summary>Sessão UI (navbar, identidade). Lookups em MemoryCache via <see cref="Lib.Lookups.ILookupQueryService"/> (Onda 6b).</summary>
    public class ContextoModel
    {
        public g_filiais g_filial { get; set; }
        public string versaoPlataforma { get; set; }
        public List<NavbarItemMessage> allNavbarItemMessage { get; set; }
        public List<NavbarItemMenu> allNavbarItemMenu { get; set; }
        public List<NavbarItemTask> allNavbarItemTask { get; set; }
        public List<NavbarItemAlert> allNavbarItemAlert { get; set; }
        public List<NavbarItemAtividade> allNavbarItemAtividade { get; set; }
        public UserIdentity userIdentity { get; set; }

        public ContextoModel()
        {
            userIdentity = new UserIdentity();
            allNavbarItemMessage = new List<NavbarItemMessage>();
            allNavbarItemMenu = new List<NavbarItemMenu>();
            allNavbarItemTask = new List<NavbarItemTask>();
            allNavbarItemAlert = new List<NavbarItemAlert>();
            allNavbarItemAtividade = new List<NavbarItemAtividade>();
        }
    }
}
