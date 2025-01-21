﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CodeGenerator
{
    public readonly record struct EnumToGenerate
    {
        public readonly string Name;
        public readonly EquatableArray<string> Values;

        public EnumToGenerate(string name, List<string> values)
        {
            Name = name;
            Values = new(values.ToArray());
        }
    }
}

