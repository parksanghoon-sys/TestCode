using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace cliAccessCompareCsv.AcessModels
{
    public class DetailNimonic
    {
        [Required]
        [Description("Field")]
        public int FildIndex { get; set; }
        [Description("Field Name")]
        public string? FieldName { get; set; }
        [Description("Field Description")]
        public string? FieldDescription { get; set; }
        [Description("Remarks")]
        public string? Remark { get; set; }
        [Description("Field Default")]
        public bool? IsFieldDefault { get; set; }
        [Description("AVTE Field Default")]
        public bool? IsAVTEFieldDefault { get; set; }
        [Description("Bit Recode")]
        public List<BitNimonic> BitNimonics { get; set; } = new List<BitNimonic>();

    }
}
