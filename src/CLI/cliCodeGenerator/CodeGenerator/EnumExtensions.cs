namespace CodeGenerator
{
    public static class EnumExtensions
    {
        public static string ToStringFast(this Colour colour)
            => colour switch
            {
                Colour.Red => nameof(Colour.Red),
                Colour.Blue => nameof(Colour.Blue),
                _ => colour.ToString(),
            };
    }
}
