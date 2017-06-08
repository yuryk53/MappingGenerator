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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MappingGenerator
{
    class OWLWriter
    {
        List<string> stdNamespaces = new List<string>();
        public OWLWriter()
        {
            stdNamespaces.Add("xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"");
            stdNamespaces.Add("xmlns:rdfs = \"http://www.w3.org/2000/01/rdf-schema#\"");
            stdNamespaces.Add("xmlns:owl = \"http://www.w3.org/2002/07/owl#\"");
            stdNamespaces.Add("xmlns:xsd =\"http://www.w3.org/2001/XMLSchema#\"");
        }

        /// <summary>
        /// Adds new namespace to stdNamespaces
        /// </summary>
        /// <param name="namespaceString">string of type: xmlns:owl="http://www.w3.org/2002/07/owl#"</param>
        public void AddNamespace(string namespaceString)
        {
            namespaceString = namespaceString.Replace(" ", "");
            Regex r = new Regex("xmlns:\\w+=\"http\\w{0,1}://.+\"");
            Console.WriteLine(r.ToString());
            Console.WriteLine(namespaceString);
            if (r.IsMatch(namespaceString))
            {
                stdNamespaces.Add(namespaceString);
            }
            else throw new ArgumentException("Invalid namespace string!");
        }

        public void EmitSimpleOWLClass(StringBuilder sb, string rdfAboutUri, string rdfsSubClassOfUri=null)
        {
            if (MatchUri(rdfAboutUri))
            {
                sb.AppendLine($"<owl:Class rdf:about=\"{rdfAboutUri}\">");
                if (rdfsSubClassOfUri != null)
                {
                    sb.AppendLine($"<rdfs:subClassOf rdf:resource=\"{rdfsSubClassOfUri}\"");
                }
                sb.AppendLine("</owl:Class>");
            }
            else throw new ArgumentException("Invalid rdf:about uri in owl:class description!");
        }

        public void EmitSimpleDataTypeProp(StringBuilder sb, string rdfAboutUri, string domainResourceUri, string rangeXSDType)
        {
            if (MatchUri(rdfAboutUri))
            {
                sb.AppendLine($"<owl:DatatypeProperty rdf:about=\"{rdfAboutUri}\">");
                sb.AppendLine($"\t<rdfs:domain rdf:resource=\"{domainResourceUri}\" />");
                sb.AppendLine($"\t<rdfs:range rdf:resource=\"xsd:{rangeXSDType}\" />");
                sb.AppendLine("</owl:DatatypeProperty>");
            }
            else throw new ArgumentException("Invalid rdf:about URI!");
        }

        public void EmitSimpleObjectProp(StringBuilder sb, string rdfAboutUri, string domainResourceUri, string rangeResourceUri)
        {
            if (MatchUri(rdfAboutUri))
            {
                sb.AppendLine($"<owl:ObjectProperty rdf:about=\"{rdfAboutUri}\">");
                sb.AppendLine($"\t<rdfs:domain rdf:resource=\"{domainResourceUri}\" />");
                sb.AppendLine($"\t<rdfs:range rdf:resource=\"{rangeResourceUri}\" />");
                sb.AppendLine("</owl:ObjectProperty>");
            }
            else throw new ArgumentException("Invalid rdf:about URI!");
        }

        private bool MatchUri(string uri)
        {
            uri = uri.Replace(" ", "");
            Regex r = new Regex(@"^http\w{0,1}://.+$");
            return r.IsMatch(uri);
        }

        public void WriteHeader(StringBuilder sb)
        {
            sb.Append($"<rdf:RDF\n {string.Join("\n\t", stdNamespaces)}>\n");
        }

        public void WriteFooter(StringBuilder sb)
        {
            sb.AppendLine("</rdf:RDF>");
        }
    }
}
