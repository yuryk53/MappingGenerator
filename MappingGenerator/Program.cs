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
            //DBSemanticsGenerator generator = new DBSemanticsGenerator(@"Data Source=ASUS\SQLEXPRESS;Initial Catalog=LMS;Integrated Security=True");
            //generator.GenerateOntologyFromDB("LMS", "xmlns: lms =\"http://www.example.org/LMS/\"", "LMS.owl");
            OntologyGraph gLMS = new OntologyGraph();
            gLMS.LoadFromFile("LMS.owl");
            //gLMS.SaveToFile("LMS_dotNetRdf.owl");

            //generator = new DBSemanticsGenerator(@"Data Source=ASUS\SQLEXPRESS;Initial Catalog=KMS;Integrated Security=True");
            //generator.GenerateOntologyFromDB("KMS", "xmlns: kms =\"http://www.example.org/KMS/\"", "KMS.owl");
            OntologyGraph gKMS = new OntologyGraph();
            gKMS.LoadFromFile("KMS.owl");
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
                AskUserIfCanMerge,
                AskUserIfCanMerge,
                0.6,
                new ShortestFederatedNamesGenerator(),
                new XSDTypeCaster());
        }

        static bool AskUserIfCanMerge(SimilarClassPropertyDescription simDescr)
        {
            if(simDescr.MergeClassRelation != MergeClassRelation.NotApplicable) //it's a class
            {
                Console.WriteLine($"Can we merge classes:\n{simDescr.ObjectName1}\n{simDescr.ObjectName2}\n(Y\\N)>");
            }
            else
            {
                Console.WriteLine($"Can we merge properties:\n{simDescr.ObjectName1}\n{simDescr.ObjectName2}\n(Y\\N)>");
            }

            ConsoleKeyInfo key = Console.ReadKey();
            if (key.KeyChar == 'Y')
            {
                Console.WriteLine("\n");
                return true;
            }
            else {
                Console.WriteLine("\n");
                return false;
            }

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
