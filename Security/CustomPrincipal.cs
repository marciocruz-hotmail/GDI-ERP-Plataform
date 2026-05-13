using System;
using System.Linq;
using System.Security.Principal;
using GdiPlataform.Models;
using Microsoft.Graph;

namespace GdiPlataform.Security
{
    public class CustomPrincipal : IPrincipal
    {
        private UserIdentity UserIndentity;

        public CustomPrincipal(UserIdentity userIdentity)
        {
            this.UserIndentity = userIdentity;
            this.Identity = new GenericIdentity(userIdentity.Username);
        }

        public IIdentity Identity
        {
            get;
            set;
        }

        public bool IsInRole(string role)
        {
            var roles = role.Split(new char[] { ',' });
            return roles.Any(r => this.UserIndentity.Roles.Contains(r));
        }
    }
}