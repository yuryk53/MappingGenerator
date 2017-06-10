﻿/* 
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
        protected OntologyGraph _merged = null;
        protected OntologyGraph _o1, _o2;
        protected SentenceSimilarity semsim;

        protected object semaphore = new object();

        public WordNetOntologyMerger()
        {
            Wnlib.WNCommon.path = ConfigurationManager.AppSettings["wordNetPath"];
            semsim = new SentenceSimilarity();
        }

        public Dictionary<string, List<SimilarClassPropertyDescription>> GetSimilarClassPropertiesMatrix(string classUri1, string classUri2, IProgress<double> progress=null)
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

                    if (progress != null)
                    {
                        double progressValue = (m * classProps_j.Count + (n + 1)) / (double)(classProps_i.Count * classProps_j.Count);    //from 0 to 1
                        progress.Report(progressValue);
                    }
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

        public Dictionary<string, List<SimilarClassPropertyDescription>> GetSimilarOntologyClassesMatrix(bool includeProperties = true, IProgress<double> progress=null)
        {
            OntologyClass classOfClasses1 = _o1.CreateOntologyClass(_o1.CreateUriNode("owl:Class"));
            OntologyClass classOfClasses2 = _o2.CreateOntologyClass(_o2.CreateUriNode("owl:Class"));

            var classesO1 = classOfClasses1.Instances.ToList();
            var classesO2 = classOfClasses2.Instances.ToList();

            //create similarity matrix
            double[,] similarityMatrix = new double[classesO1.Count, classesO2.Count];

            double stepProgressDelta = 1.0 / similarityMatrix.Length; //progress Delta for one step of 'j' variable
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

                                if (progress != null)
                                {
                                    double classLoopProgressValue = (i * similarityMatrix.GetLength(0) + (j + 1)) / (double)(similarityMatrix.Length);    //from 0 to 1
                                    double propertyLoopProgressValue = ((m * classProps_j.Count + (n + 1)) / (double)(classProps_i.Count*classProps_j.Count))*stepProgressDelta;    //from 0 to stepProgressDelta -> normalized
                                    double progressValue = classLoopProgressValue - stepProgressDelta + propertyLoopProgressValue;
                                    progress.Report(progressValue);
                                }
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

                        if(progress!= null)
                        {
                            double progressValue = (i * similarityMatrix.GetLength(0) + (j + 1)) / (double)(similarityMatrix.Length);    //from 0 to 1
                            progress.Report(progressValue);
                        }
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

        public IGraph MergeTwoOntologies(IGraph o1, IGraph o2, double classThreshold, double propertyThreshold, IProgress<double> progress=null)
        {
            _merged = new OntologyGraph();
            _merged.Merge(o1);
            _merged.Merge(o2);

            Dictionary<string, List<SimilarClassPropertyDescription>> simDict = GetSimilarOntologyClassesMatrix(includeProperties:true, progress: progress);
            List<SimilarClassPropertyDescription> similarClasses = new List<SimilarClassPropertyDescription>();
            foreach (var key in simDict.Keys)
            {
                SimilarClassPropertyDescription map = (from mapping
                                                       in simDict[key]
                                                       where mapping.SimilarityScore == simDict[key].Max(x => x.SimilarityScore)
                                                       select mapping).First(); //select pairs with highest similarity score
                similarClasses.Add(map);
            }

            (this as IInteractiveMerger).MergeOntologyClasses(
                similarClasses,
                simPair => simPair.SimilarityScore >= classThreshold,        //no user interaction here
                propPair => propPair.SimilarityScore >= propertyThreshold,   //no user interaction here
                propertyThreshold,
                new ShortestFederatedNamesGenerator(),
                new XSDTypeCaster(),
                progress);

            return _merged;
        }

        protected double GetWordNetSimilarityScore(string word1, string word2)
        {
            float score = 0;

            lock(semaphore)
            {
                score = semsim.GetScore(word1, word2);
            }
            return score;
        }

        IGraph IInteractiveMerger.MergeOntologyClasses(List<SimilarClassPropertyDescription> mergedClassPairs, 
                                                     Func<SimilarClassPropertyDescription, bool> canWeMergeClassPairCallback,
                                                     Func<SimilarClassPropertyDescription, bool> canWeMergePropertyPairCallback, 
                                                     double mergePropertiesThreshold,
                                                     IFederatedNamesGenerator federatedNamesGen,
                                                     ITypeCaster typeCaster,
                                                     IProgress<double> progress=null,
                                                     Func<string, string, IProgress<double>, Dictionary<string, List<SimilarClassPropertyDescription>>> getSimilarClassPropertiesMatrixMethod = null,
                                                     string federatedStem=null)
        {
            if(federatedStem == null)
            {
                federatedStem = ConfigurationManager.AppSettings["defaultFederatedStem"];
            }

            if (getSimilarClassPropertiesMatrixMethod == null) //user didn't provide his own method -> use default one
            {
                getSimilarClassPropertiesMatrixMethod = new Func<string, string, IProgress<double>, Dictionary<string, List<SimilarClassPropertyDescription>>>
                                                                (GetSimilarClassPropertiesMatrix);
            }

            int mergedClassPairNumber = 0;
            double classPairProgressStepDelta = 1.0 / mergedClassPairs.Count;
            foreach(SimilarClassPropertyDescription mergedClassPair in mergedClassPairs)
            {
                mergedClassPairNumber++;
                mergedClassPair.MergeClassRelation = MergeClassRelation.SubClassOf; //by default, howerver, using callback user can change this
                if (mergedClassPair.SimilarityScore >= mergePropertiesThreshold)
                {
                    if(mergedClassPair.MergeClassRelation == MergeClassRelation.SubClassOf)
                    {
                        //merge using rdfs:subClassOf (traverse all merged classes and add this attribute to them)



                        //now compose federated URI for a federated class
                        //use the shortest length class name from merged classes
                        string o1ClassName = GetClassNameFromUri(mergedClassPair.ObjectName1);
                        string o2ClassName = GetClassNameFromUri(mergedClassPair.ObjectName2);

                        string federatedClassName = federatedNamesGen.GenerateFederatedName(o1ClassName, o2ClassName);

                        string federatedUri = federatedStem + federatedClassName;
                        mergedClassPair.FederatedURI = federatedUri;

                        string fed = GetClassNameFromUri(mergedClassPair.ObjectName1);


                        if (canWeMergeClassPairCallback != null)
                        {
                            if(!canWeMergeClassPairCallback(mergedClassPair))
                            {
                                continue; //we are not allowed to merge these classes, go on, nothing to see here
                            }
                        }

                        //if callback function was not provided or we are allowed to merge classes -> merge them
                        OntologyClass federatedClass = _merged.CreateOntologyClass(new Uri(federatedUri)); //check federatedClassName to a URI!!!


                        OntologyClass oclass1 = _merged.AllClasses.Where(oclass => oclass.Resource.ToString() == mergedClassPair.ObjectName1).First();
                        federatedClass.AddSubClass(oclass1);
                        OntologyClass oclass2 = _merged.AllClasses.Where(oclass => oclass.Resource.ToString() == mergedClassPair.ObjectName2).First();
                        federatedClass.AddSubClass(oclass2);

                        //everything's OK -> add current federated property to ontology graph
                        OntologyResource ontologyClassResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlClass));
                        
                        federatedClass.AddType(ontologyClassResource);
                       
                        


                        //now obtain all merged classes' properties for merging, give them federated URIs and merge them
                        //before merging, ask a user using callback if he wants to merge the particular props
                        string class1 = mergedClassPair.ObjectName1;
                        string class2 = mergedClassPair.ObjectName2;
                        Progress<double> getClassPropsMatrixProgress = null;

                        if (progress != null)   //report progress
                        {
                            getClassPropsMatrixProgress = new Progress<double>(pValue =>
                            {
                                //take into account main loop progress
                                double progressValue = (mergedClassPairNumber / (double)mergedClassPairs.Count) - classPairProgressStepDelta + pValue * classPairProgressStepDelta;
                                progress.Report(progressValue);
                            });
                        }

                        #region Merge properties for selected classes
                        //Dictionary<string, List<SimilarClassPropertyDescription>> simDict = GetSimilarClassPropertiesMatrix(
                        //    classUri1: class1,
                        //    classUri2: class2,
                        //    progress: getClassPropsMatrixProgress); //obtain properties similarity matrix

                        Dictionary<string, List<SimilarClassPropertyDescription>> simDict = getSimilarClassPropertiesMatrixMethod(
                            class1, //classUri1
                            class2, //classUri2
                            getClassPropsMatrixProgress //progress
                        ); //obtain properties similarity matrix

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

                                    INode oprop1 = _merged.Nodes.Where(node=>node.ToString()==mergePropertyPair.ObjectName1).First(); //!!!!!! спорная ситуация
                                    federatedProperty.AddSubProperty(_merged.CreateOntologyProperty(oprop1));
                                    INode oprop2 = _merged.Nodes.Where(node => node.ToString() == mergePropertyPair.ObjectName2).First(); //!!!!!! спорная ситуация
                                    federatedProperty.AddSubProperty(_merged.CreateOntologyProperty(oprop2));

                                    //to specify domain and range we should MERGE THE DATATYPES!!!

                                    var prop01Range = _o1.GetTriplesWithSubjectPredicate(_o1.CreateUriNode(new Uri(mergePropertyPair.ObjectName1)), _o1.CreateUriNode("rdfs:range")).ToList();
                                    var prop02Range = _o2.GetTriplesWithSubjectPredicate(_o2.CreateUriNode(new Uri(mergePropertyPair.ObjectName2)), _o2.CreateUriNode("rdfs:range")).ToList();

                                    if (prop01Range.Count > 1 || prop02Range.Count > 1)
                                        throw new InvalidOperationException($"Property can have only one rdfs:range defined! [Properties {mergePropertyPair.ObjectName1} & {mergePropertyPair.ObjectName2} ]");
                                    if (prop01Range.Count != prop02Range.Count)
                                        throw new InvalidOperationException($"Properties should both have 1 (or zero) range(s) defined [Properties {mergePropertyPair.ObjectName1} & {mergePropertyPair.ObjectName2} ]");
                                    //however, props are allowed to not have range defined

                                    string prop01RangeStr = (prop01Range.First() as Triple).Object.ToString();
                                    string prop02RangeStr = (prop02Range.First() as Triple).Object.ToString();

                                    string castedRange = string.Empty;
                                    //check, if prop01RangeStr OR prop02RangeStr not to be a resource (otherwise, it's a range of an object property,
                                    //and, if ranges of different ObjectProperties don't coincide, we can't merge them, so, delete current federatedProperty
                                    Regex r = new Regex(@"http\w{0,1}://.+");
                                    SimilarClassPropertyDescription mergedClasses = null; //in case we merge resource ranges and need a federated range (case 2)
                                    if(r.IsMatch(prop01RangeStr) || r.IsMatch(prop02RangeStr))
                                    {
                                        //check that prop01RangeStr==prop02RangeStr
                                        if(prop01RangeStr==prop02RangeStr)
                                        {
                                            //OK->federated Range is equal to prop01RangeStr\prop02RangeStr
                                            castedRange = prop01RangeStr;
                                            //federatedProperty.AddRange(new Uri($"xsd:{castedRange}")); -> BUG
                                            federatedProperty.AddRange(new Uri(castedRange));
                                            federatedProperty.AddDomain(new Uri(mergedClassPair.FederatedURI));
                                            OntologyResource ontologyPropertyResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlObjectProperty));
                                            federatedProperty.AddType(ontologyPropertyResource);
                                        }
                                        else if((mergedClasses = mergedClassPairs.FirstOrDefault(simClassProp=> {
                                            return (simClassProp.ObjectName1 == prop01RangeStr &&
                                                   simClassProp.ObjectName2 == prop02RangeStr) ||
                                                   (simClassProp.ObjectName1 == prop02RangeStr &&
                                                   simClassProp.ObjectName2 == prop01RangeStr);
                                            }))!=null) //we've found merge pair which states that classes, correspoinding to prop01RangeStr & prop02RangeStr are being merged
                                        {
                                            //if ranges' URIs correspond to classes which are being merged, than we CREATE an ObjectProperty with several ranges
                                            federatedProperty.AddRange(new Uri(prop01RangeStr));
                                            federatedProperty.AddRange(new Uri(prop02RangeStr));
                                            federatedProperty.AddDomain(new Uri(mergedClassPair.FederatedURI));
                                            //add here FEDERATED range too
                                            //get federated class name -> superclass of these 2 given merged classes
                                            if(string.IsNullOrEmpty(mergedClasses.FederatedURI)) //if classes have not been merged yet
                                            {
                                                //fragment copied from above [when merging classes]
                                                o1ClassName = GetClassNameFromUri(mergedClasses.ObjectName1);
                                                o2ClassName = GetClassNameFromUri(mergedClasses.ObjectName2);

                                                federatedClassName = federatedNamesGen.GenerateFederatedName(o1ClassName, o2ClassName);

                                                federatedUri = federatedStem + federatedClassName;
                                                mergedClasses.FederatedURI = federatedUri;
                                            }
                                            federatedProperty.AddRange(new Uri(mergedClasses.FederatedURI));

                                            OntologyResource ontologyPropertyResource = _merged.CreateOntologyResource(new Uri(OntologyHelper.OwlObjectProperty));
                                            federatedProperty.AddType(ontologyPropertyResource);
                                            
                                        }
                                        else
                                        {
                                            //ranges of different ObjectProperties don't coincide, we can't merge them, so, don't add current federatedProperty to graph
                                            //federated schema should reference federated schema, otherwise it's a logical error!
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        //ranges should be XSD!, because we're using XSDTypeCaster by default
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
                        #endregion
                    }
                    else
                    {
                        throw new NotImplementedException("Merging for other types of relations is not implemented!");
                    }
                }
                else continue; //didn't pass through threshold
            }
            _merged.SaveToFile("mergedOntology.log.owl");
            return _merged;
        }

        protected string GetClassNameFromUri(string objectName)
        {
            int indexOfLastSlash01 = objectName.LastIndexOf('/');
            string oClassName = objectName.Substring(indexOfLastSlash01 + 1,
                                                                    objectName.Length - 1 - indexOfLastSlash01);

            return oClassName;
        }
    }
}
