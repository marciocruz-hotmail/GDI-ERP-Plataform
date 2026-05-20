using System.Web;

using System.Web.Optimization;



namespace GDI_ERP_Plataform

{

    public class BundleConfig

    {

        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862

        public static void RegisterBundles(BundleCollection bundles)

        {

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(

                        "~/Scripts/jquery-{version}.js"));



            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(

                        "~/Scripts/jquery.validate*"));



            bundles.Add(new Bundle("~/bundles/bootstrap").Include(

                      "~/Scripts/bootstrap.js"));



            // SweetAlert2 11.x + GdiSwal2 (startprime; ordem fixa: Swal antes do shim)

            bundles.Add(new ScriptBundle("~/bundles/libui-swal-compat").Include(

                      "~/LibUI_AdminLTE-4.0.0/plugins/sweetalert2/sweetalert2.min.js",

                      "~/LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal2-dialog-shim.js"));



            bundles.Add(new StyleBundle("~/Content/css").Include(

                      "~/Content/bootstrap.css",

                      "~/Content/site.css"));

        }

    }

}


