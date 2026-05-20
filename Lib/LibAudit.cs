using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;

namespace GdiPlataform.Lib
{
    public static class LibAudit
    {
        public static bool SaveAudit(GdiPlataformEntities db, bool ParamSaveDb, String TableName, int TableId, String Log)
        {
            bool Gravado = false;
            try
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                if (TableName == "g_clientes")
                {
                    g_clientes_audit RecordNew = new g_clientes_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_cliente = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.g_clientes_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "g_produtos")
                {
                    g_produtos_audit RecordNew = new g_produtos_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_produto = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.g_produtos_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "gc_comex_produtos")
                {
                    gc_comex_produtos_audit RecordNew = new gc_comex_produtos_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_comex_produto = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.gc_comex_produtos_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "gc_comex_importacoes")
                {
                    gc_comex_importacoes_audit RecordNew = new gc_comex_importacoes_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_importacao = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.gc_comex_importacoes_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "gc_parametros_difal")
                {
                    gc_parametros_difal_audit RecordNew = new gc_parametros_difal_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_parametro_difal = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.gc_parametros_difal_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "gc_movimentos")
                {
                    gc_movimentos_audit RecordNew = new gc_movimentos_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_movimento = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.gc_movimentos_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "gc_comex_importacoes_itens")
                {
                    gc_comex_importacoes_itens_audit RecordNew = new gc_comex_importacoes_itens_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_importacao_item = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.gc_comex_importacoes_itens_audit.Add(RecordNew);
                    Gravado = true;
                }
                else if (TableName == "gc_comex_invoices_itens")
                {
                    gc_comex_invoices_itens_audit RecordNew = new gc_comex_invoices_itens_audit();
                    RecordNew.crc = 0;
                    RecordNew.id_invoice_item = TableId;
                    RecordNew.audit = Log;
                    RecordNew.datahora_cadastro = DataHoraAtual;
                    RecordNew.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.gc_comex_invoices_itens_audit.Add(RecordNew);
                    Gravado = true;
                }
                if (Gravado == true)
                {
                    if (ParamSaveDb == true) { db.SaveChanges(); };
                }
            }
            catch (Exception)
            {
                Gravado = false;
            }
            return Gravado;
        }
    }
}