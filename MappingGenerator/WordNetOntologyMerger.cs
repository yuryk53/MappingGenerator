using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;

using WordsMatching;

namespace MappingGenerator
{
    public class WordNetOntologyMerger : IOntologyMerger, IInteractiveMerger
    {
        private OntologyGraph _merged = null;
        private OntologyGraph _o1, _o2;
        private SentenceSimilarity semsim;

        private object semaphore = new object();

        public WordNetOntologyMerger()
        {
            Wnlib.WNCommon.path = ConfigurationManager.AppSettings["wordNetPath"];
            semsim = new SentenceSimilarity();
        }

        public Dictionary<string, List<SimilarClassPropertyDescription>> GetSimilarClassPropertiesMatrix(string classUri1, string classUri2)
        {

            OntologyClass classOfClasses1 = _o1.CreateOntologyClass(_o1.CreateUriNode("owl:Class"));
            OntologyClass classOfClasses2 = _o2.CreateOntologyClass(_o2.CreateUriNode("owl:Class"));

            var classProps_i = _o1.GetTriplesWithPredicateObject(_o1.CreateUriNode("rdfs:domain"), _o1.CreateUriNode(new Uri(classUri1))).ToList();
            var classProps_j = _o2.GetTriplesWithPredicateObject(_o2.CreateUriNode("rdfs:domain"), _o1.CreateUriNode(new Uri(classUri2))).ToList();

            //remove not owl: -Properties
            classProps_i.RemoveAll(t => !t.Subject.ToString().Contains("#"));
            classProps_j.RemoveAll(t => !t.Subject.ToString().Contains("#"));

            double[][] propsSimilarityMatrix = new double[classProps_i.Count][];
            for (int m = 0; m < classProps_i.Count; m++)
            {
                propsSimilarityMatrix[m] = new double[classProps_j.Count];
                string propName1 = (classProps_i[m] as Triple).Subject.ToString().Split('#')[1];
                for (int n = 0; n < classProps_j.Count; n++)
                {
                    string propName2 = (classProps_j[n] as Triple).Subject.ToString().Split('#')[1];
                    propsSimilarityMatrix[m][n] = GetWordNetSimilarityScore(propName1, propName2);
                }
            }

            Dictionary<string, List<SimilarClassPropertyDescription>> simDict = new Dictionary<string, List<SimilarClassPropertyDescription>>();
            for (int i = 0; i < propsSimilarityMatrix.Length; i++)
            {
                string key = classProps_i[i].Subject.ToString();
                if (!simDict.ContainsKey(key))
                {
                    simDict.Add(key, new List<SimilarClassPropertyDescription>());
                }

                for (int j = 0; j < propsSimilarityMatrix[i].Length; j++)
                {
                    simDict[key].Add(new SimilarClassPropertyDescription
                    {
                        ObjectName1 = classProps_i[i].Subject.ToString(),
                        ObjectName2 = classProps_j[j].Subject.ToString(),
                        SimObjectURI1 = classProps_i[i].GraphUri.ToString(),
                        SimObjectURI2 = classProps_j[j].GraphUri.ToString(),
                        SimilarityScore = propsSimilarityMatrix[i][j],
                        MergePropRelation = MergePropertyRelation.EquivalentProperty
                    });
                }
            }

            return simDict;
        }

