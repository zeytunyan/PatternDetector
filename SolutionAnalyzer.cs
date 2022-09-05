using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternDetector
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
