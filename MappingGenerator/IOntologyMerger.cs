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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;

namespace MappingGenerator
{
    //Indicates the type of relationship between the merged properties and federated schema properties
    public enum MergePropertyRelation
    {
        EquivalentProperty, //merged properties are equivalent properties of federated schema property
        SubProperty, //merged properties are subproperties of federated schema property
        NotApplicable
    }

    public enum MergeClassRelation
    {
        SubClassOf, //merged classes are subclasses of federated schema class
        NotApplicable
    }

    public class SimilarClassPropertyDescription
    {
        public string SimObjectURI1 { get; set; }   //OWL property/class URI
        public string SimObjectURI2 { get; set; }   
        public string ObjectName1 { get; set; }     //OWL property/class name
        public string ObjectName2 { get; set; }
        public double SimilarityScore { get; set; } //similarity score [0; 1]
        public string FederatedURI { get; set; } = string.Empty;
        public MergePropertyRelation MergePropRelation { get; set; } = MergePropertyRelation.NotApplicable;
        public MergeClassRelation MergeClassRelation { get; set; } = MergeClassRelation.NotApplicable;
    }

    public interface IOntologyMerger
    {
        /// <summary>
        /// Gets similar classes from two ontologies
        /// </summary>
        /// <param name="threshold">Minimal similarity threshold</param>
        /// <returns>Description for each OWL class\property title</returns>
        Dictionary<string, List<SimilarClassPropertyDescription>> GetSimilarOntologyClassesMatrix(bool includeProperties = true, IProgress<double> progress = null);
        Dictionary<string, List<SimilarClassPropertyDescription>> GetSimilarClassPropertiesMatrix(string classUri1, string classUri2, IProgress<double> progress = null);
        IGraph MergeTwoOntologies(IGraph o1, IGraph o2, double classThreshold, double propertyThreshold, IProgress<double> progress = null);
        void Initialize(OntologyGraph o1, OntologyGraph o2);
    }
}