        public Dictionary<string, List<SimilarClassPropertyDescription>> GetSimilarOntologyClassesMatrix(bool includeProperties = true)
        {
            OntologyClass classOfClasses1 = _o1.CreateOntologyClass(_o1.CreateUriNode("owl:Class"));
            OntologyClass classOfClasses2 = _o2.CreateOntologyClass(_o2.CreateUriNode("owl:Class"));

            var classesO1 = classOfClasses1.Instances.ToList();
            var classesO2 = classOfClasses2.Instances.ToList();

            //create similarity matrix
            double[,] similarityMatrix = new double[classesO1.Count, classesO2.Count];

            for (int i = 0; i < similarityMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < similarityMatrix.GetLength(1); j++)
                {
                    double score = GetWordNetSimilarityScore(classesO1[i].ToString(), classesO2[j].ToString());

                    if (includeProperties)
                    {
                        var classProps_i = _o1.GetTriplesWithPredicateObject(_o1.CreateUriNode("rdfs:domain"), _o1.CreateUriNode(new Uri($"{classesO1[i].ToString()}"))).ToList();
                        var classProps_j = _o2.GetTriplesWithPredicateObject(_o2.CreateUriNode("rdfs:domain"), _o1.CreateUriNode(new Uri($"{classesO2[j].ToString()}"))).ToList();

                        //remove not owl: -Properties
                        classProps_i.RemoveAll(t => !t.Subject.ToString().Contains("#"));
                        classProps_j.RemoveAll(t => !t.Subject.ToString().Contains("#"));

                        double[][] propsSimilarityMatrix = new double[classProps_i.Count][];
                        for (int m = 0; m < classProps_i.Count; m++)
                        {
                            propsSimilarityMatrix[m] = new double[classProps_j.Count];
                            string propName1 = (classProps_i[m] as Triple).Subject.ToString().Split('#')[1];
                            for (int n = 0; n < classProps_j.Count; n++)
                            {
                                string propName2 = (classProps_j[n] as Triple).Subject.ToString().Split('#')[1];
                                propsSimilarityMatrix[m][n] = GetWordNetSimilarityScore(propName1, propName2);
                            }
                        }

                        List<double> propsScores = new List<double>();
                        for (int m = 0; m < classProps_i.Count; m++)
                        {
                            propsScores.Add(propsSimilarityMatrix[m].Max());
                        }
                        similarityMatrix[i, j] = (score + propsScores.Sum()) / (propsScores.Count + 1);
                    }
                    else
                    {
                        similarityMatrix[i, j] = score;
                    }
                }
            }

            Dictionary<string, List<SimilarClassPropertyDescription>> simDict = new Dictionary<string, List<SimilarClassPropertyDescription>>();
            for (int i = 0; i < similarityMatrix.GetLength(0); i++)
            {
                //double maxScore = similarityMatrix[i, 0];
                //int maxScoreIndex = 0;
                //for (int j = 1; j < similarityMatrix.GetLength(1); j++)
                //{
                //    if(i== j)
                //    {
                //        continue; //we do not compare classes to themselves
                //    }
                //    if(maxScore < similarityMatrix[i,j])
                //    {
                //        maxScore = similarityMatrix[i, j];
                //        maxScoreIndex = j;
                //    }
                //}
                string key = classesO1[i].ToString();
                if (!simDict.ContainsKey(key))
                {
                    simDict.Add(key, new List<SimilarClassPropertyDescription>());
                }

                for (int j = 0; j < similarityMatrix.GetLength(1); j++)
                {
                    simDict[key].Add(new SimilarClassPropertyDescription
                    {
                        ObjectName1 = classesO1[i].ToString(),
                        ObjectName2 = classesO2[j].ToString(),
                        SimObjectURI1 = classesO1[i].Resource.GraphUri.ToString(),
                        SimObjectURI2 = classesO2[j].Resource.GraphUri.ToString(),
                        SimilarityScore = similarityMatrix[i, j],
                        MergeClassRelation = MergeClassRelation.SubClassOf
                    });
                }
            }

            return simDict;
        }

        public void Initialize(OntologyGraph o1, OntologyGraph o2)
        {
            if(_merged == null)
            {
                _merged = new OntologyGraph();
                _merged.Merge(o1);
                _merged.Merge(o2);

                _o1 = o1;
                _o2 = o2;
            }
            else
            {
                throw new InvalidOperationException("The graph has been already initialized!");
            }
        }

