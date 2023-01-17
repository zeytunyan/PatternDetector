using System.Collections.Generic;


namespace PatternDetector.Structs
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

    struct SolutionResult
    {
        public string ProjectName;
        public List<ProjectResult> ProjectResults;

        public SolutionResult(string projectName, List<ProjectResult> projectResults)
        {
            ProjectName = projectName;
            ProjectResults = projectResults;
        }
    }
}
