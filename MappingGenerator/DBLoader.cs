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

        public List<string> GetTableNames()
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