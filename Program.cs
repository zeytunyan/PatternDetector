using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using PatternDetector.Analyzers;
using PatternDetector.Interface;

namespace PatternDetector
{
    class Program
    {
        public static int allProjectsCount;
        public static int allDocumentsCount;
        public static int allClassesCount;

        public static int detectedProjectsCount;
        public static int detectedDocumentsCount;
        public static int detectedClassesCount;
        public static int detectedPatternsCount;

        public static Dictionary<string, int> eachPatternCount;


        static async Task Main(string[] args)
        {
            SetConsole();
            SetMSBuild();

            using (var workspace = MSBuildWorkspace.Create())
            {
                // ����� ��������� ��� ������� WorkspaceFailed, ����� ������ ��������������� ���� �������� �������.
                // workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                while (true)
                {
                    Solution solution = await GetSolution(workspace);

                    if (solution == null)
                        continue;

                    SolutionAnalyzer analyzeResult = AnalyzeSolution(solution);
                    Demonstration demonstration = new Demonstration(analyzeResult);

                    if (NotWantToContinue())
                        break;
                }
            }
        }


        private static void SetConsole()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.Clear();
            //Console.SetWindowSize(150, 25);
        }

        private static void SetMSBuild()
        {
            // ������� ���������� ������ MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // ���� �� ���� ���������� ���� ������ ���� ��������� MSBuild, �� � ����������.
                ? visualStudioInstances[0]
                // ����� ������ MSBuild.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine("��� �������� �������� ������������ MSBuild �� ������: ");
            Console.WriteLine($"{instance.MSBuildPath}");
            Console.WriteLine();

            /* 
            ��������: 
                ����� ������� MSBuildWorkspace.Create() 
                ����������� �������������� ��������� � MSBuildLocator, 
                � ��������� ������ MSBuildWorkspace �� ����� ���������� MEF.
            */
            MSBuildLocator.RegisterInstance(instance);
        }


        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("���������� ��������� ��������� MSBuild, �������� ����:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"��������� {i + 1}");
                Console.WriteLine($"    ��������: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    ������: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    ����: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine();
                Console.WriteLine("���� �� ������, ���������� ��� ���.");
                Console.WriteLine();
            }
        }


        private static async Task<Solution> GetSolution(MSBuildWorkspace msWorkspace)
        {
            Solution solutionResult;

            string solutionPath = GetPath();

            try
            {
                solutionResult = await msWorkspace.OpenSolutionAsync(solutionPath);
            }
            catch
            {
                Console.WriteLine("�� ������� ��������� �������, ���������� ��� ���. ");
                Console.WriteLine();
                return null;
            }

            allProjectsCount = solutionResult.Projects.Count();
            allDocumentsCount = solutionResult.Projects.Sum(project => project.Documents.Count());
            allClassesCount = 0;

            eachPatternCount = new Dictionary<string, int>
            {
                { "Composite", 0 },
                { "Iterator", 0 },
                { "Observer", 0 },
                { "Prototype", 0 },
                { "Singleton",   0 },
                { "Template Method", 0},
            };

            detectedProjectsCount = 0;
            detectedDocumentsCount = 0;
            detectedClassesCount = 0;
            detectedPatternsCount = 0;

            SolutionLoaded(solutionResult);

            return solutionResult;
        }


        private static string GetPath()
        {
            // �������� ���� � ������� �� ������� 
            Console.WriteLine("������� ���� � �������: ");
            string path = Console.ReadLine().Trim();
            Console.WriteLine();
            Console.WriteLine("���������, ��� �������� �������... ");
            Console.WriteLine();
            return path;
        }


        private static void SolutionLoaded(Solution loadedSolution)
        {
            Console.WriteLine($@"��������� �������� ������� �� ������:
{loadedSolution.FilePath}

����� �������� � �������: {allProjectsCount}
����� ���������� � �������: {allDocumentsCount}

���������, ��� �����...
");
        }


        private static SolutionAnalyzer AnalyzeSolution(Solution analyzedSolution)
        {
            SolutionAnalyzer solutionAnalyzer = new SolutionAnalyzer(analyzedSolution);

            Console.WriteLine("����� ��������!");
            Console.WriteLine();

            return solutionAnalyzer;
        }


        private static bool NotWantToContinue()
        {
            Console.WriteLine("������� Enter, ����� ��������� ������, ��� ����� ������ �������, ����� �����");
            var consoleKey = Console.ReadKey().Key;
            Console.WriteLine();

            return consoleKey != ConsoleKey.Enter;
        }
    }
}
