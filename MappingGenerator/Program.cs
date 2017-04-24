﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;

using WordsMatching;
using System.Configuration;
using System.IO;

namespace MappingGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            //DBSemanticsGenerator generator = new DBSemanticsGenerator(@"Data Source=ASUS\SQLEXPRESS;Initial Catalog=LMSv1;Integrated Security=True");
            //generator.GenerateOntologyFromDB("LMSv1", "xmlns: lms =\"http://www.example.org/LMS/\"", "LMSv1.owl");
            OntologyGraph gLMS = new OntologyGraph();
            gLMS.LoadFromFile("LMSv1.owl");
            //gLMS.SaveToFile("LMS_dotNetRdf.owl");

            //generator = new DBSemanticsGenerator(@"Data Source=ASUS\SQLEXPRESS;Initial Catalog=KMSv1;Integrated Security=True");
            //generator.GenerateOntologyFromDB("KMSv1", "xmlns: kms =\"http://www.example.org/KMS/\"", "KMSv1.owl");
            OntologyGraph gKMS = new OntologyGraph();
            gKMS.LoadFromFile("KMSv1.owl");
            //gLMS.SaveToFile("KMS_dotNetRdf.owl");

            TryMergeOntologies(gLMS, gKMS);

            //IOntologyMerger merger = new WordNetOntologyMerger();
            //merger.Initialize(gLMS, gKMS);
            //Dictionary<string, List<SimilarClassPropertyDescription>> simDict = merger.GetSimilarOntologyClassesMatrix();



            //PrintSimilaritiesIntoFile("similarities.txt", simDict);

            //foreach (var key in simDict.Keys)
            //{
            //    SimilarClassPropertyDescription map = (from mapping
            //                                           in simDict[key]
            //                                           where mapping.SimilarityScore == simDict[key].Max(x => x.SimilarityScore)
            //                                           select mapping).First();
            //    Console.WriteLine($"{map.ObjectName1}\n{map.ObjectName2}\nSimilarity score: {map.SimilarityScore}\n\n");
            //}

            //Console.WriteLine();
            

        }

        static void TryMergeOntologies(OntologyGraph o1, OntologyGraph o2)
        {
            IOntologyMerger merger = new WordNetOntologyMerger();
            merger.Initialize(o1, o2);

            Dictionary<string, List<SimilarClassPropertyDescription>> simDict = merger.GetSimilarOntologyClassesMatrix();
            List<SimilarClassPropertyDescription> similarClasses = new List<SimilarClassPropertyDescription>();
            foreach (var key in simDict.Keys)
            {
                SimilarClassPropertyDescription map = (from mapping
                                                       in simDict[key]
                                                       where mapping.SimilarityScore == simDict[key].Max(x => x.SimilarityScore)
                                                       select mapping).First();
                similarClasses.Add(map);
            }


            IInteractiveMerger interactiveMerger = merger as IInteractiveMerger;
            interactiveMerger.MergeOntologyClasses(similarClasses,
                simPair => true,
                propPair => true,
                0.6,
                new ShortestFederatedNamesGenerator(),
                new XSDTypeCaster());
        }

        static void PrintSimilaritiesIntoFile(string fname, Dictionary<string, List<SimilarClassPropertyDescription>> simDict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in simDict.Keys)
            {
                foreach (var map in simDict[key])
                {
                    sb.AppendLine($"{map.ObjectName1}\n{map.ObjectName2}\nSimilarity score: {map.SimilarityScore}\n\n");
                }
                sb.AppendLine("\n\n");
            }

            File.WriteAllText(fname, sb.ToString());
        }
    }
}
