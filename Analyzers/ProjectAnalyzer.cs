using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace PatternDetector.Analyzers
{
    class ProjectAnalyzer
    {
        public Project Project { get; private set; }
        public List<ProjectResult> ProjectResults { get; private set; } = new List<ProjectResult>();

        public ProjectAnalyzer(Project project)
        {
            Project = project;
            var compilation = project.GetCompilationAsync().Result;

            foreach (Document document in project.Documents)
            {
                DocumentAnalyzer documentAnalyzer = new DocumentAnalyzer(document, compilation);

                if (documentAnalyzer.DocumentResults.Count > 0)
                {
                    ProjectResults.Add(new ProjectResult(document.Name, documentAnalyzer.DocumentResults));
                    Program.detectedDocumentsCount++;
                }
            }
        }
    }
}
