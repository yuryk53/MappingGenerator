using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingGenerator
{
    public class ShortestFederatedNamesGenerator : IFederatedNamesGenerator
    {
        string IFederatedNamesGenerator.GenerateFederatedName(string title1, string title2)
        {
            if (title1.Length < title2.Length)
                return title1;
            else return title2;
        }
    }
}
