using EarthLat.Backend.Function.Exception;

namespace EarthLat.Backend.Function.Extension
{
    public static class ValidationExtension
    {
        public static void ThrowIfIsEmptyOrWhitespace(this string value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException($"{propertyName} cannot be null empty or whitespace.", propertyName);
            }
        }

        public static void ThrowIfByreArrIsNull(this byte[] value, string propertyName)
        {
            if (value is null || value.Length <= 0)
            {
                throw new ValidationException($"{propertyName} is required.", propertyName);
            }
        }

        public static void ThrowIfNotInBetween(this double value, string propertyName, int min = 0,int max = 1)
        {
            if (value < min || value > max )
            {
                throw new ValidationException($"{propertyName} is not valid.", propertyName);
            }
        }
    }
 }

