/* 
Copyright © 2017 Yurii Bilyk. All rights reserved. Contacts: <yuryk531@gmail.com>

This file is part of "Database integrator".

"Database integrator" is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

"Database integrator" is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with "Database integrator".  If not, see <http:www.gnu.org/licenses/>. 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingGenerator
{
    public static class SqlToXsdDtMapper
    {
        public static Dictionary<string, string> sqlToXSDMappings = new Dictionary<string, string>();

        static SqlToXsdDtMapper()
        {
            if(sqlToXSDMappings.Count==0)
            {
                sqlToXSDMappings.Add("bigint", "long");
                sqlToXSDMappings.Add("binary", "base64Binary");
                sqlToXSDMappings.Add("bit", "boolean");
                sqlToXSDMappings.Add("char", "string");
                sqlToXSDMappings.Add("datetime", "dateTime");
                sqlToXSDMappings.Add("decimal", "decimal");
                sqlToXSDMappings.Add("float", "double");
                sqlToXSDMappings.Add("image", "base64Binary");
                sqlToXSDMappings.Add("int", "int");
                sqlToXSDMappings.Add("money", "decimal");
                sqlToXSDMappings.Add("nchar", "string");
                sqlToXSDMappings.Add("ntext", "string");
                sqlToXSDMappings.Add("nvarchar", "string");
                sqlToXSDMappings.Add("numeric", "decimal");
                sqlToXSDMappings.Add("real", "float");
                sqlToXSDMappings.Add("smalldatetime", "dateTime");
                sqlToXSDMappings.Add("smallint", "short");
                sqlToXSDMappings.Add("smallmoney", "decimal");
                sqlToXSDMappings.Add("sql_variant", "string");
                sqlToXSDMappings.Add("sysname", "string");
                sqlToXSDMappings.Add("text", "string");
                sqlToXSDMappings.Add("timestamp", "dateTime");
                sqlToXSDMappings.Add("tinyint", "unsignedByte");
                sqlToXSDMappings.Add("varbinary", "base64Binary");
                sqlToXSDMappings.Add("varchar", "string");
                sqlToXSDMappings.Add("uniqueidentifier", "string");
                sqlToXSDMappings.Add("varcharmax", "string");
                sqlToXSDMappings.Add("date", "date");
            }
        }

        public static string MapSqlToXSD(string sqlDataType)
        {
            if (sqlToXSDMappings.ContainsKey(sqlDataType))
            {
                return sqlToXSDMappings[sqlDataType];
            }
            else return "string"; //if we cannot map type, we always can map it to string
        }
    }
}
