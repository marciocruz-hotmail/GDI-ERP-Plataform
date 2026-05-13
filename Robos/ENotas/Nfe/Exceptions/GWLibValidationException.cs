using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Nfe.Exceptions
{
    public class GWLibValidationException : GWLibException
    {
        public GWLibValidationException(GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = GWLibMessages.ValidationError;
        }

        public GWLibValidationException(string message, GWLibErro[] errors)
            : base(errors)
        {
            this.Summary = message;
        }
    }
}