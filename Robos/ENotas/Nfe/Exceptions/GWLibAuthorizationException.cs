using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe.Exceptions
{
    public class GWLibAuthorizationException : GWLibException
    {
        public GWLibAuthorizationException(GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = GWLibMessages.AuthorizationError;
        }

        public GWLibAuthorizationException(string message, GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = message;
        }
    }
}