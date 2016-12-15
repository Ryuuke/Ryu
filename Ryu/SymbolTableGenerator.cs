using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryu
{
    public class SymbolTableGenerator : AbstractVisitor
    {
        int _currentNodePosition;
        int _scopeIdGen;
        SymbolTable _symTable;
        ScopeInfo _currentScope;

        public List<string> GlobalTypes { get; private set; }
        public List<string> GlobalIdentifiers { get; private set; }
        public List<IdentExpr> IdentifiersToBeInferred { get; private set; }
        

        public SymbolTable GenerateSymTable(RootScopeAST rootAST, string fileName)
        {
            _symTable = new SymbolTable();
            _symTable.FilePath = fileName;
            _symTable.FileDependencies = rootAST.FileDependencies;

            _symTable.IdentInfoDictionary =
                new Dictionary<IdentifierLocation, IdentifierInfo>();

            _symTable.TypeInfoDictionary =
                new Dictionary<string, CustomTypeInfo>();

            _symTable.ScopeInfoDictionary = new Dictionary<int, ScopeInfo>();

            GlobalIdentifiers = new List<string>();
            GlobalTypes = new List<string>();
            IdentifiersToBeInferred = new List<IdentExpr>();

            rootAST.Accept(this);

            _currentNodePosition = 0;
            _currentScope = null;
            _scopeIdGen = 0;

            return _symTable;
        }

        public override void Visit(RootScopeAST rootScope)
        {
            _currentScope = new ScopeInfo
            {
                id = _scopeIdGen,
                parent = null
            };

            _symTable.ScopeInfoDictionary.Add(_currentScope.id, _currentScope);

            foreach (var element in rootScope.elements)
            {
                _currentNodePosition++;
                element.Accept(this);
            }
        }

        public override void Visit(ScopeAST scope)
        {
            _currentScope = new ScopeInfo
            {
                id = ++_scopeIdGen,
                parent = _currentScope
            };

            _symTable.ScopeInfoDictionary.Add(_currentScope.id, _currentScope);

            foreach (var element in scope.elements)
            {
                _currentNodePosition++;
                element.Accept(this);
            }

            _currentScope = _currentScope.parent;
        }

        public override void Visit(VariableDecAST variableDec)
        {
            var identInfo = new IdentifierInfo
            {
                name = variableDec.Name,
                typeAST = variableDec.Type,
                position = _currentNodePosition,
                scopeId = _currentScope.id,
                isFunctionType = variableDec.Type is FunctionTypeAST
            };

            AddIdentInfoToSymTable(identInfo);

            if (_currentScope.id != 0)
                return;

            GlobalIdentifiers.Add(variableDec.Name);
        }

        public override void Visit(DeclareAST declareAST)
        {
            var identInfo = new IdentifierInfo
            {
                name = declareAST.VariableDec.Name,
                typeAST = declareAST.VariableDec.Type,
                position = _currentNodePosition,
                scopeId = _currentScope.id,
                isFunctionType = declareAST.VariableDec.Type is FunctionTypeAST
            };

            AddIdentInfoToSymTable(identInfo);

            if (_currentScope.id != 0)
                return;

            GlobalIdentifiers.Add(declareAST.VariableDec.Name);
        }

        public override void Visit(VariableDecAssignAST variableDecAssign)
        {
            var identInfo = new IdentifierInfo
            {
                name = variableDecAssign.Name,
                typeAST = variableDecAssign.Type,
                position = _currentNodePosition,
                scopeId = _currentScope.id,
                isFunctionType = variableDecAssign.Type is FunctionTypeAST
            };

            AddIdentInfoToSymTable(identInfo);

            if (variableDecAssign.Type != null)
                return;

            IdentifiersToBeInferred.Add(new IdentExpr
            {
                identInfo = identInfo,
                expr = variableDecAssign.ExpressionValue as BaseExprAST,
                file = _symTable.FilePath
            });

            if (_currentScope.id != 0)
                return;

            GlobalIdentifiers.Add(variableDecAssign.Name);
        }

        private void AddIdentInfoToSymTable(IdentifierInfo identInfo)
        {
            var identLocation = new IdentifierLocation
            {
                identifierName = identInfo.name,
                scopeId = identInfo.scopeId
            };

            if (_symTable.IdentInfoDictionary.ContainsKey(identLocation))
            {
                throw new Exception(string.Format("Identifier already declared {0} in file {1} line {2}", 
                    identInfo.name, _symTable.FilePath, identInfo.scopeId));
            }

            _symTable.IdentInfoDictionary.Add(identLocation, identInfo);
        }

        public override void Visit(FunctionProtoAST functionProto)
        {
            var functionType = functionProto.GetFunctionType();

            var identInfo = new IdentifierInfo
            {
                name = functionProto.Name,
                position = _currentNodePosition,
                typeAST = functionType,
                scopeId = _currentScope.id,
                isFunctionType = true
            };

            var identLocation = new IdentifierLocation
            {
                identifierName = identInfo.name,
                scopeId = identInfo.scopeId
            };

            if (_symTable.IdentInfoDictionary.ContainsKey(identLocation))
            {
                throw new Exception(string.Format("Identifier {0} already declared in file {1} line {2}",
                    identInfo.name, _symTable.FilePath, _currentNodePosition));
            }

            _symTable.IdentInfoDictionary.Add(identLocation, identInfo);

            if (_currentScope.id == 0)
            {
                GlobalIdentifiers.Add(functionProto.Name);
            }

            if (functionProto.Args == null)
                return;

            for (var i = 0; i < functionProto.Args.Count; i++)
            {
                var functionArgument = functionProto.Args[i];

                identInfo = new IdentifierInfo
                {
                    name = functionArgument.Name,
                    typeAST = functionArgument.Type,
                    position = _currentNodePosition,
                    scopeId = _scopeIdGen + 1,
                    isFunctionType = functionArgument.Type is FunctionTypeAST,
                    isFnParam = true,
                    paramIndex = i
                };

                AddIdentInfoToSymTable(identInfo);
            }
        }

        public override void Visit(FunctionBodyAST functionBody)
        {
            Visit(functionBody.Prototype);
            Visit(functionBody.Scope);
        }

        public override void Visit(StructAST structAST)
        {
            var typeInfo = new CustomTypeInfo
            {
                type = structAST,
                kind = TypeKind.STRUCT,
                position = _currentNodePosition,
                scopeId = _currentScope.id,
                memberNameType = new Dictionary<string, TypeAST>()
            };

            foreach (var member in structAST.Variables)
            {
                var variableDec = member as VariableDecAST;

                typeInfo.memberNameType.Add(variableDec.Name, variableDec.Type);
            }

            AddType(typeInfo);

            if (_currentScope.id != 0)
                return;

            GlobalTypes.Add(structAST.Name);
        }

        public override void Visit(EnumAST enumAST)
        {
            var typeInfo = new CustomTypeInfo
            {
                type = enumAST,
                kind = TypeKind.ENUM,
                scopeId = _currentScope.id,
                position = _currentNodePosition,
                memberNameType = new Dictionary<string, TypeAST>()
            };

            foreach (var member in enumAST.Values)
            {
                var variableDec = member as VariableDecAST;

                typeInfo.memberNameType.Add(variableDec.Name, variableDec.Type);
            }

            AddType(typeInfo);

            if (_currentScope.id != 0)
                return;

            GlobalTypes.Add(enumAST.Name);
        }

        private void AddType(CustomTypeInfo typeInfo)
        {
            /* ATM all types are declared in the global scope so they must have different names */
            if (_symTable.TypeInfoDictionary.ContainsKey(typeInfo.type.ToString()))
            {
                throw new Exception(string.Format("Identifier already declared {0} in file {1} line {2}",
                    typeInfo.type.ToString(), _symTable.FilePath, _currentNodePosition));
            }

            _symTable.TypeInfoDictionary.Add(typeInfo.type.ToString(), typeInfo);
        }

        public override void Visit(ConstantVariable constantVariable)
        {
            var identInfo = new IdentifierInfo
            {
                name = constantVariable.VariableName,
                typeAST = null,
                position = _currentNodePosition,
                scopeId = _currentScope.id,
                isConstant = true
            };

            AddIdentInfoToSymTable(identInfo);

            IdentifiersToBeInferred.Add(new IdentExpr
            {
                identInfo = identInfo,
                expr = constantVariable.ExpressionValue,
                file = _symTable.FilePath
            });

            if (_currentScope.id != 0)
                return;

            GlobalIdentifiers.Add(constantVariable.VariableName);
        }
    }
}