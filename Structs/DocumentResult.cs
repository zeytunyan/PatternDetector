
using System.Collections.Generic;


namespace PatternDetector
{
    struct DocumentResult
    {
        public string ClassName;
        public List<string> ClassResults;

        public DocumentResult(string className, List<string> classResults)
        {
            ClassName = className;
            ClassResults = classResults;
        }
    }
}
