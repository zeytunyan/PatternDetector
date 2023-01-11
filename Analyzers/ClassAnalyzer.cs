using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PatternDetector.Detectors;
using System;
using System.Collections.Generic;

namespace PatternDetector.Analyzers
{
    class ClassAnalyzer
    {
        public Dictionary<string, AbstractPatternDetector> Detectors { get; private set; }
        public List<string> ClassResults { get; private set; } = new List<string>();
        public TypeDeclarationSyntax Type { get; private set; }
        public ClassAnalyzer(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
        {
            Type = typeDeclaration;

            Detectors = new Dictionary<string, AbstractPatternDetector>
            {
                { "Composite", new CompositeDetector(typeDeclaration, semanticModel) },
                { "Iterator", new IteratorDetector(typeDeclaration, semanticModel) },
                { "Observer", new ObserverDetector(typeDeclaration, semanticModel)},
                { "Prototype", new PrototypeDetector(typeDeclaration, semanticModel) },
                { "Singleton",  new SingletonDetector(typeDeclaration, semanticModel) },
                { "Template Method", new TemplateMethodDetector(typeDeclaration, semanticModel) },
            };

            foreach (KeyValuePair<string, AbstractPatternDetector> detector in Detectors)
            {
                if (detector.Value.Detect())
                {
                    ClassResults.Add(detector.Key);
                    Program.eachPatternCount[detector.Key]++;
                    Program.detectedPatternsCount++;
                }

            }
        }
    }
}
