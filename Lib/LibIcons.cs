using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibIcons
    {
        public static String getIcon(string icon, string title, string color, string size)
        {
            string icone = "<i class='[icon][size]' style='[color]cursor: pointer;' data-bs-toggle='tooltip' data-bs-placement='auto'[title]></i>";

            if (color.EmptyIfNull().ToString().Trim().ToLowerInvariant() == "gray") { color = "#808080"; }
            else if (color.EmptyIfNull().ToString().Trim().ToLowerInvariant() == "green") { color = "#008000"; }
            else if (color.EmptyIfNull().ToString().Trim().ToLowerInvariant() == "red") { color = "#cc0000"; }
            else if (color.EmptyIfNull().ToString().Trim().ToLowerInvariant() == "orange") { color = "#ce7e00"; }
            else if (color.EmptyIfNull().ToString().Trim().ToLowerInvariant() == "blue") { color = "#0dcaf0"; };

            if (icon.Trim().Length > 0) { icone = icone.Replace("[icon]", icon); }
            else { icone = icone.Replace("[icon]", ""); }

            if (size.Trim().Length > 0) { icone = icone.Replace("[size]", " " + size); }
            else { icone = icone.Replace("[size]", ""); }

            if (color.Trim().Length > 0) { icone = icone.Replace("[color]", "color: " + color + "; "); }
            else { icone = icone.Replace("[color]", ""); }

            if (title.Trim().Length > 0) { icone = icone.Replace("[title]", " title='" + title + "'"); }
            else { icone = icone.Replace("[title]", ""); }

            return icone;
        }

    }
}
