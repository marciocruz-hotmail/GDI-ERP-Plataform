using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.qa.Models
{
    public class EvidenceEventDto
    {
        public int ContentId { get; set; }           // id do conteúdo (id_arquivo GED ou id LMS)
        public string ContentType { get; set; }      // "VIDEO" | "PDF"
        public string EventType { get; set; }        // "OPEN" | "HEARTBEAT" | "COMPLETE" | "ACK"
        public int? CurrentSeconds { get; set; }
        public int? DurationSeconds { get; set; }
        public int? PercentViewed { get; set; }
        public int? PageNumber { get; set; }
        public int? TotalPages { get; set; }
    }
}