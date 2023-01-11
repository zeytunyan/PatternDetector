using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Linq;

namespace PatternDetector.Detectors
{
    class SingletonDetector : AbstractPatternDetector
    {
        public SingletonDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
            : base(typeDeclaration, semanticModel) { }

        private IFieldSymbol singleField;

        public override bool Detect()
        {
            if (TypeInfo.TypeKind == TypeKind.Interface ||
                TypeInfo.IsValueType ||
                TypeInfo.IsStatic ||
                NonPrivateContructor())
            {
                return false;
            }

            singleField = SingleField();

            if (singleField == null || HasNotOneCreation())
                return false;

            int assignmentsCount = AssignmentsCount();

            if (assignmentsCount > 1 || (assignmentsCount != 0 && singleField.IsReadOnly))
                return false;

            if (singleField.DeclaredAccessibility != Accessibility.Private)
                return singleField.IsReadOnly;

            return HasSingletonMethods() || HasSingletonProperties();
        }


        private bool NonPrivateContructor()
        {
            var constructors = TypeInfo.Constructors;

            foreach (IMethodSymbol constructor in constructors)
            {
                if (constructor.DeclaredAccessibility != Accessibility.Private &&
                    constructor.DeclaredAccessibility != Accessibility.Protected &&
                    constructor.DeclaredAccessibility != Accessibility.ProtectedAndInternal &&
                    !constructor.IsStatic)
                {
                    return true;
                }
            }

            return false;
        }


        private IFieldSymbol SingleField()
        {
            IFieldSymbol result = null;

            var fields = FindAllFields();

            foreach (IFieldSymbol field in fields)
            {
                if (field.IsConst ||
                    !field.IsStatic ||
                    (!field.Type.Equals(TypeInfo) && 
                    (field.Type.SpecialType == SpecialType.System_Object ||
                    !field.Type.Equals(TypeInfo.BaseType))))
                {
                    continue;
                }

                if (result != null)
                    return null;

                result = field;
            }

            return result;
        }

        private List<IFieldSymbol> FindAllFields()
        {
            List<IFieldSymbol> allFields = new List<IFieldSymbol>();

            foreach (ISymbol member in TypeInfo.GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                    allFields.Add((IFieldSymbol)member);

                if (member.Kind != SymbolKind.NamedType)
                    continue;

                INamedTypeSymbol namedTypeMember = (INamedTypeSymbol)member;

                if (namedTypeMember.TypeKind != TypeKind.Class)
                    continue;

                foreach (ISymbol nestedMember in namedTypeMember.GetMembers())
                {
                    if (nestedMember.Kind == SymbolKind.Field)
                        allFields.Add((IFieldSymbol)nestedMember);
                }
            }

            return allFields;
        }

        private bool HasNotOneCreation()
        {
            bool noOne = true;

            var classObjectCreations = TypeDeclaration.DescendantNodes()
               .Where(descendant => descendant.Kind() == SyntaxKind.ObjectCreationExpression)
               .Select(objCreation => (ObjectCreationExpressionSyntax)objCreation);

            foreach (ObjectCreationExpressionSyntax objectCreation in classObjectCreations)
            {
                var operation = SemanticModel.GetOperation(objectCreation);

                if (operation.Kind != OperationKind.ObjectCreation)
                    continue;

                var objectCreationOperation = (IObjectCreationOperation)operation;

                if (!objectCreationOperation.Type.Equals(TypeInfo)) // singleField.Type?
                    continue;

                if (!noOne)
                    return true;

                noOne = false;
            }

            return noOne;
        }

