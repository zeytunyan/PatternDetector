using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;

namespace PatternDetector.Detectors
{
    class PrototypeDetector : AbstractPatternDetector
    {
        public PrototypeDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
           : base(typeDeclaration, semanticModel) { }

        public override bool Detect()
        {
            if (TypeInfo.TypeKind == TypeKind.Interface || TypeInfo.IsStatic)
                return false;
            
            INamedTypeSymbol iCloneableInterface = FindICloneableInterface();

            if (iCloneableInterface == null)
                return false;
            
            var interfaceMembers = iCloneableInterface.GetMembers();

            if (interfaceMembers.Count() == 0)
                return true; ////

            ISymbol interfaceCloneMethod = interfaceMembers.First();
            ISymbol typeCloneMethod = TypeInfo.FindImplementationForInterfaceMember(interfaceCloneMethod);

            if (typeCloneMethod == null)
                return true; ////

            IMethodSymbol cloneMethod = (IMethodSymbol)typeCloneMethod;

            if (!cloneMethod.IsAbstract && !cloneMethod.IsVirtual)
                return IsCloneMethod(cloneMethod);

            IMethodSymbol overridingMethod = FindOverriding(cloneMethod);
            
            if (overridingMethod == null || overridingMethod.IsAbstract)
                return true;

            return IsCloneMethod(overridingMethod);
        }

        private INamedTypeSymbol FindICloneableInterface()
        {
            var ICloneable = SemanticModel.Compilation.GetTypeByMetadataName("System.ICloneable");

            foreach (INamedTypeSymbol @interface in TypeInfo.AllInterfaces)
            {
                if (@interface.Equals(ICloneable))
                    return @interface;
            }

            return null;
        }

        private IMethodSymbol FindOverriding(IMethodSymbol searchMethod)
        {
            var allMethods = FindMethods(TypeInfo);

            foreach (IMethodSymbol method in allMethods)
            {
                if (method.Equals(searchMethod) || 
                    (method.IsOverride && method.OverriddenMethod.Equals(searchMethod)))
                {
                    return method;
                }     
            }

            return null;
        }

        private bool IsCloneMethod(IMethodSymbol analyzedMethod)
        {
            var methodDeclarationSyntax = GetMethodSyntaxFromSymbol(analyzedMethod);

            if (methodDeclarationSyntax == null)
                return true;

            var methodBody = GetMethodOperation(methodDeclarationSyntax);

            if (methodBody == null)
                return true;

            return HasCloneReturns(methodBody);
        }

        private bool HasCloneReturns(IBlockOperation blockOperation)
        {
            var returns = blockOperation.Descendants()
                .Where(operation => operation.Kind == OperationKind.Return)
                .Select(ret => (IReturnOperation)ret);

            return returns.Count() != 0 && returns.All(@return => IsCloneReturn(@return));
        }


        private bool IsCloneReturn(IReturnOperation returnOperation)
        {
            var returnedValue = returnOperation.ReturnedValue;

            if (returnedValue.Kind != OperationKind.Conversion)
                return IsNormalReference(returnedValue);

            var conversionOperation = (IConversionOperation)returnedValue;
            var conversionOperand = conversionOperation.Operand;

            return IsNormalReference(conversionOperand);
        }


        private bool IsNormalReference(IOperation reference)
        {
            if (reference.Syntax.Kind() == SyntaxKind.ThisExpression ||
                reference.Syntax.Kind() == SyntaxKind.ThisKeyword ||
                reference.Syntax.Kind() == SyntaxKind.NullLiteralExpression ||
                reference.Syntax.Kind() == SyntaxKind.NullKeyword )
            {
                return false;
            }

            return true;
        }
    }
}
