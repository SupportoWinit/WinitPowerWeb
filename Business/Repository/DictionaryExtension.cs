using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository
{
    public static class DictionaryExtension
    {
        public static void AddOrAppend(this Dictionary<String, String> dictionary, String key, String value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = new StringBuilder(dictionary[key]).Append(", ").Append(value).ToString();
            else
                dictionary.Add(key, value);
        }
    }
}
