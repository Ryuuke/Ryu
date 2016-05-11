using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryu
{
    public class TypeChecker : AbstractVisitor
    {
        string _currentFileName;
        int _scopeIdGen;
        int _currentNodePosition;
        int _currentScopeId;

        SymbolTableManager _symTableManager;
        Dictionary<string, RootScopeAST> _programAST;
        ExprTypeVisitor _exprTypeVisitor;
        TypeAST _returnType;
        string _nullType = Enum.GetName(typeof(Keyword), Keyword.NULL).ToLower();


        public TypeChecker(SymbolTableManager symbolTableManager)
        {
            _exprTypeVisitor = new ExprTypeVisitor(symbolTableManager);

            _symTableManager = symbolTableManager;
            _programAST = symbolTableManager.ProgramAST;
        }

        public void TypeCheck()
        {
            foreach (var entry in _programAST)
            {
                _currentFileName = entry.Key;
                _scopeIdGen = 0;
                _currentScopeId = 0;
                _currentScopeId = 0;

                entry.Value.Accept(this);
            }
        }

        public override void Visit(RootScopeAST rootScope)
        {
            _currentScopeId = _scopeIdGen;

            foreach (var element in rootScope.elements)
            {
                _currentNodePosition++;
                element.Accept(this);
            }
        }

        public override void Visit(ScopeAST scope)
        {
            var parentScopeId = _currentScopeId;
            _currentScopeId = ++_scopeIdGen;

            foreach (var element in scope.elements)
            {
                _currentNodePosition++;
                element.Accept(this);
            }

            _currentScopeId = parentScopeId;
        }

        public override void Visit(VariableDecAssignAST variableDecAssign)
        {
            if (variableDecAssign.Type == null)
                return;

            var identInfo = _symTableManager.LookupIdentifierInfo(_currentFileName, variableDecAssign.Name,
                _currentScopeId, _currentNodePosition);

            var exprType = _exprTypeVisitor.GetExprType(_currentFileName, identInfo, variableDecAssign.ExpressionValue);

            if (variableDecAssign.Type.ToString() != exprType.ToString() && 
                exprType.ToString() != _nullType)
            {
                throw new Exception(string.Format("Type mismatch : variable '{0}' have type '{1}' but assigned '{2}' type", 
                    variableDecAssign.Name, variableDecAssign.Type, exprType));
            }
        }

        public override void Visit(FunctionProtoAST functionProto)
        {
            _returnType = functionProto.ReturnType;
        }

        public override void Visit(FunctionBodyAST functionBody)
        {
            Visit(functionBody.Prototype);
            Visit(functionBody.Scope);
            _returnType = null;
        }

        public override void Visit(FunctionCallAST functionCall)
        {
            _exprTypeVisitor.GetASTType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                functionCall);
        }

        public override void Visit(StructMemberAssignAST structMemberAssign)
        {
            var structMemberType = _exprTypeVisitor.GetASTType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                structMemberAssign.StructMember);

            var assignmentExprType = _exprTypeVisitor.GetASTType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                structMemberAssign.AssignExpr);

            if (structMemberType.ToString() != assignmentExprType.ToString() && 
                assignmentExprType.ToString() != _nullType)
                throw new Exception(string.Format("Type mismatch : struct member '{0}' have type '{1}' but assigned '{2}' type",
                    structMemberAssign.StructMember, structMemberType, assignmentExprType));
        }

        public override void Visit(VariableAssignAST variableAssign)
        {
            var identInfo = _symTableManager.LookupIdentifierInfo(_currentFileName, variableAssign.VariableName,
                _currentScopeId, _currentNodePosition);

            var exprType = _exprTypeVisitor.GetExprType(_currentFileName, identInfo, variableAssign.ExpressionValue);

            if (identInfo.typeAST.ToString() != exprType.ToString() && exprType.ToString() != _nullType)
            {
                throw new Exception(string.Format("Type mismatch : variable '{0}' have type '{1}' but assigned '{2}' type",
                    variableAssign.VariableName, identInfo.typeAST.ToString(), exprType));
            }
        }

        public override void Visit(ArrayAccessAST arrayAccess)
        {
            foreach (var expr in arrayAccess.AccessExprList)
            {
                var accessExprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                expr);

                if (!Vocabulary.Ints.Contains(accessExprType.ToString()))
                    throw new Exception(string.Format("Type mismatch : array {0} accessor must be an integer, but '{1}' type found", 
                        arrayAccess.ArrayVariableName.ToString(), accessExprType.ToString()));
            }
        }

        public override void Visit(ArrayAcessAssignAST arrayAccessAssign)
        {
            arrayAccessAssign.ArrayAcess.Accept(this);

            var arrayContainedType = _exprTypeVisitor.GetASTType(_currentFileName, 
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                arrayAccessAssign.ArrayAcess);

            var arrayAssignmentType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                arrayAccessAssign.AssignmentExpr);

            if (arrayContainedType.ToString() != arrayAssignmentType.ToString() && 
                arrayAssignmentType.ToString() != _nullType)
                throw new Exception(string.Format("Type mismatch : variable '{0}' have type '{1}' but assigned '{2}' type",
                    arrayAccessAssign.ArrayAcess.ArrayVariableName.ToString(), 
                    arrayContainedType.ToString(), arrayAssignmentType.ToString()));
        }

        public override void Visit(IfAST ifStatement)
        {
            var exprType = _exprTypeVisitor.GetExprType(_currentFileName, 
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition }, 
                ifStatement.ConditionExpr);

            if (exprType.ToString() != Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower())
            {
                throw new Exception(string.Format("Type mismatch : Condition must be of type 'bool', but it's a '{0}' ...",
                    exprType.ToString()));
            }
        }

        public override void Visit(ForAST forStatement)
        {
            var fromExprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                forStatement.FromExpr);

            var toExprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                forStatement.ToExpr);

            if (!Vocabulary.Ints.Contains(fromExprType.ToString()) || 
                !Vocabulary.Ints.Contains(toExprType.ToString()))
            {
                throw new Exception(string.Format("Type mismatch : For statement expression must be an integer type",
                    fromExprType.ToString()));
            }
        }

        public override void Visit(ForeachAST foreachStatement)
        {
            var arrayExprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                foreachStatement.ArrayExpr);

            if (!(arrayExprType is ArrayTypeAST))
            {
                throw new Exception(string.Format("Type mismatch : Foreach statement expression must be an array type",
                    arrayExprType.ToString()));
            }
        }

        public override void Visit(WhileAST whileStatement)
        {
            var exprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                whileStatement.ConditionExpr);

            if (exprType.ToString() != Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower())
            {
                throw new Exception(string.Format("Type mismatch : Condition must be of type 'bool', but it's a '{0}' ...",
                    exprType.ToString()));
            }
        }

        public override void Visit(DoWhileAST doWhileStatement)
        {
            var exprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                doWhileStatement.ConditionExpr);

            if (exprType.ToString() != Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower())
            {
                throw new Exception(string.Format("Type mismatch : Condition must be of type 'bool', but it's a '{0}' ...",
                    exprType.ToString()));
            }
        }

        public override void Visit(ReturnAST returnStatement)
        {
            if (_returnType.ToString() == Enum.GetName(typeof(Keyword), Keyword.VOID).ToLower() && returnStatement.ReturnExpr != null)
                throw new Exception("Return statement must not be followed by expression because the function has no return type");

            var returnExprType = _exprTypeVisitor.GetExprType(_currentFileName,
                new IdentifierInfo { scopeId = _currentScopeId, position = _currentNodePosition },
                returnStatement.ReturnExpr);

            var x = _returnType.GetType().IsAssignableFrom(returnExprType.GetType());

            if (_returnType.ToString() != returnExprType.ToString() && !x && 
                returnExprType.ToString() != _nullType)
                throw new Exception(string.Format("Return type must be of type '{0}', found type '{1}'",
                    _returnType, returnExprType.ToString()));
        }

        public override void Visit(DeleteAST deleteStatement)
        {
            var variableNameExists = _symTableManager.LookupIdentifierInfo(_currentFileName, 
                deleteStatement.VariableName.Name, _currentScopeId, _currentNodePosition);

            if (variableNameExists == null)
                throw new Exception("Undefined variable " + deleteStatement.VariableName.Name);
        }

        public override void Visit(DeferAST deferStatement) { }
    }
}