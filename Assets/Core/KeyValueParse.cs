using System.Collections.Generic;
using System;

namespace Engine.Source
{
    public class KeyValueParse
    {
        public static Dictionary<String, String> Load(String[] KVFile)
        {
            Dictionary<String, String> IDictionary
                = new Dictionary<String, String>();

            foreach (String Line in KVFile)
            {
                String Param = Line.Replace("\"", " ").Replace("\t", " ").Trim().ToLower();

                if (Param.IndexOf("//") == -1)
                {
                    if (Param.Contains(" "))
                    {
                        String[] KeyValue = Param.Split(new Char[] { ' ' }, 2);

                        if (!IDictionary.ContainsKey(KeyValue[0].Trim()))
                            IDictionary.Add(KeyValue[0].Trim(), KeyValue[1].Trim());
                    }

                    // CAUTION!!! CRUTCH! (FOR *.VMT)
                    else if (Param.Length > 1)
                    {
                        if (!IDictionary.ContainsKey(Param))
                            IDictionary.Add(Param, null);
                    }
                }
            }

            return IDictionary;
        }
    }
}
