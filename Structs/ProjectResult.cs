using System.Collections.Generic;


namespace PatternDetector
{
    struct ProjectResult
    {
        public string DocumentName;
        public List<DocumentResult> DocumentResults;

        public ProjectResult(string documentName, List<DocumentResult> documentResults)
        {
            DocumentName = documentName;
            DocumentResults = documentResults;
        }
    }
}