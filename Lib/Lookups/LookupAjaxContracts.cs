using System.Collections.Generic;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Contrato JSON typeahead 1.6 — Select2 processResults mapeia para { id, text }.</summary>
    public sealed class LookupAjaxItemDto
    {
        public string id { get; set; }
        public string text { get; set; }
    }

    public sealed class LookupAjaxItemsResponse
    {
        public List<LookupAjaxItemDto> items { get; set; } = new List<LookupAjaxItemDto>();
        public string errorMessage { get; set; }
        public string severity { get; set; }
    }
}
