using System;

namespace GdiPlataform.Controllers
{
    /// <summary>
    /// Modelo de requisição para disparar um job via API JobServer.
    /// O chamador deve enviar os parâmetros corretos (ex.: Key configurada no Web.config).
    /// </summary>
    public class JobRunRequest
    {
        /// <summary>Chave de autorização (deve coincidir com appSettings JobServer:Key no Web.config).</summary>
        public string Key { get; set; }

        /// <summary>Nome ou identificador do job a executar (para uso na lógica do processamento).</summary>
        public string JobName { get; set; }

        /// <summary>Parâmetros adicionais em texto (ex.: JSON) para o job.</summary>
        public string Parameters { get; set; }
    }
}
