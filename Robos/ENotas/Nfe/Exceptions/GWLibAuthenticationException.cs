using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe.Exceptions
{
    public class GWLibAuthenticationException : GWLibException
    {
        public GWLibAuthenticationException(GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = GWLibMessages.AuthenticationError;
        }

        public GWLibAuthenticationException(string message, GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = message;
        }
    }
}