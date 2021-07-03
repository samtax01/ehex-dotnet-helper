using System;
using Newtonsoft.Json;


// ReSharper disable once CheckNamespace
#pragma warning disable 1570
namespace Ehex.Helpers
{
    
    
    /// <summary>
    /// Helper Class
    /// @version: 1.0
    /// @repo: https://github.com/samtax01/ehex-dotnet-helper
    ///
    /// Object Converter
    ///     var bar = new Bar(){...};
    ///     var foo = ObjectConverter.To<Foo>(bar)
    /// </summary>
    public static class ObjectConverter
    {

        /// <summary>
        /// Convert from one Object to another
        /// </summary>
        public static T To<T>(object from)
        {
            try
            {
                var value = JsonConvert.SerializeObject(from);
                return (typeof(T) == typeof(string)) ? (T) (object) value : JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception e)
            {
                throw new Exception($"Conversion failed: Unable to convert from Type {from.GetType().Name} to Type {typeof(T).Name}");
            }
        }
    }


}