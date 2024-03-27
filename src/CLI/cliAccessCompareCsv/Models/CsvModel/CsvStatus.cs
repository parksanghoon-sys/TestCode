using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliAccessCompareCsv.CsvModel
{
    public class CsvStatus
    {
        // FieldName 일치여부 체크
        // ex) IMC_PPC_AVN_STATUS_01
        public string? StatusClass { get; set; }
        // BitName 와 같아야함
        // ex ) IFF_Crypto_Zeroize_Status
        public string? Property { get; set; }
        public string? Name { get; set; }

    }
}
