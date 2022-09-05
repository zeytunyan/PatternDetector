using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PatternDetector.Detectors
{
    class CompositeDetector : PatternDetector
    {
        public CompositeDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
            : base(typeDeclaration, semanticModel) { }


        public override bool Detect()
        {
            if (TypeInfo.TypeKind == TypeKind.Interface || TypeInfo.IsStatic) // TypeInfo.BaseType == null || methods.Count == 0
                return false;

            var fields = FindFields(TypeInfo); 

            return fields.Any(field => IsSameTypeField(field));
        }

        private bool IsSameTypeField(IFieldSymbol analyzedField)
        {
            var fieldType = analyzedField.Type;

            if (analyzedField.IsStatic || !IsEnumerable(fieldType))
                return false;

            var typeKind = fieldType.TypeKind;

            if (typeKind == TypeKind.Array)
            {
                var arrayField = (IArrayTypeSymbol)fieldType;
                var arrayType = arrayField.ElementType;

                return IsSameType(arrayType);
            }
            else if (typeKind == TypeKind.Class || typeKind == TypeKind.Struct || typeKind == TypeKind.Interface)
            {
                var classField = (INamedTypeSymbol)fieldType;

                if (!classField.IsGenericType)
                    return false;

                return classField.TypeArguments.Any(genericType => IsSameType(genericType));
            }

            return false;
        }

        private bool IsSameType(ITypeSymbol genericType)
        {
            if (genericType.TypeKind != TypeKind.Class && 
                genericType.TypeKind != TypeKind.Interface &&
                genericType.TypeKind != TypeKind.Struct)
            {
                return false;
            }  

            INamedTypeSymbol genericNamedType = (INamedTypeSymbol)genericType; 

            if (genericType.TypeKind == TypeKind.Class && SameType(genericNamedType))
                return true;

            return SameInterface(genericNamedType);
        }

        private bool SameType(INamedTypeSymbol analyzedType)
        {
            var classAncestors = AllAncestorsAndSelf(TypeInfo);
            var analyzedTypeAncestors = AllAncestorsAndSelf(analyzedType);

            bool rootFounded = false;

            foreach (INamedTypeSymbol classAncestor in classAncestors)
            {
                if (!rootFounded && analyzedTypeAncestors.Any(ancestor => ancestor.Equals(classAncestor)))
                    rootFounded = true;

                if (rootFounded && HasMethods(classAncestor))
                    return true;
            }

            return false;
        }

        private bool SameInterface(INamedTypeSymbol analyzedType)
        {
            var classInterfaces = TypeInfo.AllInterfaces;

            if (analyzedType.TypeKind == TypeKind.Interface && 
                classInterfaces.Any(@interface => @interface.Equals(analyzedType)) &&
                HasMethods(analyzedType))
            {
                return true;
            }

            var analyzedTypeInterfaces = analyzedType.AllInterfaces;

            foreach (INamedTypeSymbol classInterface in classInterfaces)
            {
                if (analyzedTypeInterfaces.Any(@interface => @interface.Equals(classInterface)) 
                    && HasMethods(classInterface))
                {
                    return true;
                }  
            }
            
            return false;
        }
        
        private bool HasMethods(INamedTypeSymbol namedTypeSymbol)
        {
            var methods = FindMethods(namedTypeSymbol);

            var normalMethods = methods
                .Where(method => method.MethodKind == MethodKind.Ordinary ||
                method.MethodKind == MethodKind.LambdaMethod ||
                method.MethodKind == MethodKind.ExplicitInterfaceImplementation || 
                method.MethodKind == MethodKind.PropertyGet || 
                method.MethodKind == MethodKind.PropertySet);

            foreach (IMethodSymbol normalMethod in normalMethods)
            {
                if (!normalMethod.IsStatic &&
                    normalMethod.CanBeReferencedByName &&
                    normalMethod.DeclaredAccessibility != Accessibility.Private &&
                    normalMethod.DeclaredAccessibility != Accessibility.Protected &&
                    normalMethod.DeclaredAccessibility != Accessibility.ProtectedAndInternal)
                {
                    return true;
                }
            }

            return false;
        }
    }
}