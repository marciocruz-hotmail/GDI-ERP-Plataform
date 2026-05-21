using System.Web.Mvc;

namespace GdiPlataform.Lib
{
    /// <summary>Flash TempData para PRG (modal de erro HTML e mensagens de login).</summary>
    public static class LibFlashMessage
    {
        public const string ModalMessageKey = "message";
        public const string ErrorKey = "Error";
        public const string InfoKey = "Info";

        /// <summary>Mensagem HTML exibida em <c>Error/ModalError</c> (mantém TempData no próximo request).</summary>
        public static void SetModalMessage(Controller controller, string htmlMessage)
        {
            if (controller == null) { return; }
            controller.TempData[ModalMessageKey] = htmlMessage ?? string.Empty;
            controller.TempData.Keep(ModalMessageKey);
        }

        public static void SetError(Controller controller, string message)
        {
            if (controller == null) { return; }
            controller.TempData[ErrorKey] = message ?? string.Empty;
            controller.TempData.Keep(ErrorKey);
        }

        public static void SetInfo(Controller controller, string message)
        {
            if (controller == null) { return; }
            controller.TempData[InfoKey] = message ?? string.Empty;
            controller.TempData.Keep(InfoKey);
        }
    }
}
