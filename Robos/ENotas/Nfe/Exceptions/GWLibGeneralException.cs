using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe.Exceptions
{
    public class GWLibGeneralException : GWLibException
    {
        public GWLibGeneralException(string message)
            : base(message)
        {
            this.Summary = message;
        }
        public GWLibGeneralException(GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = GWLibMessages.GeneralError;
        }

        public GWLibGeneralException(string message, GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = message;
        }

        public GWLibGeneralException(string message, Exception inner)
            : base(message, inner)
        {
            this.Summary = message;
        }
    }
}