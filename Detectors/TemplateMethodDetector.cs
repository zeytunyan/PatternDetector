using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;

namespace PatternDetector.Detectors
{
    class TemplateMethodDetector : AbstractPatternDetector
    {
        public TemplateMethodDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
            : base(typeDeclaration, semanticModel) { }

        public override bool Detect()
        {
            if (TypeInfo.TypeKind == TypeKind.Interface || !TypeInfo.IsAbstract)
                return false;

            var methodDeclarations = TypeDeclaration.Members
                .Where(member => member.Kind() == SyntaxKind.MethodDeclaration)
                .Select(methodDecl => (MethodDeclarationSyntax)methodDecl);

            return methodDeclarations.Any(methodDeclaration => IsTemplateMethod(methodDeclaration));
        }


        private bool IsTemplateMethod(MethodDeclarationSyntax analyzedMethod)
        {
            var methodBody = GetMethodOperation(analyzedMethod);

            if (methodBody == null)
                return false;

            var methodInformation = SemanticModel.GetDeclaredSymbol(analyzedMethod);

            if (methodInformation.IsAbstract ||
                methodInformation.IsVirtual ||
                methodInformation.IsStatic ||
                methodInformation.DeclaredAccessibility == Accessibility.Private ||
                methodInformation.DeclaredAccessibility == Accessibility.NotApplicable ||
                methodBody.Operations.Length < 2)
            {
                return false;
            }

            return HasAbstractInvocations(methodBody);
        }


        private bool HasAbstractInvocations(IBlockOperation body)
        {
            var invocations = body.Descendants()
                .Where(descendant => descendant.Kind == OperationKind.Invocation)
                .Select(inv => (IInvocationOperation)inv);

            return invocations.Any(invocation => IsAbstractInvocation(invocation));
        }


        private bool IsAbstractInvocation(IInvocationOperation analyzedInvocation)
        {
            ISymbol invocationInformation = SemanticModel.GetSymbolInfo(analyzedInvocation.Syntax).Symbol;

            if (analyzedInvocation.Syntax.Kind() != SyntaxKind.InvocationExpression ||
                !invocationInformation.IsAbstract ||
                analyzedInvocation.Instance.Kind != OperationKind.InstanceReference)
            {
                return false;
            }

            IInstanceReferenceOperation invocationInstance = (IInstanceReferenceOperation)analyzedInvocation.Instance;

            return invocationInstance.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;
        }
    }
}
