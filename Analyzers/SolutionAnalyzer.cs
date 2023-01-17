using Microsoft.CodeAnalysis;
using PatternDetector.Structs;
using System.Collections.Generic;

namespace PatternDetector.Analyzers
{
    class SolutionAnalyzer
    {
        public Solution Solution { get; private set; }
        public List<SolutionResult> SolutionResults { get; private set; } = new List<SolutionResult>();

        public SolutionAnalyzer(Solution solution)
        {
            Solution = solution;

            foreach (Project project in solution.Projects)
            {
                ProjectAnalyzer projectAnalyzer = new ProjectAnalyzer(project);

                if (projectAnalyzer.ProjectResults.Count > 0)
                {
                    SolutionResults.Add(new SolutionResult(project.Name, projectAnalyzer.ProjectResults));
                    Program.detectedProjectsCount++;
                }
            }
        }
    }
}
