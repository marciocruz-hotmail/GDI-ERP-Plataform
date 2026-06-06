using System.Web;
using System.Web.Optimization;

namespace GDI_ERP_Plataform
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // SweetAlert2 11.x + GdiSwal2 (startprime; ordem fixa: Swal antes do shim)
            bundles.Add(new ScriptBundle("~/bundles/libui-swal-compat").Include(
                      "~/LibUI_AdminLTE-4.0.0/plugins/sweetalert2/sweetalert2.min.js",
                      "~/LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal2-dialog-shim.js"));
        }
    }
}
