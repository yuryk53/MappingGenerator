﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MappingGenerator
{
    public interface IFederatedNamesGenerator
    {
        string GenerateFederatedName(string title1, string title2);
    }
}
