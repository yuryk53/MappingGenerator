using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingGenerator
{
    public interface ITypeCaster
    {
        string CastTypes(string typeName1, string typeName2);
    }
}
