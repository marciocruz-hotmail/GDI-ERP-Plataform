// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    public class GlobalController : Controller
    {
        private GdiPlataformEntities db;

        public GlobalController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region AjaxGetFileProcessamento
        public FileResult AjaxGetFileProcessamento(int? id)
        {
            if (db == null)
            {
                const string msg = "A base de dados da sessão não está disponível. Efetue nova conexão.";
                return File(Encoding.UTF8.GetBytes(msg), "text/plain", "erro-sessao.txt");
            }
            string filePath = "fileError";
            string fileName = "fileError";
            string contentType = "text/plain";
            g_processamento record_g_processamento = db.g_processamento.Find(id);
            if (record_g_processamento != null)
            {
                filePath = record_g_processamento.pathfile.ToString();
                fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith("csv")) { contentType = "application/octet-stream"; }
                else if (fileName.EndsWith("zip")) { contentType = "application/zip"; }
                else if (fileName.EndsWith("xml")) { contentType = "text/xml"; }
                else if (fileName.EndsWith("xls")) { contentType = "application/excel"; }
                else if (fileName.EndsWith("pdf")) { contentType = "application/pdf"; }
                else if (fileName.EndsWith("doc")) { contentType = "application/msword"; }
                else if (fileName.EndsWith("txt")) { contentType = "text/plain"; };
            };
            return File(filePath, contentType, fileName);
        }
        #endregion


        public FileResult AjaxGetFileResultDownload(string url)
        {
            string filePath = url;
            string fileName = Path.GetFileName(filePath);
            string contentType = "text/plain";
            if (fileName.EndsWith("csv")) { contentType = "application/octet-stream"; }
            else if (fileName.EndsWith("zip")) { contentType = "application/zip"; }
            else if (fileName.EndsWith("xml")) { contentType = "text/xml"; }
            else if (fileName.EndsWith("xls")) { contentType = "application/excel"; }
            else if (fileName.EndsWith("pdf")) { contentType = "application/pdf"; }
            else if (fileName.EndsWith("doc")) { contentType = "application/msword"; }
            else if (fileName.EndsWith("txt")) { contentType = "text/plain"; };
            return File(filePath, contentType, fileName);
        }

    }
}