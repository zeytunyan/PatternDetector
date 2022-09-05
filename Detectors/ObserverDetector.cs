using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternDetector.Detectors
{
    class ObserverDetector : PatternDetector
    {
        private IEnumerable<ISymbol> members;
        private IEnumerable<IMethodSymbol> methods;
        private IEnumerable<IEventSymbol> events;
        private IEnumerable<IFieldSymbol> genericFields;
        private IEnumerable<IPropertySymbol> genericProperties;

        public ObserverDetector(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
            : base(typeDeclaration, semanticModel)
        {
            members = LookupMembers(TypeInfo);
            methods = FindNormalMethods();
            events = FindNormalEvents();
            genericFields = FindGenericFields();
            genericProperties = FindGenericProperties();
        }

        public override bool Detect()
        {
            if (TypeInfo.TypeKind == TypeKind.Interface || TypeInfo.IsStatic)
                return false;

            return IsInterfaceObservable() || IsEventObservable() || IsOrdinaryObservable();
        }


        private bool IsInterfaceObservable()
        {
            var interfaces = TypeInfo.AllInterfaces;
            Compilation compilation = SemanticModel.Compilation;
            INamedTypeSymbol IObservable = compilation.GetTypeByMetadataName("System.IObservable`1");

            return interfaces.Any(intF => intF.Equals(IObservable) || @intF.ConstructedFrom.Equals(IObservable));
        }


        private bool IsEventObservable()
        {
            if (events.Count() == 0)
                return false;

            return methods.Any(method => HasEventRaise(method));
        }

        private bool HasEventRaise(IMethodSymbol analyzedMethod)
        {
            var methodOperations = GetMethodDescendants(analyzedMethod);

            if (methodOperations == null)
                return false;

            var methodInvocations = methodOperations
                .Where(op => op.Kind == OperationKind.Invocation)
                .Select(inv => (IInvocationOperation)inv);

            return methodInvocations.Any(invocation => IsEventInvocation(invocation));
        }

        private bool IsEventInvocation(IInvocationOperation analyzedInvocation)
        {
            IOperation invocationInstance = analyzedInvocation.Instance;

            if (invocationInstance == null)
                return false;

            if (invocationInstance.Kind == OperationKind.ConditionalAccessInstance)
            {
                var invocationParent = analyzedInvocation.Parent;

                if (invocationParent.Kind != OperationKind.ConditionalAccess)
                    return false;

                var parentConditionalAccess = (IConditionalAccessOperation)invocationParent;
                invocationInstance = parentConditionalAccess.Operation;
            }

            if (invocationInstance.Kind != OperationKind.EventReference)
                return false;

            var eventReference = (IEventReferenceOperation)invocationInstance;

            return events.Any(@event => @event.Equals(eventReference.Event)) && IsInvoke(analyzedInvocation);
        }

        private bool IsInvoke(IInvocationOperation analyzedInvocation)
        {
            var targetMethod = analyzedInvocation.TargetMethod;

            if (targetMethod.MethodKind == MethodKind.DelegateInvoke ||
                targetMethod.Name == "Invoke" ||
                targetMethod.Name == "DynamicInvoke" ||
                targetMethod.Name == "BeginInvoke")
            {
                return true;
            }

            return false;
        }


        private bool IsOrdinaryObservable()
        {
            if (genericFields.Count() == 0 && genericProperties.Count() == 0)
                return false;

            return methods.Any(method => HasCollectionLoop(method));
        }

        private bool HasCollectionLoop(IMethodSymbol analyzedMethod)
        {
            var methodOperations = GetMethodDescendants(analyzedMethod);

            if (methodOperations == null)
                return false;

            var methodLoops = methodOperations
                .Where(op => op.Kind == OperationKind.Loop)
                .Select(loop => (ILoopOperation)loop)
                .Where(loop => loop.LoopKind == LoopKind.ForEach)
                .Select(loop => (IForEachLoopOperation)loop);

            return methodLoops.Any(loop => IsCollectionLoop(loop));
        }

        private bool IsCollectionLoop(IForEachLoopOperation analyzedLoop)
        {
            if (analyzedLoop.Body == null || !IsPropertyOrFieldLoopCollection(analyzedLoop.Collection))
                return false;

            ILocalSymbol loopLocal = analyzedLoop.Locals.First();

            var loopInvocations = analyzedLoop.Body.Descendants()
                .Where(desc => desc.Kind == OperationKind.Invocation)
                .Select(inv => (IInvocationOperation)inv);

            return loopInvocations.Any(invocation => IsLoopLocalInvocation(invocation, loopLocal));
        }

        private bool IsPropertyOrFieldLoopCollection(IOperation collection)
        {
            if (collection == null)
                return false;

            if (collection.Kind == OperationKind.Conversion)
            {
                var conversion = (IConversionOperation)collection;
                collection = conversion.Operand;
            }

            var kind = collection.Kind;

            if (kind != OperationKind.FieldReference && kind != OperationKind.PropertyReference)
                return false;

            var memberReference = (IMemberReferenceOperation)collection;
            var member = memberReference.Member;

            return genericFields.Any(fld => fld.Equals(member)) || genericProperties.Any(prprt => prprt.Equals(member));
        }

        private bool IsLoopLocalInvocation(IInvocationOperation invocation, ILocalSymbol loopLocal)
        {
            var targetMethod = invocation.TargetMethod;

            if (!targetMethod.ReturnsVoid || targetMethod.ReturnsByRef || targetMethod.ReturnsByRefReadonly)
                return false;

            var instance = invocation.Instance;

            if (instance == null || instance.Kind != OperationKind.LocalReference)
                return false;

            var operationLocal = (ILocalReferenceOperation)instance;

            return loopLocal.Equals(operationLocal.Local);
        }

       
        private IEnumerable<ISymbol> LookupMembers(INamedTypeSymbol symbol)
        {
            List<ISymbol> results = new List<ISymbol>();
            var syntaxReferences = symbol.DeclaringSyntaxReferences;

            foreach (SyntaxReference reference in syntaxReferences)
            {
                int syntaxEnd = reference.Span.End - 2;
                ImmutableArray<ISymbol> foundSymbols;

                try
                {
                    foundSymbols = SemanticModel.LookupSymbols(syntaxEnd, symbol);
                }
                catch
                {
                    continue;
                }

                foreach (ISymbol foundSymbol in foundSymbols)
                    results.Add(foundSymbol);
            }

            return results;
        }

        private IEnumerable<IEventSymbol> FindNormalEvents()
        {
            return members.Where(mbr => mbr.Kind == SymbolKind.Event)
                .Select(@event => (IEventSymbol)@event)
                .Where(@event => !@event.IsAbstract);
        }

        private IEnumerable<IMethodSymbol> FindNormalMethods()
        {
            return members.Where(mbr => mbr.Kind == SymbolKind.Method)
                .Select(method => (IMethodSymbol)method)
                .Where(method => !method.IsAbstract && !method.IsStatic);
        }

        private IEnumerable<IFieldSymbol> FindGenericFields()
        {
            List<IFieldSymbol> resultFields = new List<IFieldSymbol>();

            var allFields = members.Where(mbr => mbr.Kind == SymbolKind.Field)
                   .Select(field => (IFieldSymbol)field);

            foreach (IFieldSymbol field in allFields)
            {
                var fieldType = field.Type;

                if (field.IsStatic || !IsEnumerable(fieldType))
                    continue;

                if (IsAllowGenericType(fieldType))
                    resultFields.Add(field);
            }

            return resultFields;
        }

        private IEnumerable<IPropertySymbol> FindGenericProperties()
        {
            List<IPropertySymbol> resultProperties = new List<IPropertySymbol>();

            var allProperties= members.Where(mbr => mbr.Kind == SymbolKind.Property)
                   .Select(prop => (IPropertySymbol)prop);

            foreach (IPropertySymbol property in allProperties)
            {
                var propertyType = property.Type;

                if (property.IsStatic ||
                    property.IsWriteOnly ||
                    property.IsIndexer ||
                    !IsEnumerable(propertyType))
                {
                    continue;
                }

                if (IsAllowGenericType(propertyType))
                    resultProperties.Add(property);
            }

            return resultProperties;
        }

        private bool IsAllowGenericType(ITypeSymbol type)
        {
            var typeKind = type.TypeKind;

            if (typeKind == TypeKind.Array)
            {
                var arrayType = (IArrayTypeSymbol)type;
                return !IsEnumerable(arrayType.ElementType);
            }

            if (typeKind != TypeKind.Interface && typeKind != TypeKind.Class && typeKind != TypeKind.Struct)
                return false;

            INamedTypeSymbol named = (INamedTypeSymbol)type;

            if (named.Arity != 1)
                return false;

            var namedTypeParameter = named.TypeArguments.First();

            return !IsEnumerable(namedTypeParameter);
        }


        private IEnumerable<IOperation> GetMethodDescendants(IMethodSymbol searchMethod)
        {
            var methodSyntax = GetMethodSyntaxFromSymbol(searchMethod);

            if (methodSyntax == null)
                return null;

            var methodBody = GetMethodOperation(methodSyntax);

            if (methodBody == null)
                return null;

            var methodOperations = methodBody.Descendants();

            if (methodOperations.Count() == 0)
                return null;

            return methodOperations;
        }
    }
}
