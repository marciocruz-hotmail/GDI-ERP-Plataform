using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace GdiPlataform.Robos.Nfe.Exceptions
{
    public class GWLibException : Exception
    {
        protected string Summary { get; set; }

        public GWLibException()
        {
        }

        public GWLibErro[] Errors { get; protected set; }

        public GWLibException(string message)
            : base(message)
        {
        }

        public GWLibException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public GWLibException(GWLibErro[] errors)
        {
            this.Errors = errors;
        }

        public override string Message
        {
            get
            {
                String msgErro = this.Summary.ToString().Trim().Replace("\r", "").Replace("\n", "").Replace("\n", "").Replace("400 - Bad Request", "400 - Bad Request ");

                if (this.Errors != null)
                {
                    if (this.Errors.Length > 0)
                    {
                        msgErro += " (" + this.Errors.Length.ToString() + ")";
                    }
                }

                /*sb.AppendLine(this.Summary);
                if (this.Errors != null)
                {
                    if (this.Errors.Length > 0)
                    {
                        sb.AppendFormat("\r\n{0}:\r\n", GWLibMessages.Errors);
                        foreach (var error in this.Errors)
                        {
                            sb.AppendLine(error.ToString());
                        }
                    }
                }*/

                return msgErro;
            }
        }
    }
}