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
                // Вывод сообщения для события WorkspaceFailed, чтобы помочь диагностировать сбои загрузки проекта.
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
            // Попытка установить версию MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // Если на этом компьютере есть только один экземпляр MSBuild, он и выбирается.
                ? visualStudioInstances[0]
                // Выбор версии MSBuild.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine("Для загрузки проектов используется MSBuild по адресу: ");
            Console.WriteLine($"{instance.MSBuildPath}");
            Console.WriteLine();

            /* 
            ВНИМАНИЕ: 
                Перед вызовом MSBuildWorkspace.Create() 
                обязательно заргистрируйте экземпляр в MSBuildLocator, 
                в противном случае MSBuildWorkspace не будет составлять MEF.
            */
            MSBuildLocator.RegisterInstance(instance);
        }


        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Обнаружено несколько установок MSBuild, выберите одну:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Экземпляр {i + 1}");
                Console.WriteLine($"    Название: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Версия: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    Путь: {visualStudioInstances[i].MSBuildPath}");
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
                Console.WriteLine("Ввод не принят, попробуйте еще раз.");
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
                Console.WriteLine("Не удалось загрузить решение, попробуйте еще раз. ");
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
            // Получаем путь к решению из консоли 
            Console.WriteLine("Укажите путь к решению: ");
            string path = Console.ReadLine().Trim();
            Console.WriteLine();
            Console.WriteLine("Подождите, идёт загрузка решения... ");
            Console.WriteLine();
            return path;
        }


        private static void SolutionLoaded(Solution loadedSolution)
        {
            Console.WriteLine($@"Завершена загрузка решения по адресу:
{loadedSolution.FilePath}

Всего проектов в решении: {allProjectsCount}
Всего документов в решении: {allDocumentsCount}

Подождите, идёт поиск...
");
        }


        private static SolutionAnalyzer AnalyzeSolution(Solution analyzedSolution)
        {
            SolutionAnalyzer solutionAnalyzer = new SolutionAnalyzer(analyzedSolution);

            Console.WriteLine("Поиск завершен!");
            Console.WriteLine();

            return solutionAnalyzer;
        }


        private static bool NotWantToContinue()
        {
            Console.WriteLine("Нажмите Enter, чтобы повторить анализ, или любую другую клавишу, чтобы выйти");
            var consoleKey = Console.ReadKey().Key;
            Console.WriteLine();

            return consoleKey != ConsoleKey.Enter;
        }
    }
}
