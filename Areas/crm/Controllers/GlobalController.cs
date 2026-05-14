using System.IO;
using System.Text;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.crm.Controllers
{
    [CustomAuthorize(Roles = "gc_PortalCliente_PortalFinanceiro")]
    public class GlobalController : Controller
    {
        private GdiPlataformEntities db;

        public GlobalController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(string.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

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
            g_processamento recordGProcessamento = db.g_processamento.Find(id);
            if (recordGProcessamento != null)
            {
                filePath = recordGProcessamento.pathfile.ToString();
                fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith("csv")) { contentType = "application/octet-stream"; }
                else if (fileName.EndsWith("zip")) { contentType = "application/zip"; }
                else if (fileName.EndsWith("xml")) { contentType = "text/xml"; }
                else if (fileName.EndsWith("xls")) { contentType = "application/excel"; }
                else if (fileName.EndsWith("pdf")) { contentType = "application/pdf"; }
                else if (fileName.EndsWith("doc")) { contentType = "application/msword"; }
                else if (fileName.EndsWith("txt")) { contentType = "text/plain"; }
            }
            return File(filePath, contentType, fileName);
        }
    }
}
