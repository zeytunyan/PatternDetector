using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternDetector
{
    class Demonstration
    {
        public Demonstration(SolutionAnalyzer solutionAnalyzer)
        {
            if (solutionAnalyzer.SolutionResults.Count == 0)
            {
                Console.WriteLine($"В решении не обнаружено использование паттернов проектирования.");
            }
            else
            {
                DemonstrateTable(solutionAnalyzer.SolutionResults);
                DemonstrateSolutionStatistics();
                DemonstratePatternsStatistic();
            }
        }

        public void DemonstrateTable(List<SolutionResult> resultList)
        {
            int numCellLength = 6;
            int smallCellLength = 50;
            int bigCellLength = 60;

            Dictionary<int, int> borderData = new Dictionary<int, int>()
            {
                { numCellLength, 1 },
                { smallCellLength, 3 },
                { bigCellLength, 1}
            };

            string rowBorder = MakeRowBorder(borderData, "-");

            Console.SetWindowSize(numCellLength + smallCellLength * 3 + bigCellLength + 10, 60);

            Console.WriteLine("Результаты поиска:");
            Console.WriteLine(rowBorder);
            Console.WriteLine(MakeRow(borderData, new string[]{ "Номер", "Проект", "Документ", "Класс/Интерфейс/Структура", "Паттерны" }));
            Console.WriteLine(MakeRowBorder(borderData, "="));

            int num = 0;

            foreach (SolutionResult solutionResult in resultList)
            {
                string projectCell = MakeCell(solutionResult.ProjectName, smallCellLength);

                foreach (ProjectResult projectResult in solutionResult.ProjectResults)
                {
                    string documentCell = MakeCell(projectResult.DocumentName, smallCellLength);

                    foreach (DocumentResult documentResult in projectResult.DocumentResults)
                    {
                        string[] values = {
                            Convert.ToString(++num),
                            projectCell,
                            documentCell,
                            documentResult.ClassName,
                            String.Join<string>(", ", documentResult.ClassResults)
                        };

                        Console.WriteLine(MakeRow(borderData, values));
                        Console.WriteLine(rowBorder);
                    }
                }
            }

            Console.WriteLine();
        }

        public void DemonstrateSolutionStatistics()
        {
            Dictionary<int, int> borderData = new Dictionary<int, int>()
            {
                { 30, 1 },
                { 13, 1 },
                { 16, 1}
            };

            string rowBorder = MakeRowBorder(borderData, "-");

            string[][] mainRows = {
                new string[]  { " ", "С паттернами", "Всего в решении" },
                new string[] { "Проектов", Convert.ToString(Program.detectedProjectsCount), Convert.ToString(Program.allProjectsCount) },
                new string[] { "Документов", Convert.ToString(Program.detectedDocumentsCount),  Convert.ToString(Program.allDocumentsCount) },
                new string[] { "Классов/Интерфейсов/Структур", Convert.ToString(Program.detectedClassesCount), Convert.ToString(Program.allClassesCount)},
            };

            Console.WriteLine("Статистика по решению:");
            Console.WriteLine(rowBorder);

            foreach (var mainRow in mainRows)
            {
                Console.WriteLine(MakeRow(borderData, mainRow));
                Console.WriteLine(rowBorder);
            }
            
            Console.WriteLine();
        }


        private void DemonstratePatternsStatistic()
        {
            Dictionary<int, int> borderData = new Dictionary<int, int>()
            {
                { 30, 1 },
                { 11, 1 }
            };
            
            string rowBorder = MakeRowBorder(borderData, "-");

            Console.WriteLine("Статистика по паттернам:");
            Console.WriteLine(rowBorder);
            Console.WriteLine(MakeRow(borderData, new string[] { "Паттерн", "Количество" }));
            Console.WriteLine(rowBorder);

            foreach (KeyValuePair<string, int> patternCount in Program.eachPatternCount)
            {
                Console.WriteLine(MakeRow(borderData, new string[] { patternCount.Key, Convert.ToString(patternCount.Value) }));
                Console.WriteLine(rowBorder);
            }

            Console.WriteLine(MakeRow(borderData, new string[] { "Все вместе", Convert.ToString(Program.detectedPatternsCount) }));
            Console.WriteLine(rowBorder);
            Console.WriteLine();
        }


        private string MakeCell(string str, int cellLength, string placeHolder = "...")
        {
            if (cellLength < 3) 
                cellLength = 3;
            
            if (placeHolder.Length > 3) 
                placeHolder = "...";

            if (str.Length <= cellLength)
                return str.PadRight(cellLength);

            return str.Substring(0, cellLength - 3) + placeHolder;
        }

        private string MakeCellBorder(string symbol, int length)
        {
            string res = "";

            for (int i = 0; i < length; i++)
                res += symbol;

            return res;
        }

        private string MakeRowBorder(Dictionary<int, int> data, string symbol)
        {
            string border = "+";

            foreach (var d in data)
                for (int i = 0; i < d.Value; i++)
                    border += MakeCellBorder(symbol, d.Key) + "+";

            return border;
        }

        private string MakeRow(Dictionary<int, int> rowData, string[] rowValues)
        {
            string row = "|";
            int arI = 0;

            foreach (var rd in rowData)
            {
                for (int i = 0; i < rd.Value; i++)
                {
                    row += MakeCell(rowValues[arI], rd.Key) + "|";
                    arI++;
                }
            }

            return row;
        }
    }
}
