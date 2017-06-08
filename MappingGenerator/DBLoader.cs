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
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace MappingGenerator
{
    public class RawTriple
    {
        public string Subj { get; set; }
        public string Pred { get; set; }
        public string Obj { get; set; }
        public override string ToString()
        {
            return $"{Subj} {Pred} {Obj}";
        }
    }

    public class DBLoader
    {
        string connString;

        public DBLoader(string connString)
        {
            this.connString = connString;
        }

        public List<string> GetTableNames(bool includeViews = false)
        {
            List<string> tables = new List<string>();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                DataTable schema = conn.GetSchema("Tables");
                foreach (DataRow row in schema.Rows)
                {
                    tables.Add((string)row[2]);
                }

                if (!includeViews)
                {
                    schema = conn.GetSchema("Views");
                    foreach (DataRow row in schema.Rows)
                    {
                        tables.Remove((string)row[2]);
                    }
                }
            }
            tables.Remove("sysdiagrams");
            return tables;
        }

        public List<string> GetColumnNames(string tableName)
        {
            List<string> columns = new List<string>();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string[] restrictions = new string[4] { null, null, tableName, null };
                conn.Open();
                columns = conn.GetSchema("Columns", restrictions).AsEnumerable().Select(s => s.Field<String>("Column_Name")).ToList();
            }
            return columns;
        }

        public List<RawTriple> GetTriplesFromTable(string tableName, params string[] tableColumns)
        {
            List<RawTriple> triples = new List<RawTriple>();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand cmd = conn.CreateCommand();
                string columns;
                if (tableColumns.Length == 0)
                {
                    columns = string.Join(",", GetColumnNames(tableName));
                }
                else
                    columns = string.Join(",", tableColumns);

                cmd.CommandText = $"SELECT {columns} FROM [{tableName}]";
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                int counter = 0; //in case there's no ID field
                while (reader.Read())
                {
                    foreach (string colName in columns.Split(','))
                    {
                        RawTriple triple = new RawTriple
                        {
                            Subj = tableName + (reader["ID"] ?? ++counter),
                            Pred = colName,
                            Obj = reader[colName].ToString()
                        };
                        triples.Add(triple);
                    }
                    triples.Add(new RawTriple { Subj = tableName + (reader["ID"] ?? ++counter), Pred = "Table", Obj = tableName });
                }
            }
            return triples;
        }
    }
}