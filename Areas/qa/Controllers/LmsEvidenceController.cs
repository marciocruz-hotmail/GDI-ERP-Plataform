using DocumentFormat.OpenXml.Wordprocessing;
using GdiPlataform.Areas.qa.Models;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.qa.Controllers
{
    public class LmsEvidenceController : Controller
    {
        private GdiPlataformEntities db;
        public LmsEvidenceController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [HttpPost]
        public JsonResult Track(EvidenceEventDto dto)
        {
            try
            {
                if (dto == null || dto.ContentId <= 0)
                    return Json(new { ok = false, error = "Dados inválidos." });

                var contentType = (dto.ContentType ?? "").Trim().ToUpperInvariant();
                var eventType = (dto.EventType ?? "").Trim().ToUpperInvariant();

                if (contentType != "VIDEO" && contentType != "PDF")
                    return Json(new { ok = false, error = "Tipo de conteúdo inválido." });

                if (eventType != "OPEN" && eventType != "HEARTBEAT" && eventType != "COMPLETE" && eventType != "ACK")
                    return Json(new { ok = false, error = "Evento inválido." });

                int userId = GetCurrentUserId();  // implemente conforme seu padrão
                string ip = Request.UserHostAddress;
                string ua = (Request.UserAgent ?? "");
                if (ua.Length > 300) ua = ua.Substring(0, 300);

                using (var cn = new SqlConnection(db.Database.Connection.ConnectionString))
                {
                    cn.Open();
                    using (var tx = cn.BeginTransaction())
                    {
                        SaveEvidenceEvent(userId, dto, contentType, eventType, ip, ua);
                        UpsertEvidenceStatus(userId, dto, contentType, eventType);
                        tx.Commit();
                    }
                }

                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { ok = false, error = ex.Message, stack = ex.StackTrace });
            }
        }
        private int GetCurrentUserId()
        {
            if (CachePersister.userIdentity == null)
                throw new InvalidOperationException("Usuário não autenticado.");
            return CachePersister.userIdentity.IdUsuario;
        }

        private void SaveEvidenceEvent(int userId, EvidenceEventDto dto, string contentType, string eventType, string ip, string ua)
        {
            using (var cn = new SqlConnection(db.Database.Connection.ConnectionString))
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                INSERT INTO dbo.qa_lms_evidence_event
                (id_usuario, id_conteudo, tipo_conteudo, evento, current_seconds, duration_seconds, percent_viewed, page_number, total_pages, ip_address, user_agent)
                VALUES
                (@id_usuario, @id_conteudo, @tipo_conteudo, @evento, @current_seconds, @duration_seconds, @percent_viewed, @page_number, @total_pages, @ip_address, @user_agent);";

                cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@id_conteudo", SqlDbType.Int).Value = dto.ContentId;
                cmd.Parameters.Add("@tipo_conteudo", SqlDbType.VarChar, 10).Value = contentType;
                cmd.Parameters.Add("@evento", SqlDbType.VarChar, 20).Value = eventType;

                cmd.Parameters.Add("@current_seconds", SqlDbType.Int).Value = (object)dto.CurrentSeconds ?? DBNull.Value;
                cmd.Parameters.Add("@duration_seconds", SqlDbType.Int).Value = (object)dto.DurationSeconds ?? DBNull.Value;
                cmd.Parameters.Add("@percent_viewed", SqlDbType.Int).Value = (object)dto.PercentViewed ?? DBNull.Value;

                cmd.Parameters.Add("@page_number", SqlDbType.Int).Value = (object)dto.PageNumber ?? DBNull.Value;
                cmd.Parameters.Add("@total_pages", SqlDbType.Int).Value = (object)dto.TotalPages ?? DBNull.Value;

                cmd.Parameters.Add("@ip_address", SqlDbType.NVarChar, 45).Value = (object)ip ?? DBNull.Value;
                cmd.Parameters.Add("@user_agent", SqlDbType.NVarChar, 300).Value = (object)ua ?? DBNull.Value;

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void UpsertEvidenceStatus(int userId, EvidenceEventDto dto, string contentType, string eventType)
        {
            // cálculo simples do percentual (servidor confere coerência)
            int pct = 0;
            if (contentType == "VIDEO" && dto.DurationSeconds.HasValue && dto.DurationSeconds.Value > 0 && dto.CurrentSeconds.HasValue)
            {
                pct = (int)Math.Floor((dto.CurrentSeconds.Value * 100.0) / dto.DurationSeconds.Value);
                if (pct < 0) pct = 0;
                if (pct > 100) pct = 100;
            }
            else if (dto.PercentViewed.HasValue)
            {
                pct = Math.Max(0, Math.Min(100, dto.PercentViewed.Value));
            }

            bool markComplete =
                (contentType == "VIDEO" && (eventType == "COMPLETE" || pct >= 90)) ||
                (contentType == "PDF" && eventType == "ACK");

            using (var cn = new SqlConnection(db.Database.Connection.ConnectionString))
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                MERGE dbo.qa_lms_evidence_status AS t
                USING (SELECT @id_usuario AS id_usuario, @id_conteudo AS id_conteudo) AS s
                ON (t.id_usuario = s.id_usuario AND t.id_conteudo = s.id_conteudo)
                WHEN MATCHED THEN
                    UPDATE SET
                        tipo_conteudo  = @tipo_conteudo,
                        started_at     = COALESCE(t.started_at, CASE WHEN @evento = 'OPEN' THEN SYSDATETIME() ELSE t.started_at END),
                        last_seen_at   = SYSDATETIME(),
                        percent_viewed = CASE WHEN @pct > t.percent_viewed THEN @pct ELSE t.percent_viewed END,
                        completed_at   = CASE WHEN @markComplete = 1 THEN COALESCE(t.completed_at, SYSDATETIME()) ELSE t.completed_at END
                WHEN NOT MATCHED THEN
                    INSERT (id_usuario, id_conteudo, tipo_conteudo, started_at, last_seen_at, completed_at, percent_viewed)
                    VALUES (@id_usuario, @id_conteudo, @tipo_conteudo,
                            CASE WHEN @evento = 'OPEN' THEN SYSDATETIME() ELSE NULL END,
                            SYSDATETIME(),
                            CASE WHEN @markComplete = 1 THEN SYSDATETIME() ELSE NULL END,
                            @pct);";

                cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@id_conteudo", SqlDbType.Int).Value = dto.ContentId;
                cmd.Parameters.Add("@tipo_conteudo", SqlDbType.VarChar, 10).Value = contentType;
                cmd.Parameters.Add("@evento", SqlDbType.VarChar, 20).Value = eventType;
                cmd.Parameters.Add("@pct", SqlDbType.Int).Value = pct;
                cmd.Parameters.Add("@markComplete", SqlDbType.Bit).Value = markComplete ? 1 : 0;

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}