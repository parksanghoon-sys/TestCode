using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace wpfEnumMarkupExtension
{
    public enum ETest
    {
        A,
        B,
        C,
        D,
        E
    }
    internal class EnumBindingSourceExtension : MarkupExtension
    {
        private Type _enumType;
        public EnumBindingSourceExtension(Type enumType)
        {
            _enumType = enumType;
        }
        public Type EnumType
        {
            get => _enumType;
            private set
            {
                if (value != this._enumType)
                {
                    Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                    if (enumType.IsEnum == false)
                        throw new ArgumentException("Enum 타입이 아닙니다");
                    this._enumType = value;
                }
            }
        }
        public string Fillter { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if(_enumType == null)
                throw new ArgumentException("Enum 타입이 아닙니다");
            var enumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
            Array enumValuews = Enum.GetValues(enumType);

            if(enumType == _enumType)
            {
                if(string.IsNullOrWhiteSpace(Fillter))
                {
                    return enumValuews;
                }
                else
                {
                    var fillterArr = Fillter.ToString().Split(new string[] { ","}, StringSplitOptions.RemoveEmptyEntries);
                    return enumValuews.Cast<Enum>().Where(p => fillterArr.Contains(p.ToString()));
                }
            }
            {
                Array enumVarArr = Array.CreateInstance(enumType, enumValuews.Length + 1);
                enumValuews.CopyTo(enumVarArr, 1);
                if(string.IsNullOrWhiteSpace(Fillter) )
                {
                    return enumValuews;

                }
                else
                {
                    var fillterArr = Fillter.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    return enumValuews.Cast<Enum>().Where(p => fillterArr.Contains(p.ToString()));
                }
            }

        }
    }
}
