using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace PatternDetector.Detectors
{
    class IteratorDetector : AbstractPatternDetector
    {
        public IteratorDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
            : base(typeDeclaration, semanticModel) { }

        public override bool Detect()
        {
            if (TypeInfo.TypeKind == TypeKind.Interface || TypeInfo.IsStatic)
                return false;

            if (!TypeInfo.IsAbstract && 
                TypeInfo.AllInterfaces.Any(@interface => @interface.SpecialType == SpecialType.System_Collections_IEnumerable))
            {
                return true;
            }

            var getEnumerator = FindGetEnumerator(TypeInfo);

            if (getEnumerator != null)
                return !getEnumerator.IsAbstract;

            getEnumerator = FirstAncestorGetEnumerator(TypeInfo);

            return getEnumerator != null && !getEnumerator.IsAbstract;
        }

        private IMethodSymbol FindGetEnumerator(INamedTypeSymbol typeSymbol)
        {
            var methods = FindMethods(typeSymbol);

            foreach (IMethodSymbol method in methods)
            {
                if (IsGetEnumerator(method))
                    return method;
            }

            return null;
        }


        private IMethodSymbol FirstAncestorGetEnumerator(INamedTypeSymbol typeSymbol)
        {
            var typeAncestors = AllAncestors(typeSymbol);

            foreach (INamedTypeSymbol typeAncestor in typeAncestors)
            {
                var foundGetEnumerator = FindGetEnumerator(typeAncestor);

                if (foundGetEnumerator != null)
                    return foundGetEnumerator;
            }

            return null;
        }

        private bool IsGetEnumerator(IMethodSymbol analyzedMethod)
        {
            if (analyzedMethod.IsStatic ||
                analyzedMethod.DeclaredAccessibility != Accessibility.Public ||
                analyzedMethod.Name != "GetEnumerator" ||
                analyzedMethod.Parameters.Length > 0 ||
                analyzedMethod.ReturnsVoid)
            {
                return false;
            }

            return IsEnumerator(analyzedMethod.ReturnType);
        }


        private bool IsEnumerator(ITypeSymbol analyzedType)
        {
            if (analyzedType.TypeKind != TypeKind.Interface && 
                analyzedType.TypeKind != TypeKind.Class &&
                analyzedType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            if (analyzedType.SpecialType == SpecialType.System_Collections_IEnumerator ||
                analyzedType.AllInterfaces.Any(@interface => @interface.SpecialType == SpecialType.System_Collections_IEnumerator))
            {
                return true;
            }

            INamedTypeSymbol analyzedNamedType = (INamedTypeSymbol)analyzedType;

            return HasMoveNextAndCurrent(analyzedNamedType);
        }

        private bool HasMoveNextAndCurrent(INamedTypeSymbol searchType)
        {
            bool hasMoveNext = HasMoveNext(searchType) || AllAncestors(searchType).Any(an => HasMoveNext(an));

            if (!hasMoveNext) 
                return false;

            bool hasCurrent = HasCurrent(searchType) || AllAncestors(searchType).Any(an => HasCurrent(an));

            return hasCurrent;
        }

        private bool HasMoveNext(INamedTypeSymbol searchType)
        {
            var typeMethods = FindMethods(searchType);

            foreach (IMethodSymbol typeMethod in typeMethods)
            {
                if (typeMethod.ReturnType.SpecialType == SpecialType.System_Boolean &&
                    typeMethod.DeclaredAccessibility == Accessibility.Public &&
                    !typeMethod.IsStatic &&
                    typeMethod.Parameters.Length == 0 &&
                    typeMethod.Name == "MoveNext")
                {
                    return true;
                }
            }

            return false;
        }


        private bool HasCurrent(INamedTypeSymbol searchType)
        {
            var typeProperties = FindProperties(searchType);

            foreach (IPropertySymbol typeProperty in typeProperties)
            {
                if (typeProperty.DeclaredAccessibility == Accessibility.Public &&
                    !typeProperty.IsStatic &&
                    typeProperty.Name == "Current" &&
                    !typeProperty.IsWriteOnly)
                {
                    return true;
                }
            }

            return false;
        }
    }
}