using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;

namespace MappingGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            DBSemanticsGenerator generator = new DBSemanticsGenerator(@"Data Source=ASUS\SQLEXPRESS;Initial Catalog=LMSv1;Integrated Security=True");
            generator.GenerateOntologyFromDB("LMSv1", "xmlns: lms =\"http://www.example.org/LMS/\"", "LMSv1.owl");
            IGraph g = new Graph();
            g.LoadFromFile("LMSv1.owl");
            g.SaveToFile("LMS_dotNetRdf.owl");
            Console.WriteLine();
            
        }
    }
}
