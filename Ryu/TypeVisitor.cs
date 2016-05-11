﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryu
{
    public class ExprTypeVisitor : AbstractVisitor
    {
        public struct StateInfo
        {
            public string currentFile;
            public IdentifierInfo identInfo;
            public TypeAST currentType;
            public CustomTypeInfo structOrEnum;
        }

        SymbolTableManager _symTableManager;
        StateInfo _stateInfo;
        Func<IdentifierInfo, TypeAST> _getVariableTypeFunc;

        public ExprTypeVisitor(SymbolTableManager symTableManager, Func<IdentifierInfo, TypeAST> getVariableTypeFunc = null)
        {
            _symTableManager = symTableManager;
            _getVariableTypeFunc = getVariableTypeFunc;
        }

        public TypeAST GetExprType(string file, IdentifierInfo identInfo, BaseExprAST expression)
        {
            _stateInfo = new StateInfo { currentFile = file, identInfo = identInfo, currentType = null };

            expression.Accept(this);

            return _stateInfo.currentType;
        }

        public TypeAST GetASTType(string file, IdentifierInfo identInfo, ASTNode ast)
        {
            _stateInfo = new StateInfo { currentFile = file, identInfo = identInfo, currentType = null };

            ast.Accept(this);

            return _stateInfo.currentType;
        }

        public override void Visit(NumberAST number)
        {
            var numberType = new TypeAST
            {
                TypeName = number.ExplicitType != string.Empty ?
                   number.ExplicitType :
                   Enum.GetName(typeof(Keyword), Keyword.S32).ToLower()
            };

            _stateInfo.currentType = numberType;
        }

        public override void Visit(FloatAST floatNumber)
        {
            _stateInfo.currentType = new TypeAST
            {
                TypeName = floatNumber.ExplicitType != string.Empty ?
                            floatNumber.ExplicitType :
                   Enum.GetName(typeof(Keyword), Keyword.F32).ToLower()
            };
        }

        public override void Visit(OperatorAST op)
        {
            op.Lhs.Accept(this);

            var lhsType = _stateInfo.currentType;

            op.Rhs.Accept(this);

            var rhsType = _stateInfo.currentType;

            if (IsLogicalOperator(op.OperatorString))
            {
                _stateInfo.currentType = new TypeAST { TypeName = Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower() };
                return;
            }

            _stateInfo.currentType = new TypeAST { TypeName = GetTypeOf(lhsType, rhsType) };
        }

        public override void Visit(ConstantKeywordAST constantKeyword)
        {
            if (constantKeyword.keyword == Enum.GetName(typeof(Keyword), Keyword.NULL).ToLower())
            {
                _stateInfo.currentType = new TypeAST { TypeName = Enum.GetName(typeof(Keyword), Keyword.NULL).ToLower() };

                return;
            }

            _stateInfo.currentType = new TypeAST { TypeName = Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower() };
        }

        public override void Visit(FunctionCallAST functionCall)
        {
            _stateInfo.currentType = GetReturnType(functionCall);
        }

        public override void Visit(StructMemberCallAST structCall)
        {
            for (var i = 0; i < structCall.variableNames.Count; i++)
            {
                var ast = structCall.variableNames[i];

                ast.Accept(this);

                if (i != structCall.variableNames.Count - 1)
                {
                    var structTypeInfo = _symTableManager.
                            LookupTypeInfo(_stateInfo.currentFile, _stateInfo.currentType.ToString());

                    if (structTypeInfo == null)
                        throw new Exception("Invalid struct type " + _stateInfo.currentType.ToString());

                    _stateInfo.structOrEnum = structTypeInfo;
                }
            }

            _stateInfo.structOrEnum = null;
        }

        public override void Visit(UnaryOperator unaryOperator)
        {
            if (unaryOperator.Operator == Enum.GetName(typeof(Symbol), Symbol.EXCLAMATION))
            {
                unaryOperator.term.Accept(this);

                var boolType = _stateInfo.currentType;

                if (boolType.TypeName != Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower())
                    throw new Exception("Cannot Apply negation operator to " + boolType.ToString() + " type");
            }
            else
            {
                unaryOperator.term.Accept(this);
            }
        }

        public override void Visit(NewExprAST newStatement)
        {
            TypeAST type = newStatement.Type;

            ArrayTypeAST arrayType;

            while((arrayType = type as ArrayTypeAST) != null)
            {
                type = arrayType.TypeOfContainedValues;
            }

            if (!(type is FunctionTypeAST))
            {
                var typeExists = _symTableManager.LookupTypeInfo(_stateInfo.currentFile, type.ToString());

                if (typeExists == null)
                    throw new Exception(string.Format("New statement : struct type : {0}", type.ToString()));
            }

            _stateInfo.currentType = newStatement.Type;
        }

        public override void Visit(ExprAST expr)
        {
            expr.expr.Accept(this);
        }

        public override void Visit(ArrayAccessAST arrayAccess)
        {
            var arrayType = GetArrayType(arrayAccess);

            var accessDimensions = arrayAccess.AccessExprList.Count;

            _stateInfo.currentType = GetArrayContainedType(arrayType, accessDimensions);
        }

        public override void Visit(VariableNameAST variableName)
        {
            if (_stateInfo.structOrEnum != null)
            {
                _stateInfo.currentType = _stateInfo.structOrEnum.memberNameType[variableName.Name];
                return;
            }

            _stateInfo.currentType = GetVariableType(variableName);
        }

        public override void Visit(StringAST stringConstant)
        {
            _stateInfo.currentType = new TypeAST { TypeName = Enum.GetName(typeof(Keyword), Keyword.STR).ToLower() };
        }

        public override void Visit(HexNumberAST hexNumber)
        {
            _stateInfo.currentType = new TypeAST
            {
                TypeName = hexNumber.ExplicitType != string.Empty ?
                    hexNumber.ExplicitType :
                    Enum.GetName(typeof(Keyword), Keyword.S32).ToLower()
            };
        }

        private bool IsLogicalOperator(string operatorString)
        {
            var logicalOperators = new string[] { "==", ">=", ">", "<", "<=", "!=" };

            return logicalOperators.Any(x => x == operatorString);
        }

        private string ComputeType(string currentTypeString, string otherTypeString)
        {
            if (Vocabulary.Types.All(x => x != currentTypeString) ||
                Vocabulary.Types.All(x => x != otherTypeString))
                throw new Exception("Cannot apply operation on types " + currentTypeString + " and " + otherTypeString);

            if (currentTypeString == otherTypeString)
                return otherTypeString;

            if (currentTypeString == "str" || otherTypeString == "str")
                return "str";

            if ((currentTypeString == "s64" || currentTypeString.StartsWith("s")) &&
                (otherTypeString == "s64" || otherTypeString.StartsWith("s")))
                return "s64";

            if (currentTypeString.StartsWith("s") && otherTypeString.StartsWith("s"))
                return "s32";

            if ((currentTypeString == "u64" || currentTypeString.StartsWith("s")) &&
               (otherTypeString == "u64" || otherTypeString.StartsWith("s")))
                return "u64";

            if ((currentTypeString == "u64" || currentTypeString.StartsWith("u")) &&
               (otherTypeString == "u64" || otherTypeString.StartsWith("u")))
                return "u64";

            if (currentTypeString.StartsWith("u") && otherTypeString.StartsWith("u"))
                return "u32";

            if ((currentTypeString == "u64" || currentTypeString.StartsWith("f")) &&
                (otherTypeString == "u64" || otherTypeString.StartsWith("f")))
                return "f64";

            if ((currentTypeString == "s64" || currentTypeString.StartsWith("f")) &&
                (otherTypeString == "s64" || otherTypeString.StartsWith("f")))
                return "f64";

            if (currentTypeString == "f64" || otherTypeString == "f64")
                return "f64";

            if ((currentTypeString.StartsWith("s") || currentTypeString.StartsWith("f")) &&
                (otherTypeString.StartsWith("s") || otherTypeString.StartsWith("f")))
                return "f32";

            if ((currentTypeString.StartsWith("u") || currentTypeString.StartsWith("f")) &&
                (otherTypeString.StartsWith("u") || otherTypeString.StartsWith("f")))
                return "f32";

            throw new Exception("Cannot apply operation on types " + currentTypeString + " and " + otherTypeString);
        }

        private string GetTypeOf(TypeAST currentType, TypeAST otherType)
        {
            if (currentType == null)
                return otherType.ToString();

            return ComputeType(currentType.ToString(), otherType.ToString());
        }

        private string GetTypeOf(TypeAST currentType, string otherType)
        {
            if (currentType == null)
                return otherType.ToString();

            return ComputeType(currentType.ToString(), otherType);
        }

        private TypeAST GetVariableType(VariableNameAST variableName, bool isConstant = false)
        {
            var variableInfo = _symTableManager.
                                    LookupIdentifierInfo(_stateInfo.currentFile, variableName.Name,
                                    _stateInfo.identInfo.scopeId, _stateInfo.identInfo.position, isConstant);

            if (variableInfo == null)
                throw new Exception(string.Format("Undeclared {0} identifier {1} in file {2}",
                    isConstant ? "constant" : "", variableName.Name, _stateInfo.currentFile));

            if (variableInfo.typeAST == null)
            {
                Debug.Assert(_getVariableTypeFunc != null);

                var previousStateInfo = _stateInfo;

                variableInfo.typeAST = _getVariableTypeFunc(variableInfo);

                 _stateInfo = previousStateInfo;
            }

            _stateInfo.identInfo.isFunctionType = variableInfo.isFunctionType;

            return variableInfo.typeAST;
        }

        public TypeAST GetReturnType(FunctionCallAST functionCallAst)
        {
            List<TypeAST> argsType = null;

            if (functionCallAst.ExpressionList.Count != 0)
            {
                argsType = new List<TypeAST>();

                foreach (var expression in functionCallAst.ExpressionList)
                {
                    expression.Accept(this);

                    argsType.Add(_stateInfo.currentType);
                }
            }

            FunctionTypeAST functionType;

            var arrayFunctionCall = functionCallAst.Name as ArrayAccessAST;

            if (arrayFunctionCall != null)
            {
                var arrayType = GetArrayType(arrayFunctionCall);

                var accessDimensions = arrayFunctionCall.AccessExprList.Count;

                functionType = GetArrayContainedType(arrayType, accessDimensions) as FunctionTypeAST;

                string invalidArgsMessage = string.Format("Invalid function arguments expected '{0}' got '{1}'",
                        string.Join(",", functionType.ArgumentTypes), argsType == null ? "no argument" : string.Join(",", argsType));

                if (argsType != null && functionType.ArgumentTypes.Count != argsType.Count)
                    throw new Exception(invalidArgsMessage);

                if (argsType == null && functionType.ArgumentTypes.Count != 0)
                    throw new Exception(invalidArgsMessage);

                for (var i = 0; i < functionType.ArgumentTypes.Count; i++)
                {
                    if (functionType.ArgumentTypes[i].ToString() != argsType[i].ToString())
                    {
                        throw new Exception(invalidArgsMessage);
                    }
                }
            }
            else
            {
                var functionCallInfo = _symTableManager.
                        LookupFunctionInfo(_stateInfo.currentFile, functionCallAst.Name.ToString(),
                        _stateInfo.identInfo.scopeId, argsType);

                if (functionCallInfo == null)
                    throw new Exception("Undefined function " + functionCallAst.Name);

                functionType = functionCallInfo.typeAST as FunctionTypeAST;
            }

            if (functionType.ReturnType.TypeName == Enum.GetName(typeof(Keyword), Keyword.VOID).ToLower())
                throw new Exception(string.Format("Function {0} returns void, variable cannot be of type void", functionCallAst.Name.ToString()));

            return functionType.ReturnType;
        }

        public ArrayTypeAST GetArrayType(ArrayAccessAST arrayAccess)
        {
            arrayAccess.ArrayVariableName.Accept(this);

            ArrayTypeAST arrayType = _stateInfo.currentType as ArrayTypeAST;

            if (arrayType == null)
                throw new Exception("Invalid array type " + arrayAccess.ArrayVariableName);

            return arrayType;
        }

        public static TypeAST GetArrayContainedType(ArrayTypeAST arrayType, int accessDimensions = 1)
        {
            var accessType = arrayType.TypeOfContainedValues;

            for (var i = 1; i < accessDimensions; i++)
            {
                var arrayCast = (accessType as ArrayTypeAST);

                if (arrayCast == null)
                    throw new Exception("Array dimensions error: cannot access to " + i + "rd dimension of " + arrayType);

                accessType = arrayCast.TypeOfContainedValues;
            }

            return accessType;
        }
    }
}
