﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingGenerator
{
    public interface IInteractiveMerger
    {
        void MergeOntologyClasses(List<SimilarClassPropertyDescription> mergedClassPairs, 
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
