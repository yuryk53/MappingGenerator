using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;

using VDS.RDF;
using System.Data.SqlClient;
using VDS.RDF.Ontology;
using VDS.RDF.Writing;
using System.IO;
using System.Text.RegularExpressions;

namespace MappingGenerator
{
    class DBSemanticsGenerator
    {
        string connString = @"Data Source=ASUS\SQLEXPRESS;Initial Catalog=LMS;Integrated Security=True";

        public DBSemanticsGenerator(string connectionString)
        {
            this.connString = connectionString;
        }

        //for each of the tables make owl:Class with fields as owl:ObjectProperty or owl:DataProperty
        public IGraph GenerateOntologyFromDB(string dbName, string dbNamespace, string ontologyName)
        {
            DBLoader dbLoader = new DBLoader(connString);
            List<string> tableNames = dbLoader.GetTableNames();
            List<string> nmTableNames = new List<string>();
            //referenced tables = ObjectProperty (FK fields)
            //other fields = DataProperty

            //for each table create owl:Class
            OntologyGraph g = new OntologyGraph();
            string ns = dbNamespace;

            //RdfXmlWriter writer = new RdfXmlWriter();
            //writer.PrettyPrintMode = true;
            //writer.Save(g, PathWrapper.Combine(Environment.CurrentDirectory, "LMS.owl"));
            ////header was printed

            OWLWriter owlWriter = new OWLWriter();
            owlWriter.AddNamespace(ns);
            StringBuilder sb = new StringBuilder();
            owlWriter.WriteHeader(sb);
            string globalUri = MatchUri(ns);
            foreach (string tableName in tableNames)
            {
                //check, if it's not a n:m mapping table
                HashSet<string> PKs = new HashSet<string>(GetPrimaryKeys(dbName, tableName));
                if (PKs.Count >= 2)
                {
                    HashSet<string> FKs = new HashSet<string>(GetReferencingTableColumns(dbName, tableName));
                    //if all PK columns are FKs at the same time -> probably, we have n:m relation
                    if(FKs.IsSupersetOf(PKs))  //is ProperSubset maybe ???
                    {
                        nmTableNames.Add(tableName);
                        continue; //go to next table
                    }
                }

                string tableUri = globalUri.Remove(globalUri.LastIndexOf('"')) + tableName;
                //OntologyClass newClass = g.CreateOntologyClass(new Uri(tableUri));
                owlWriter.EmitSimpleOWLClass(sb, tableUri);

                List<string> tableColumns = dbLoader.GetColumnNames(tableName);
                //now distinguish between DataProperty and ObjectProperty columns
                List<string> dataColumns = new List<string>(),
                            objectColumns = GetReferencingTableColumns(dbName, tableName);  //referenced via Foreign Key

                dataColumns = (from column in tableColumns select column).Except(objectColumns).ToList();


                //seek to the end of file (till the </rdf:RDF>


                foreach (string dataColumn in dataColumns)
                {
                    //newClass.AddLiteralProperty($"{tableUri}/{dataColumn}", g.CreateLiteralNode(dataColumn), true);
                    owlWriter.EmitSimpleDataTypeProp(sb, $"{tableUri}#{dataColumn}");
                }

                foreach(string objectColumn in objectColumns)
                {
                    string referencedTable = GetReferencedTableName(dbName, tableName, objectColumn);
                    string refTableUri = globalUri.Remove(globalUri.LastIndexOf('"')) + referencedTable;
                    owlWriter.EmitSimpleObjectProp(sb, $"{tableUri}#{objectColumn}", tableUri, refTableUri);
                }

                //foreach(string objectColumn in objectColumns)
                //{
                //    IUriNode uriNode = g.Create
                //}
            }

            //process n:m tables
            foreach(var tableName in nmTableNames)
            {
                string tableUri = ComposeTableUri(globalUri, tableName);
                List<string> PKs = GetPrimaryKeys(dbName, tableName);
                string tableN = GetReferencedTableName(dbName, tableName, PKs[0]);
                string tableM = GetReferencedTableName(dbName, tableName, PKs[1]);
                //table N = domain (or vice versa)
                //table M = range (or vice versa)
                owlWriter.EmitSimpleObjectProp(sb, tableUri, ComposeTableUri(globalUri, tableN), ComposeTableUri(globalUri, tableM));
            }


            owlWriter.WriteFooter(sb);
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, ontologyName), sb.ToString());

