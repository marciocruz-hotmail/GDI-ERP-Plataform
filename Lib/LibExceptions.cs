using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibExceptions
    {
        public static String getExceptionShortMessage(Exception e)
        {
            string shortMessage = string.Empty;
            if ((e.Message != null) && (e.Message.ToString().Trim().Length > 0))
            {
                shortMessage = e.Message;
            }
            while ((e.InnerException != null) && (e.InnerException.Message.ToString() != String.Empty))
            {
                shortMessage +=  "\r\n" + e.InnerException.Message.ToString();
                e = e.InnerException;
            }
            return shortMessage;
        }

        public static String getDbEntityValidationException(DbEntityValidationException ex)
        {
            string shortMessage = string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (var failure in ex.EntityValidationErrors)
            {
                sb.AppendFormat("{0} falha na validação dos dados\n", failure.Entry.Entity.GetType());
                foreach (var error in failure.ValidationErrors)
                {
                    sb.AppendFormat("- {0} : {1}", error.PropertyName, error.ErrorMessage);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public static String getWebException(WebException ex)
        {
            string MsgWebException = string.Empty;
            using (var stream = ex.Response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                MsgWebException = reader.ReadToEnd();
            }
            MsgWebException = MsgWebException.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
            return MsgWebException;
        }
    }
}