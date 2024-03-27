using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace cliAccessCompareCsv.AcessModels
{
    public class BitNimonic
    {
        [Required]
        [Description("Bit No")]
        public string BitNumber { get; set; }
        [Description("Bit Name")]
        public string? BitName { get; set; }
        [Description("Bit Description")]
        public string? BitDescription { get; set; }
        [Description("Bit Default")]
        public bool IsBitDefault { get; set; }
        [Description("AVTE Bit Default")]
        public bool? IsAVTEFieldDefault { get; set; }

    }
}
