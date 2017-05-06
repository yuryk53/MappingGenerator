using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;
using VDS.RDF.Ontology;
using VDS.RDF;
using System.Linq;

namespace MappingGenerator.Tests
{
    [TestClass]
    public class MGenUnitTests
    {
        
        const string dbName = "LMS";
        const string dbPrefix = "lms";
        const string dbURI = "http://www.example.org/LMS/";
        DBSemanticsGenerator generator = new DBSemanticsGenerator($@"Data Source=ASUS\SQLEXPRESS;Initial Catalog={dbName};Integrated Security=True");
        List<string> tables = new List<string>();
        Dictionary<string, string[]> tableAttributesDict = new Dictionary<string, string[]>();
        List<string> nmRelTables = new List<string>();

        public MGenUnitTests()
        {
            tables.Add("Course");
            tables.Add("Course_Material");
            tables.Add("Material");
            tables.Add("Role");
            tables.Add("Skill");
            tables.Add("User");
            tables.Add("User_Course");
            tables.Add("User_Skill");

            //Add attribute names of each table that we know beforehand
            tableAttributesDict.Add("Course", new string[] { "ID", "NAME" });
            tableAttributesDict.Add("Course_Material", new string[] { "ID_COURSE", "ID_MATERIAL" });
            tableAttributesDict.Add("Material", new string[] { "ID", "NAME", "LOCATION" });
            tableAttributesDict.Add("Role", new string[] { "ID", "NAME" });
            tableAttributesDict.Add("Skill", new string[] { "ID", "NAME", "DESCRIPTION" });
            tableAttributesDict.Add("User", new string[] { "ID", "NAME", "EMAIL", "PASSWORD", "ROLE_ID" });
            tableAttributesDict.Add("User_Course", new string[] { "ID_USER", "ID_COURSE" });
            tableAttributesDict.Add("User_Skill", new string[] { "ID_USER", "ID_SKILL" });

            nmRelTables.AddRange(new string[] { "Course_Material", "User_Course", "User_Skill" });
        }

        [TestMethod]
        public void Test_GenerateOntologyFromDB()
        {
            //test owl file creation, test all classes/attributes to be present
            //Arrange
            string fileName = $"{dbName}_test.owl";

            //Act
            IGraph g = generator.GenerateOntologyFromDB(dbName, $"xmlns: {dbPrefix} =\"{dbURI}\"", fileName);

            //Assert
            Assert.IsTrue(File.Exists(fileName)); //check file creation

            OntologyGraph ograph = new OntologyGraph();
            ograph.Merge(g);

            //check that each table is present in ontology as class
            IEnumerable<OntologyClass> oclasses = ograph.AllClasses;
            foreach(string tableName in tables.Except(nmRelTables))
            {
                string classUri = $"{dbURI}{tableName}";
                OntologyClass oclass = (from c in oclasses where (c.Resource as UriNode).Uri.ToString() == classUri select c).FirstOrDefault();
                Assert.IsNotNull(oclass);

                //check for properties
                OntologyClass classOfClasses = ograph.CreateOntologyClass(ograph.CreateUriNode("owl:Class"));
                var classProps = ograph.GetTriplesWithPredicateObject(ograph.CreateUriNode("rdfs:domain"), ograph.CreateUriNode(new Uri(classUri))).ToList();

                foreach(var propNameExpected in tableAttributesDict[tableName])
                {
                    Triple triple = classProps.FirstOrDefault(t => t.Subject.ToString().Split('#')[1] == propNameExpected);
                    Assert.IsNotNull(triple); //if null -> we didn't find the property in ontology that should be there
                }
            }

            //check that all n:m tables are mapped as object properties
            IEnumerable<OntologyProperty> objectProperties = ograph.OwlObjectProperties;
            foreach(var opropName in nmRelTables)
            {
                Assert.IsNotNull(objectProperties.FirstOrDefault(p => p.ToString().Split('#')[1] == opropName));
            }
        }

        [TestMethod]
        public void Test_GetTableAttributeDataType() //I
        {
            //Act
            string sqlType_course_ID = generator.GetTableAttributeDataType(dbName, "Course", "ID"); //should be int
            string sqlType_material_location = generator.GetTableAttributeDataType(dbName, "Material", "LOCATION"); //varchar
            string sqlType_role_name = generator.GetTableAttributeDataType(dbName, "Role", "NAME"); //nvarchar

            //Assert
            Assert.AreEqual("int", sqlType_course_ID);
            Assert.AreEqual("varchar", sqlType_material_location);
            Assert.AreEqual("nvarchar", sqlType_role_name);
        }

        [TestMethod]
        public void Test_ConvertNativeToXsdDataType()
        {
            //Assert
            Assert.AreEqual(SqlToXsdDtMapper.MapSqlToXSD("varchar"), generator.ConvertNativeToXsdDataType("varchar"));
            Assert.AreEqual(SqlToXsdDtMapper.MapSqlToXSD("int"), generator.ConvertNativeToXsdDataType("int"));
            Assert.AreEqual(SqlToXsdDtMapper.MapSqlToXSD("bit"), generator.ConvertNativeToXsdDataType("bit"));
        }

        [TestMethod]
        public void Test_HasForeignKeyConstraints()
        {
            bool hasFK = generator.HasForeignKeyConstraints(dbName, "User"); //has ROLE_ID foreign key to Role relation

            Assert.IsTrue(hasFK);
        }

        [TestMethod]
        public void Test_GetReferencedTableNames()
        {
            List<string> referencedTableNames = generator.GetReferencedTableNames(dbName, "User");

            Assert.AreEqual(1, referencedTableNames.Count);
            Assert.AreEqual("Role", referencedTableNames[0]);
        }

        [TestMethod]
        public void Test_GetReferencedTableName()
        {
            string tname = generator.GetReferencedTableName(dbName, "User", "ROLE_ID"); //references Role table
            Assert.AreEqual("Role", tname);
        }

        [TestMethod]
        public void Test_GetPrimaryKeys()
        {
            List<string> pks = generator.GetPrimaryKeys(dbName, "Course_Material");

            Assert.AreEqual(2, pks.Count);
            Assert.IsTrue(pks.Contains("ID_COURSE"));
            Assert.IsTrue(pks.Contains("ID_MATERIAL"));
        }

        [TestMethod]
        public void Test_GetReferencingTableColumns()
        {
            List<string> refColumns = generator.GetReferencingTableColumns(dbName, "User");

            Assert.AreEqual(1, refColumns.Count);
            Assert.AreEqual("ROLE_ID", refColumns[0]);
        }
    }
}
