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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace MappingGenerator
{
    public interface IInteractiveMerger
    {
        IGraph MergeOntologyClasses(List<SimilarClassPropertyDescription> mergedClassPairs, 
                                  Func<SimilarClassPropertyDescription, bool> canWeMergePairCallback,
                                  Func<SimilarClassPropertyDescription, bool> canWeMergePropertyPairCallback,
                                  double mergePropertiesThreshold,
                                  IFederatedNamesGenerator federatedNamesGen,
                                  ITypeCaster typeCaster,
                                  IProgress<double> progress=null,
                                  Func<string, string, IProgress<double>, Dictionary<string, List<SimilarClassPropertyDescription>>> getSimilarClassPropertiesMatrixMethod = null,
                                  string federatedStem = null
                                  );
    }
}
