using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PatternDetector.Detectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternDetector
{
    class DocumentAnalyzer
    {
        public Document Document { get; private set; }
        public List<DocumentResult> DocumentResults { get; private set; } = new List<DocumentResult>();

        public DocumentAnalyzer(Document document, Compilation compilation)
        {
            Document = document;
            var tree = document.GetSyntaxTreeAsync().Result;
            var semanticModel = compilation.GetSemanticModel(tree);

            var typeDeclarations = tree.GetRoot().DescendantNodes()
                .Where(node => node.Kind() == SyntaxKind.ClassDeclaration || 
                node.Kind() == SyntaxKind.InterfaceDeclaration || 
                node.Kind() == SyntaxKind.StructDeclaration)
                .Select(typeNode => (TypeDeclarationSyntax)typeNode);

            foreach (TypeDeclarationSyntax typeDeclaration in typeDeclarations)
            {
                Program.allClassesCount++;
                ClassAnalyzer classAnalyzer = new ClassAnalyzer(typeDeclaration, semanticModel);

                if (classAnalyzer.ClassResults.Count > 0)
                {
                    DocumentResults.Add(new DocumentResult(typeDeclaration.Identifier.ValueText, classAnalyzer.ClassResults));
                    Program.detectedClassesCount++;
                }
            }
        }
    }
}
