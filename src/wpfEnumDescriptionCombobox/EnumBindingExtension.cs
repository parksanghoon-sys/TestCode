using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace wpfEnumDescriptionCombobox
{
    public class EnumBindingExtension : MarkupExtension
    {
        public Type EnumType { get; private set; }
        public EnumBindingExtension(Type type)
        {
            if (type == null || !type.IsEnum)
                throw new ArgumentException("Not Enum");
            EnumType = type;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(EnumType);
        }
    }
}
