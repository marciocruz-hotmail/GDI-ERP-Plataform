using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Controllers;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.a.Controllers
{
    public class AuditController : Controller
    {
        private GdiPlataformEntities db;

        public AuditController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region GetAuditTrail
        public ActionResult GetAuditTrail(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            int IdTable = 0;
            int QtdRegistros = 0;
            int.TryParse(param.yesCustomIdPK, out IdTable);
            List<string[]> list = new List<string[]>();
            String TableName = param.yesCustomField01.EmptyIfNull().ToString().Trim();
            var allUsuarios = db.g_usuarios.ToList();

            if (TableName == "g_clientes")
            {
                var ListaAudit = db.g_clientes_audit.Select(l => l).Where(l => l.id_cliente == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "g_produtos")
            {
                var ListaAudit = db.g_produtos_audit.Select(l => l).Where(l => l.id_produto == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "gc_comex_produtos")
            {
                var ListaAudit  = db.gc_comex_produtos_audit.Select(l => l).Where(l => l.id_comex_produto == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "gc_comex_importacoes")
            {
                var ListaAudit  = db.gc_comex_importacoes_audit.Select(l => l).Where(l => l.id_importacao == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "gc_parametros_difal")
            {
                var ListaAudit  = db.gc_parametros_difal_audit.Select(l => l).Where(l => l.id_parametro_difal == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "gc_movimentos")
            {
                var ListaAudit  = db.gc_movimentos_audit.Select(l => l).Where(l => l.id_movimento == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "gc_comex_importacoes_itens")
            {
                var ListaAudit  = db.gc_comex_importacoes_itens_audit.Select(l => l).Where(l => l.id_importacao_item == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            else if (TableName == "gc_comex_invoices_itens")
            {
                var ListaAudit  = db.gc_comex_invoices_itens_audit.Select(l => l).Where(l => l.id_invoice_item == IdTable).OrderBy(l => l.datahora_cadastro).ToList();
                QtdRegistros = ListaAudit.Count();
                foreach (var RecordLog in ListaAudit)
                {
                    String NomeUsuario = RecordLog.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                    try { NomeUsuario += "<br/>" + allUsuarios.Find(u => u.id_usuario == RecordLog.id_usuario_cadastro).nome.EmptyIfNull().ToString(); } catch (Exception) { };
                    list.Add(new[] {
                                    NomeUsuario,
                                    RecordLog.audit.EmptyIfNull().ToString()
                                });
                }
            }
            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = "0",
                sEcho = param.sEcho,
                iTotalRecords = QtdRegistros,
                iTotalDisplayRecords = QtdRegistros,
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param);
            }
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

    }
}