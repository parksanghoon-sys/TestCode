using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfEnumDescriptionCombobox
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum TcasDisplayRange
    {
        [Description("5")]
        Rate5 = 5,
        [Description("10")]
        Rate10 = 10,
        [Description("20")]
        Rate20 = 20,
        [Description("40")]
        Rate40 = 40
        // 5, 10, 20, 40
    }
}
