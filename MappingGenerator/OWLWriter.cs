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
                sb.AppendLine($"<owl:DataTypeProperty rdf:about=\"{rdfAboutUri}\">");
                sb.AppendLine($"\t<rdfs:domain rdf:resource=\"{domainResourceUri}\" />");
                sb.AppendLine($"\t<rdfs:range rdf:resource=\"xsd:{rangeXSDType}\" />");
                sb.AppendLine("</owl:DataTypeProperty>");
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