            return g;
        }

        private string ComposeTableUri(string globalUri, string tableName)
        {
            return globalUri.Remove(globalUri.LastIndexOf('"')) + tableName;
        }

        private string MatchUri(string uri)
        {
            uri = uri.Replace(" ", "");
            Regex r = new Regex(@"http\w{0,1}://.+");
            if (r.IsMatch(uri))
            {
                MatchCollection matches = r.Matches(uri);
                return matches[0].Groups[0].Value;
            }
            else throw new ArgumentException($"String ' {uri} ' does not contain a valid uri!");
        }

        public bool HasForeignKeyConstraints(string dbName, string tableName)
        {
            SqlConnection sqlConnection = new SqlConnection(connString);
            //build a "serverConnection" with the information of the "sqlConnection"
            ServerConnection serverConnection = new ServerConnection(sqlConnection);

            //The "serverConnection is used in the ctor of the Server.
            Server server = new Server(serverConnection);
            Database db = server.Databases[dbName];

            Table tbl = db.Tables[tableName];
            return tbl.ForeignKeys.Count > 0;
        }

        public List<string> GetReferencedTableNames(string dbName, string tableName)
        {
            SqlConnection sqlConnection = new SqlConnection(connString);
            //build a "serverConnection" with the information of the "sqlConnection"
            ServerConnection serverConnection = new ServerConnection(sqlConnection);

            //The "serverConnection is used in the ctor of the Server.
            Server server = new Server(serverConnection);
            Database db = server.Databases[dbName];

            Table tbl = db.Tables[tableName];

            List<string> referencedTables = new List<string>();

            foreach (ForeignKey fk in tbl.ForeignKeys)
            {
                referencedTables.Add(fk.ReferencedTable);
            }
            return referencedTables;
        }

        /// <summary>
        /// Gets table name which is referenced by foreign key FK
        /// </summary>
        /// <param name="dbName">Database name</param>
        /// <param name="tableName">Table, which references another table using FK</param>
        /// <param name="FK">Foreign key</param>
        /// <returns></returns>
        public string GetReferencedTableName(string dbName, string tableName, string FK)
        {
            SqlConnection sqlConnection = new SqlConnection(connString);
            //build a "serverConnection" with the information of the "sqlConnection"
            ServerConnection serverConnection = new ServerConnection(sqlConnection);

            //The "serverConnection is used in the ctor of the Server.
            Server server = new Server(serverConnection);
            Database db = server.Databases[dbName];

            Table tbl = db.Tables[tableName];


            foreach (ForeignKey fk in tbl.ForeignKeys)
            {
                if (fk.Columns.Contains(FK))
                    return fk.ReferencedTable;
            }

            throw new ArgumentException($"No such foreign key '{FK}' in '{tableName}'");
        }

        public List<string> GetPrimaryKeys(string dbName, string tableName)
        {
            SqlConnection sqlConnection = new SqlConnection(connString);
            //build a "serverConnection" with the information of the "sqlConnection"
            ServerConnection serverConnection = new ServerConnection(sqlConnection);

            //The "serverConnection is used in the ctor of the Server.
            Server server = new Server(serverConnection);
            Database db = server.Databases[dbName];

            Table tbl = db.Tables[tableName];
            if(tbl == null) // possibly a view
            {
                return new List<string>(); //return empty list
            }

            List<string> pkList = new List<string>();
            foreach (Column col in tbl.Columns)
            {
                if(col.InPrimaryKey)
                    pkList.Add(col.Name);
            }

            return pkList;
        }

        /// <summary>
        /// Get table columns which reference another tables (Get foreigh keys)
        /// </summary>
        /// <returns>FK columns</returns>
        public List<string> GetReferencingTableColumns(string dbName, string tableName)
        {
            SqlConnection sqlConnection = new SqlConnection(connString);
            //build a "serverConnection" with the information of the "sqlConnection"
            ServerConnection serverConnection = new ServerConnection(sqlConnection);

            //The "serverConnection is used in the ctor of the Server.
            Server server = new Server(serverConnection);
            Database db = server.Databases[dbName];

            Table tbl = db.Tables[tableName];
            if (tbl == null) // possibly a view
            {
                return new List<string>(); //return empty list
            }


            List<string> referencingColumns = new List<string>();

            foreach (ForeignKey fk in tbl.ForeignKeys)
            {
                string columnName = fk.Columns[0].ToString().Trim(new char[] { '[', ']' });
                referencingColumns.Add(columnName);
            }
            return referencingColumns;
        }
    }
}
