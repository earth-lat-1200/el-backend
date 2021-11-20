namespace EarthLat.Backend.Core.Extensions
{
    public static class ArgumentExtensions
    {
        public static bool ThrowIfIsNullEmptyOrWhitespace(this string value, string propertyName) => 
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"'{propertyName}' cannot be null or whitespace.", propertyName) : true;
    }
}