        private int AssignmentsCount()
        {
            int assignmentsCount = 0;

            var classAssignments = TypeDeclaration.DescendantNodes()
                .Where(descendant => descendant.Kind() == SyntaxKind.SimpleAssignmentExpression)
                .Select(assign => (AssignmentExpressionSyntax)assign);

            foreach (AssignmentExpressionSyntax classAssignment in classAssignments)
            {
                var assignment = SemanticModel.GetOperation(classAssignment);

                if (!(assignment is IAssignmentOperation))
                    continue;

                IAssignmentOperation classAssignmentInfo = (IAssignmentOperation)assignment;

                if (classAssignmentInfo.Target.Kind != OperationKind.FieldReference)
                    continue;

                var target = (IFieldReferenceOperation)classAssignmentInfo.Target;

                if (target.Member.Equals(singleField))
                    assignmentsCount++;
            }

            return assignmentsCount;
        }


        private bool HasSingletonMethods()
        {
            var methods = TypeDeclaration.Members
                .Where(member => member.Kind() == SyntaxKind.MethodDeclaration)
                .Select(methodDecl => (MethodDeclarationSyntax)methodDecl);

            return methods.Any(method => IsSingletonMethod(method));
        }


        private bool HasSingletonProperties()
        {
            var properties = TypeDeclaration.Members
                .Where(member => member.Kind() == SyntaxKind.PropertyDeclaration)
                .Select(propertyDecl => (PropertyDeclarationSyntax)propertyDecl);

            return properties.Any(property => IsSingletonProperty(property));
        }


        private bool IsSingletonMethod(MethodDeclarationSyntax analyzedMethod)
        {
            var methodBody = GetMethodOperation(analyzedMethod);

            if (methodBody == null)
                return false;

            var methodInfo = SemanticModel.GetDeclaredSymbol(analyzedMethod);

            if (!methodInfo.IsStatic ||
                methodInfo.IsAbstract ||
                methodInfo.DeclaredAccessibility == Accessibility.Private ||
                methodInfo.DeclaredAccessibility == Accessibility.Protected ||
                methodInfo.DeclaredAccessibility == Accessibility.ProtectedAndInternal)
            {
                return false;
            }

            return HasSingleFieldReturns(methodBody);
        }


        private bool IsSingletonProperty(PropertyDeclarationSyntax analyzedProperty)
        {
            var propertyInfo = SemanticModel.GetDeclaredSymbol(analyzedProperty);

            if (!propertyInfo.IsReadOnly ||
                !propertyInfo.IsStatic ||
                propertyInfo.DeclaredAccessibility == Accessibility.Private ||
                propertyInfo.DeclaredAccessibility == Accessibility.Protected ||
                propertyInfo.DeclaredAccessibility == Accessibility.ProtectedAndInternal)
            {
                return false;
            }

            IBlockOperation propertyBody;

            if (analyzedProperty.ExpressionBody != null) 
            {
                propertyBody = (IBlockOperation)SemanticModel.GetOperation(analyzedProperty.ExpressionBody); 
            }
            else
            {
                var accessor = analyzedProperty.AccessorList.Accessors.First();

                if (accessor.Body == null && accessor.ExpressionBody == null)
                    return singleField.AssociatedSymbol.Equals(propertyInfo);

                var propertyOperation = (IMethodBodyOperation)SemanticModel.GetOperation(accessor);
                propertyBody = propertyOperation.BlockBody ?? propertyOperation.ExpressionBody;
            }

            return HasSingleFieldReturns(propertyBody);
        }

        private bool HasSingleFieldReturns(IBlockOperation analyzedBody)
        {
            var returnStatements = analyzedBody.Descendants()
                .Where(operation => operation.Kind == OperationKind.Return)
                .Select(ret => (IReturnOperation)ret);

            return returnStatements.Any(retSt => IsSingleFieldReturn(retSt));
        }

        private bool IsSingleFieldReturn(IReturnOperation analyzedReturn)
        {
            var fieldReferences = analyzedReturn.Descendants()
                .Where(descendant => descendant.Kind == OperationKind.FieldReference)
                .Select(fieldReference => (IFieldReferenceOperation)fieldReference);

            return fieldReferences.Any(fieldRef => fieldRef.Field.Equals(singleField));
        }
    }
}