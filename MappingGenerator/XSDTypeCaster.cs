using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingGenerator
{
    public class XSDTypeCaster : ITypeCaster
    {
        class StringInvariantEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.ToLower().CompareTo(y.ToLower()) == 0;
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        };

        Dictionary<string, string[]> typeCastTable = new Dictionary<string, string[]>();
        public XSDTypeCaster()
        {
            typeCastTable.Add("date", new string [] { "dateTime" });
            typeCastTable.Add("float", new string[] { "double" });
            typeCastTable.Add("long", new string[] { "integer" });
            typeCastTable.Add("integer", new string[] { "decimal" });
            typeCastTable.Add("int", new string[] { "integer", "long", "decimal" });
            typeCastTable.Add("short", new string[] { "int", "integer", "long", "decimal" });
            typeCastTable.Add("byte", new string[] { "short", "int", "integer", "long", "decimal" });
            typeCastTable.Add("unsignedByte", new string[] { "unsignedShort", "unsignedInt", "unsignedLong", "nonNegativeInteger", "integer", "decimal" });
            typeCastTable.Add("unsignedShort", new string[] { "unsignedInt", "unsignedLong", "nonNegativeInteger", "integer", "decimal" });
            typeCastTable.Add("unsignedInt", new string[] { "unsignedLong", "nonNegativeInteger", "integer", "decimal" });
            typeCastTable.Add("unsignedLong", new string[] { "nonNegativeInteger", "integer", "decimal" });
            typeCastTable.Add("positiveInteger", new string[] { "nonNegativeInteger", "integer", "decimal" });
            typeCastTable.Add("nonNegativeInteger", new string[] { "integer", "decimal" });
            typeCastTable.Add("nonPositiveInteger", new string[] { "integer", "decimal" });
            typeCastTable.Add("negativeInteger", new string[] { "nonPositiveInteger", "integer", "decimal" });

            ////replicate for lower case
            //foreach(var key in typeCastTable.Keys)
            //{
            //    typeCastTable.Add(key.ToLower(), typeCastTable[key]);
            //}
        }

        string ITypeCaster.CastTypes(string typeName1, string typeName2)
        {
            typeName1 = typeName1.Replace("xsd:", "");
            typeName2 = typeName2.Replace("xsd:", "");
            if (typeName1.Contains(":") || typeName2.Contains(":"))
            {
                throw new ArgumentException("Type name can't contain ':' symbol, did you forget to trim the xsd: or xs: type name?");
            }

            if(typeName1.ToLower().CompareTo(typeName2.ToLower())==0)
            {
                //if typeNames are equal
                return typeName1;
            }

            if (typeCastTable.ContainsKey(typeName1))
            {
                //look for typeName2 in array of compatible types for typeName1
                if (typeCastTable[typeName1].Contains(typeName2, new StringInvariantEqualityComparer()))
                {
                    string biggerTypeName = typeCastTable[typeName1].FirstOrDefault(s2 => s2.CompareTo(typeName2) == 0);
                    if (biggerTypeName == null || biggerTypeName.Length == 0)
                        throw new Exception("Something unexpected happened in XSDTypeCaster.CastTypes(string,string)");
                    else
                        return biggerTypeName;
                }
                else return "string"; //can't cast types -> fallback to string
            }
            else
                return "string"; //if we cannot cast types, we can cast both types to string
        }
    }
}
