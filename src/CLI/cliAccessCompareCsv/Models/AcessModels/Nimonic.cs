using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliAccessCompareCsv.AcessModels
{
    public class Nimonic
    {
        [Required]
        [Description("ICD 식별자")]
        public string Id { get; set; }
        [Description("명칭(니모닉)")]
        public string? Title { get; set; }
        [Description("설명")]
        public string? Description { get; set; }
        [Description("4586 Message Type")]
        public string? Type { get; set; }
        [Description("적용 범위")]
        public string? Range { get; set; }
        [Description("하위레코드")]
        public List<DetailNimonic> DetailNimonics { get; set; } = new List<DetailNimonic>();
    }
}