        public IGraph MergeTwoOntologies(IGraph o1, IGraph o2, double classThreshold, double propertyThreshold)
        {
            Dictionary<string, List<SimilarClassPropertyDescription>> simDict = GetSimilarOntologyClassesMatrix();
            List<SimilarClassPropertyDescription> similarClasses = new List<SimilarClassPropertyDescription>();
            foreach (var key in simDict.Keys)
            {
                SimilarClassPropertyDescription map = (from mapping
                                                       in simDict[key]
                                                       where mapping.SimilarityScore == simDict[key].Max(x => x.SimilarityScore)
                                                       select mapping).First();
                similarClasses.Add(map);
            }

            (this as IInteractiveMerger).MergeOntologyClasses(
                similarClasses,
                simPair => simPair.SimilarityScore >= classThreshold,        //no user interaction here
                propPair => propPair.SimilarityScore >= propertyThreshold,   //no user interaction here
                0.6,
                new ShortestFederatedNamesGenerator(),
                new XSDTypeCaster());

            return _merged;
        }

        private double GetWordNetSimilarityScore(string word1, string word2)
        {
            float score = 0;

            lock(semaphore)
            {
                score = semsim.GetScore(word1, word2);
            }
            return score;
        }

        void IInteractiveMerger.MergeOntologyClasses(List<SimilarClassPropertyDescription> mergedClassPairs, 
                                                     Func<SimilarClassPropertyDescription, bool> canWeMergeClassPairCallback,
                                                     Func<SimilarClassPropertyDescription, bool> canWeMergePropertyPairCallback, 
                                                     double mergePropertiesThreshold,
                                                     IFederatedNamesGenerator federatedNamesGen,
                                                     ITypeCaster typeCaster,
                                                     string federatedStem=null)
        {
            if(federatedStem == null)
            {
                federatedStem = ConfigurationManager.AppSettings["defaultFederatedStem"];
            }

            foreach(SimilarClassPropertyDescription mergedClassPair in mergedClassPairs)
            {
                mergedClassPair.MergeClassRelation = MergeClassRelation.SubClassOf; //by default, howerver, using callback user can change this
                if (mergedClassPair.SimilarityScore >= mergePropertiesThreshold)
                {
                    if(mergedClassPair.MergeClassRelation == MergeClassRelation.SubClassOf)
                    {
                        //merge using rdfs:subClassOf (traverse all merged classes and add this attribute to them)


                        //now compose federated URI for a federated class
                        //use the shortest length class name from merged classes
                        int indexOfLastSlash01 = mergedClassPair.ObjectName1.LastIndexOf('/');
                        string o1ClassName = mergedClassPair.ObjectName1.Substring(indexOfLastSlash01 + 1,
                                                                                mergedClassPair.ObjectName1.Length - 1 - indexOfLastSlash01);
                        int indexOfLastSlash02 = mergedClassPair.ObjectName2.LastIndexOf('/');
                        string o2ClassName = mergedClassPair.ObjectName2.Substring(indexOfLastSlash02 + 1,
                                                                                mergedClassPair.ObjectName2.Length - 1 - indexOfLastSlash02);

                        string federatedClassName = federatedNamesGen.GenerateFederatedName(o1ClassName, o2ClassName);

                        string federatedUri = federatedStem + federatedClassName;
                        mergedClassPair.FederatedURI = federatedUri;
                        
                        
                        if(canWeMergeClassPairCallback != null)
                        {
                            if(!canWeMergeClassPairCallback(mergedClassPair))
                            {
                                continue; //we are not allowed to merge these classes, go on, nothing to see here
                            }
                        }

                        //if callback function was not provided or we are allowed to merge classes -> merge them
                        OntologyClass federatedClass = _merged.CreateOntologyClass(new Uri(federatedUri)); //check federatedClassName to a URI!!!

                        //everything's OK -> add current federated property to ontology graph
                        OntologyResource ontologyClassResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlClass));
                        federatedClass.AddType(ontologyClassResource);


                        //now obtain all merged classes' properties for merging, give them federated URIs and merge them
                        //before merging, ask a user using callback if he wants to merge the particular props
                        string class1 = mergedClassPair.ObjectName1;
                        string class2 = mergedClassPair.ObjectName2;
                        Dictionary<string, List<SimilarClassPropertyDescription>> simDict = GetSimilarClassPropertiesMatrix(
                            classUri1: class1,
                            classUri2: class2); //obtain properties similarity matrix

                        List<SimilarClassPropertyDescription> highestScoreMergePairs = new List<SimilarClassPropertyDescription>();
                        foreach(var key in simDict.Keys)
                        {
                            SimilarClassPropertyDescription map = (from mapping
                                                                   in simDict[key]
                                                                   where mapping.SimilarityScore == simDict[key].Max(x => x.SimilarityScore)
                                                                   select mapping).First();
                            highestScoreMergePairs.Add(map);
                        }

                        //now we have property pairs which are most likely to be equivalent, try to merge them asking user before each merge
                        foreach (var mergePropertyPair in highestScoreMergePairs)
                        {
                            if (mergedClassPair.SimilarityScore >= mergePropertiesThreshold)
                            {
                                mergePropertyPair.MergePropRelation = MergePropertyRelation.EquivalentProperty; //by default


                                //generate federated property URI to suggest to user
                                int indexOfLastSharp01 = mergePropertyPair.ObjectName1.LastIndexOf('#');
                                string o1PropName = mergePropertyPair.ObjectName1.Substring(indexOfLastSharp01 + 1,
                                                                                      mergePropertyPair.ObjectName1.Length - 1 - indexOfLastSharp01);
                                int indexOfLastSharp02 = mergePropertyPair.ObjectName2.LastIndexOf('#');
                                string o2PropName = mergePropertyPair.ObjectName2.Substring(indexOfLastSharp02 + 1,
                                                                                      mergePropertyPair.ObjectName2.Length - 1 - indexOfLastSharp02);


                                string federatedPropertyName = federatedNamesGen.GenerateFederatedName(
                                    o1PropName, o2PropName);

                                string federatedPropURI = $"{mergedClassPair.FederatedURI}#{federatedPropertyName}";
                                mergePropertyPair.FederatedURI = federatedPropURI;
                                //federated property URI generation END

                                if (canWeMergePropertyPairCallback(mergePropertyPair))   //ask user if we're allowed to merge
                                {
                                    //we're allowed to merge -> add to _merged ontology graph (to federated class) federated property
                                    OntologyProperty federatedProperty = _merged.CreateOntologyProperty(new Uri(mergePropertyPair.FederatedURI));
                                    if (mergePropertyPair.MergePropRelation == MergePropertyRelation.EquivalentProperty)
                                    {
                                        federatedProperty.AddEquivalentProperty(new Uri(mergePropertyPair.ObjectName1));
                                        federatedProperty.AddEquivalentProperty(new Uri(mergePropertyPair.ObjectName2));
                                    }
                                    if (mergePropertyPair.MergePropRelation == MergePropertyRelation.SubProperty)
                                    {
                                        federatedProperty.AddSubProperty(new Uri(mergePropertyPair.ObjectName1));   //!!!!!! спорная ситуация
                                        federatedProperty.AddSubProperty(new Uri(mergePropertyPair.ObjectName2));   //!!!!!! спорная ситуация
                                    }
                                    
                                    //to specify domain and range we should MERGE THE DATATYPES!!!

                                    var prop01Range = _o1.GetTriplesWithSubjectPredicate(_o1.CreateUriNode(new Uri(mergePropertyPair.ObjectName1)), _o1.CreateUriNode("rdfs:range")).ToList();
                                    var prop02Range = _o2.GetTriplesWithSubjectPredicate(_o2.CreateUriNode(new Uri(mergePropertyPair.ObjectName2)), _o2.CreateUriNode("rdfs:range")).ToList();

                                    if (prop01Range.Count > 1 || prop02Range.Count > 1)
                                        throw new InvalidOperationException($"Property can have only one rdfs:range defined! [Properties {mergePropertyPair.ObjectName1} & {mergePropertyPair.ObjectName2} ]");
                                    if (prop01Range.Count != prop02Range.Count)
                                        throw new InvalidOperationException($"Properties should both have 1 (or zero) range(s) defined [Properties {mergePropertyPair.ObjectName1} & {mergePropertyPair.ObjectName2} ]");
                                    //howerver, props are allowed to not have range defined

                                    string prop01RangeStr = (prop01Range.First() as Triple).Object.ToString();
                                    string prop02RangeStr = (prop02Range.First() as Triple).Object.ToString();

                                    string castedRange = string.Empty;
                                    //check, if prop01RangeStr OR prop02RangeStr not to be a resource (otherwise, it's a range of an object property,
                                    //and, if ranges of different ObjectProperties don't coincide, we can't merge them, so, delete current federatedProperty
                                    Regex r = new Regex(@"http\w{0,1}://.+");
                                    if(r.IsMatch(prop01RangeStr) || r.IsMatch(prop02RangeStr))
                                    {
                                        //check that prop01RangeStr==prop02RangeStr
                                        if(prop01RangeStr==prop02RangeStr)
                                        {
                                            //OK->federated Range is equal to prop01RangeStr\prop02RangeStr
                                            castedRange = prop01RangeStr;
                                            federatedProperty.AddRange(new Uri($"xsd:{castedRange}"));
                                            federatedProperty.AddDomain(new Uri(mergedClassPair.FederatedURI));
                                            OntologyResource ontologyPropertyResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlObjectProperty));
                                            federatedProperty.AddType(ontologyPropertyResource);
                                        }
                                        else if(mergedClassPairs.FirstOrDefault(simClassProp=> {
                                            return (simClassProp.ObjectName1 == prop01RangeStr &&
                                                   simClassProp.ObjectName2 == prop02RangeStr) ||
                                                   (simClassProp.ObjectName1 == prop02RangeStr &&
                                                   simClassProp.ObjectName2 == prop01RangeStr);
                                            })!=null) //we've found merge pair which states that classes, correspoinding to prop01RangeStr & prop02RangeStr are being merged
                                        {
                                            //if ranges' URIs correspond to classes which are being merged, than we CREATE an ObjectProperty with several ranges
                                            federatedProperty.AddRange(new Uri(prop01RangeStr));
                                            federatedProperty.AddRange(new Uri(prop02RangeStr));
                                            federatedProperty.AddDomain(new Uri(mergedClassPair.FederatedURI));
                                            OntologyResource ontologyPropertyResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlObjectProperty));
                                            federatedProperty.AddType(ontologyPropertyResource);
                                        }
                                        else
                                        {
                                            //ranges of different ObjectProperties don't coincide, we can't merge them, so, don't add current federatedProperty to graph
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        castedRange = typeCaster.CastTypes(prop01RangeStr, prop02RangeStr);
                                        federatedProperty.AddRange(new Uri($"xsd:{castedRange}"));
                                        federatedProperty.AddDomain(new Uri(mergedClassPair.FederatedURI));

                                        //everything's OK -> add current federated property to ontology graph
                                        OntologyResource ontologyPropertyResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlDatatypeProperty));
                                        federatedProperty.AddType(ontologyPropertyResource);
                                    }
                                }
                                else
                                {
                                    continue; //we're not allowed to merge this pair
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        throw new NotImplementedException("Merging for other types of relations is not implemented!");
                    }
                }
                else continue; //didn't pass through threshold
            }
            _merged.SaveToFile("mergedOntology.log.owl");
        }
    }
}
