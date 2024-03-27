using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace cliAccessCompareCsv.Models.AcessModels
{
    public class StatusNimonic
    {
        [Required]
        [Description("명칭(니모닉)")]
        public string Id { get; set; }
        [Description("Bit Name")]
        public string? BitName { get; set; }
        [Description("field Name")]
        public string? FieldName { get; set; }
        public string FullName => Id + "_" + FieldName + "@" + BitName;
    }
}
