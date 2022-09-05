using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternDetector
{
    abstract class PatternDetector
    {
        protected TypeDeclarationSyntax TypeDeclaration { get; private set; }
        protected INamedTypeSymbol TypeInfo { get; private set; }
        protected SemanticModel SemanticModel { get; private set; }

        public PatternDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) 
        {
            TypeDeclaration = typeDeclaration;
            SemanticModel = semanticModel;
            TypeInfo = SemanticModel.GetDeclaredSymbol(typeDeclaration);
        }

        public abstract bool Detect();


        public IEnumerable<IMethodSymbol> FindMethods(INamedTypeSymbol typeSymbol) =>
            FindMembers(typeSymbol, SymbolKind.Method).Select(member => (IMethodSymbol)member);

        public IEnumerable<IPropertySymbol> FindProperties(INamedTypeSymbol typeSymbol) =>
            FindMembers(typeSymbol, SymbolKind.Property).Select(member => (IPropertySymbol)member);

        public IEnumerable<IFieldSymbol> FindFields(INamedTypeSymbol typeSymbol) => 
            FindMembers(typeSymbol, SymbolKind.Field).Select(field => (IFieldSymbol)field);

        public IEnumerable<ISymbol> FindMembers(INamedTypeSymbol typeSymbol, SymbolKind kind) => 
            typeSymbol.GetMembers().Where(mbr => mbr.Kind == kind);


        protected IEnumerable<INamedTypeSymbol> AllAncestors(INamedTypeSymbol namedType, bool onlyAbstract = false)
        {
            List<INamedTypeSymbol> ancestors = new List<INamedTypeSymbol>();

            void FindAncestors(INamedTypeSymbol type)
            {
                var baseType = type.BaseType;

                if (baseType == null || baseType.SpecialType.Equals(SpecialType.System_Object))
                    return;

                if (!onlyAbstract || type.BaseType.IsAbstract)
                    ancestors.Add(type.BaseType);

                FindAncestors(type.BaseType);
            }

            FindAncestors(namedType);

            return (ancestors);
        }

        protected IEnumerable<INamedTypeSymbol> AllAncestorsAndSelf(INamedTypeSymbol namedType, bool onlyAbstract = false) => 
            AllAncestors(namedType, onlyAbstract).Prepend(namedType);


        protected IBlockOperation GetMethodOperation(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
                return null;

            IOperation methodOperation = SemanticModel.GetOperation(methodDeclaration);

            if (methodOperation == null)
                return null;

            IMethodBodyBaseOperation methodBody = (IMethodBodyBaseOperation)methodOperation;
            
            return methodBody.BlockBody ?? methodBody.ExpressionBody;
        }

        protected MethodDeclarationSyntax GetMethodSyntaxFromSymbol(IMethodSymbol methodSymbol)
        {
            var methodSyntaxReferences = methodSymbol.DeclaringSyntaxReferences;

            if (methodSyntaxReferences.Count() == 0)
                return null;

            var methodSyntax = methodSyntaxReferences.First().GetSyntax();

            if (methodSyntax == null || methodSyntax.Kind() != SyntaxKind.MethodDeclaration)
                return null;

            return (MethodDeclarationSyntax)methodSyntax;
        }

        protected bool IsEnumerable(ITypeSymbol type) 
        {
            return type.SpecialType == SpecialType.System_Collections_IEnumerable ||
                type.AllInterfaces.Any(@interface => @interface.SpecialType == SpecialType.System_Collections_IEnumerable);
        }
    }
}
