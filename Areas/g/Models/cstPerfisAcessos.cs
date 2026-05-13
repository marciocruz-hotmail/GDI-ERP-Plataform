using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstPerfisAcessos
    {
        public int id_sistema_controller { get; set; }
        public string grupo { get; set; }
        public string title_perfil { get; set; }
        public string title_menu { get; set; }
        public int id_sistema { get; set; }
        public bool is_crud { get; set; }
        public bool is_process { get; set; }
        public bool is_report { get; set; }
        public bool is_panel { get; set; }
        public int id_sistema_modulo { get; set; }
        public int id_sistema_controller_pai { get; set; }
        public Nullable<int> id_perfil_acesso { get; set; }
        public bool ativo { get; set; }
        public bool action_run { get; set; }
        public bool action_create { get; set; }
        public bool action_read { get; set; }
        public bool action_update { get; set; }
        public bool action_delete { get; set; }
        public bool action_manager { get; set; }
    }
}