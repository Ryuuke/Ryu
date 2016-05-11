using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

namespace Ryu
{
    public enum CompilerDirective
    {
        IMPORT,
        LOAD,
        RUN,
    }

    public enum ScopeKind
    {
        GLOBAL,
        FUNCTION,
        LOOP,
    }

    public class FileParser
    {
        Queue<TokenInfo> _tokenInfoQueue;
        TokenInfo _currentTokenInfo;
        string _currentIdentifier;
        string _filePath;
        RootScopeAST _rootAST;
        ScopeKind _scopeKind;

        #region events
        public event Action<string> OnLoadFile;

        public void LoadFile(string fullPath)
        {
            if (OnLoadFile != null)
                OnLoadFile(fullPath);
        }
        #endregion

        public FileParser(string filePath, Queue<TokenInfo> tokenInfoQueue)
        {
            _tokenInfoQueue = tokenInfoQueue;
            _filePath = filePath;
            _rootAST = new RootScopeAST();
            _rootAST.elements = new List<ASTNode>();
            _rootAST.FileDependencies = new List<string>();
        }

        private void ReadNext()
        {
            _currentTokenInfo = _tokenInfoQueue.Dequeue();

            if (IsTokenType(TokenType.IDENTIFIER))
                _currentIdentifier = _currentTokenInfo.token;
        }

        private TokenInfo PeekNext()
        {
            return _tokenInfoQueue.Peek();
        }

        private TokenInfo Peek(int amount)
        {
            return _tokenInfoQueue.Skip(amount).FirstOrDefault();
        }

        public RootScopeAST Parse()
        {
            return ParseRootScope();
        }

        private RootScopeAST ParseRootScope()
        {
            ASTNode tmpResult;

            while (_tokenInfoQueue.Count != 0)
            {
                ReadNext();

                if (IsSymbol(Symbol.NUMBERSIGN))
                {
                    ParseCompilerDirective();
                }
                else if (IsTokenType(TokenType.IDENTIFIER))
                {
                    if ((tmpResult = ParseIdentifier()) != null)
                    {
                        _rootAST.elements.Add(tmpResult);
                    }
                }
                else
                {
                    throw new Exception(
                        string.Format("'{0}' is invalid in global scope", _currentTokenInfo));
                }
            }

            return _rootAST;
        }

        private ScopeAST ParseScope()
        {
            if (!IsSymbol(Symbol.L_CURL_BRACKET))
                return null;

            ReadNext();

            var scopeAST = new ScopeAST()
            {
                elements = new List<ASTNode>()
            };

            if (IsSymbol(Symbol.R_CURL_BRACKET))
                return scopeAST;

            ASTNode tmpResult;

            while (!IsSymbol(Symbol.R_CURL_BRACKET))
            {
                if (IsTokenType(TokenType.KEYWORD))
                {
                    if ((tmpResult = ParseStatement()) != null)
                    {
                        scopeAST.elements.Add(tmpResult);
                    }
                }
                else if (IsTokenType(TokenType.IDENTIFIER))
                {
                    if ((tmpResult = ParseIdentifier()) != null)
                    {
                        scopeAST.elements.Add(tmpResult);
                    }
                }
                /* new inner scope */
                else if (IsSymbol(Symbol.L_CURL_BRACKET))
                {
                    if ((tmpResult = ParseScope()) != null)
                    {
                        scopeAST.elements.Add(tmpResult);
                    }
                }

                ReadNext();
            }

            return scopeAST;
        }

        private void ParseCompilerDirective()
        {
            ExpectSymbol(Symbol.NUMBERSIGN);

            ReadNext();

            var directive = (CompilerDirective)
                Enum.Parse(typeof(CompilerDirective), _currentIdentifier, true);

            /* #load "filename" */
            if (directive == CompilerDirective.LOAD)
            {
                ReadNext();

                ExpectType(TokenType.STRING_CONST);

                var fullPath =
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_filePath), _currentTokenInfo.token));

                if (_rootAST.FileDependencies.Contains(fullPath))
                    return;

                _rootAST.FileDependencies.Add(fullPath);

                LoadFile(fullPath);
            }
        }

        /**
         * All things that begins with an identifier, it can be :
         * Variable name : x
         * Struct declaration : lol :: struct { };
         * Function declaration : lol :: (float x) -> float { thing };
         * Enum declaration : lol :: enum {};
         * Variable declaration : lol := 5;
         * Variable assignment : lol = 6;
         * FunctionPtr declaration/assignment : lol := otherFuncVariable
         * Function call : lol();
         * struct variable assign : x.y.z =
         */
        private ASTNode ParseIdentifier()
        {
            ReadNext();

            // Function Call
            if (IsSymbol(Symbol.L_PARAN))
            {
                var functionCallOrArrayCall = ParseFunctionCall();

                /* fn()[expr] = */
                if (IsSymbol(Symbol.R_BRACKET))
                {
                    if (PeekNext().token[0] == (int)Symbol.POINT)
                    {
                        ReadNext();

                        var structMember = ParseStructMember(functionCallOrArrayCall);

                        if (structMember.variableNames.Last() is FunctionCallAST)
                        {
                            ExpectSymbol(Symbol.SEMICOLON);

                            return structMember;
                        }

                        ExpectSymbol(Symbol.EQUAL);

                        return ParseStructMemberAssign(structMember);
                    }

                    ReadNext();

                    ExpectSymbol(Symbol.EQUAL);

                    return ParseArrayAssignment(functionCallOrArrayCall as ArrayAccessAST);
                }

                return functionCallOrArrayCall;
            }
            else if (IsSymbol(Symbol.COLON))
            {
                if (IsType(PeekNext()))
                {
                    return ParseVariableDec();
                }

                ReadNext();

                if (IsSymbol(Symbol.COLON))
                {
                    // struct or enum
                    if (PeekNext().type ==  TokenType.KEYWORD && _scopeKind == ScopeKind.GLOBAL)
                    {
                        ReadNext();

                        if (IsKeyword(Keyword.STRUCT))
                        {
                            return ParseStruct();
                        }
                        else if (IsKeyword(Keyword.ENUM))
                        {
                            return ParseEnum();
                        }
                        else
                        {
                            throw new Exception(string.Format(
                                "Invalid keyword : {0}, expected enum or struct", 
                                _currentTokenInfo));
                        }
                    }
                    else
                    {
                        if (PeekNext().token[0] == (int) Symbol.L_PARAN && 
                            (Peek(1).token[0] == (int)Symbol.R_PARAN || 
                            (Peek(1).type == TokenType.IDENTIFIER &&
                             Peek(2).token[0] == (int) Symbol.COLON)))
                        {
                            ReadNext();

                            return ParseFunctionProto();
                        }

                        return ParseConstant();
                    }
                }
                else if (IsSymbol(Symbol.EQUAL))
                {
                    var variableDecAssignAST = ParseVariableDecAssign(_currentIdentifier, null);

                    return variableDecAssignAST;
                }
            }
            else if (IsSymbol(Symbol.EQUAL)) 
            {
                return ParseVariableAssignment();
            }
            else if (IsArithmeticOperator())
            {
                var op = _currentTokenInfo.token;

                ReadNext();

                if (IsSymbol(Symbol.EQUAL))
                {
                    return ParseVariableAssignment(op + "=");
                }
            }
            else if (IsSymbol(Symbol.POINT)) // x.y.z
            {
                var structMember = ParseStructMember();

                ExpectSymbol(Symbol.EQUAL);

                return ParseStructMemberAssign(structMember);
            }
            else if (IsSymbol(Symbol.L_BRACKET))
            {
                var arrayCallAST = ParseArrayCall();

                /* struct member call arr[expr]. */
                if (PeekNext().token[0] == (int)Symbol.POINT)
                {
                    var structMemberAST = ParseStructMember(arrayCallAST);

                    return ParseStructMemberAssign(structMemberAST);
                }

                /* normal array assignment */
                ReadNext();

                ExpectSymbol(Symbol.EQUAL);

                return ParseArrayAssignment(arrayCallAST as ArrayAccessAST);
            }

            return null;
        }

        private ASTNode ParseConstant()
        {
            var constAST = new ConstantVariable
            {
                VariableName = _currentIdentifier,
            };

            ReadNext();
            constAST.ExpressionValue = ParseExpr();

            ExpectSymbol(Symbol.SEMICOLON);

            return constAST;
        }

        private ASTNode ParseArrayAssignment(ArrayAccessAST arrayCall)
        {
            ExpectSymbol(Symbol.EQUAL);

            var arrayAssignAST = new ArrayAcessAssignAST();
            arrayAssignAST.ArrayAcess = arrayCall;

            ReadNext();

            arrayAssignAST.AssignmentExpr = ParseExpr();

            ExpectSymbol(Symbol.SEMICOLON);

            return arrayAssignAST;
        }
        
        private ASTNode ParseVariableDec()
        {
            ExpectSymbol(Symbol.COLON);

            var variableName = _currentIdentifier;

            ReadNext();

            TypeAST typeAST = ParseType();

            ReadNext();

            if (IsSymbol(Symbol.SEMICOLON))
            {
                return new VariableDecAST
                {
                    Name = variableName,
                    Type = typeAST
                };
            }
            else if (IsSymbol(Symbol.EQUAL))
            {
                return ParseVariableDecAssign(variableName, typeAST);
            }

            return null;
        }

        private ASTNode ParseVariableDecAssign(string variableName, TypeAST typeAST)
        {
            ExpectSymbol(Symbol.EQUAL);
            ReadNext();

            BaseExprAST assignExpr;

            assignExpr = ParseExpr();

            ExpectSymbol(Symbol.SEMICOLON);

            return new VariableDecAssignAST
            {
                Name = variableName,
                Type = typeAST,
                ExpressionValue = assignExpr
            };
        }

        /*
         * normal type -> variable : type;
         * infered type -> variable := EXPR
         * static array type -> variable : [100] int;
         * dynamic array type -> variable : [] int;
         * function type -> (type, type) -> type;
         * if it's declared in a function, only dynamic array type syntax is allowed
         */
        private TypeAST ParseType(bool isFunctionType = false)
        {
            Func<string> typeErrorMessage = () =>
            {
                return string.Format("invalid type '{0}'", _currentTokenInfo);
            };

            if (!IsType() && !IsSymbol(Symbol.EQUAL))
            {
                throw new Exception(typeErrorMessage());
            }

            TypeAST typeAST;

            if (IsSymbol(Symbol.L_BRACKET))
            {
                ReadNext();

                if (IsSymbol(Symbol.POINT))
                {
                    ReadNext();

                    ExpectSymbol(Symbol.POINT);

                    ReadNext();

                    ExpectSymbol(Symbol.R_BRACKET);
                    ReadNext();

                    typeAST = new DynamicArrayTypeAST() { TypeOfContainedValues = ParseType(isFunctionType) };
                    typeAST.TypeName = "Dynamic Array";
                    return typeAST;
                }
                else if (IsSymbol(Symbol.R_BRACKET))
                {
                    ReadNext();
                    typeAST = new ArrayTypeAST { TypeOfContainedValues = ParseType(isFunctionType) };
                    typeAST.TypeName = "Array Type";
                    return typeAST;
                }
                else if (!isFunctionType)
                {
                    uint arraySize;

                    if (uint.TryParse(_currentTokenInfo.token, out arraySize))
                    {
                        ReadNext();

                        ExpectSymbol(Symbol.R_BRACKET);

                        ReadNext();

                        typeAST = new StaticArrayTypeAST { Size = arraySize, TypeOfContainedValues = ParseType() };
                        typeAST.TypeName = "Static Array";
                        return typeAST;
                    }
                    else
                    {
                        throw new Exception(typeErrorMessage());
                    }
                }
                else
                {
                    throw new Exception(typeErrorMessage());
                }
            }
            else if (IsSymbol(Symbol.L_PARAN))
            {
                var functionTypeAST = new FunctionTypeAST();
                functionTypeAST.TypeName = "Function";
                functionTypeAST.ArgumentTypes = new List<TypeAST>();
                functionTypeAST.kind = FunctionTypeKind.FUNCTION_PTR;

                while (true)
                {
                    ReadNext();

                    if (IsSymbol(Symbol.R_PARAN))
                        break;

                    ExpectVariableType();

                    functionTypeAST.ArgumentTypes.Add(ParseType(true));

                    ReadNext();

                    if (IsSymbol(Symbol.R_PARAN))
                        break;

                    ExpectSymbol(Symbol.COMMA);
                }

                ReadNext();

                ExpectSymbol(Symbol.SUB);
                ReadNext();
                ExpectSymbol(Symbol.GREATER);   
                ReadNext();

                functionTypeAST.ReturnType = ParseType(true);

                return functionTypeAST;
            }
            else
            {
                typeAST = new TypeAST();
            }

            typeAST.TypeName = _currentTokenInfo.token;

            return typeAST;
        }

        private ASTNode ParseVariableAssignment(string op = null)
        {
            ExpectSymbol(Symbol.EQUAL);

            var variableName = _currentIdentifier;

            ReadNext();

            BaseExprAST assignExpr;

            assignExpr = ParseExpr();

            ExpectSymbol(Symbol.SEMICOLON);


            return new VariableAssignAST
            {
                VariableName = variableName,
                ExpressionValue = assignExpr,
                Operator = op ?? "="
            };
        }

        /* 
         * fn : (varname : TYPE) -> RETTYPE;
         * fn :=  (varname : TYPE) -> RETTYPE { scope }
         */
        private ASTNode ParseFunctionProto()
        {
            ExpectSymbol(Symbol.L_PARAN);

            var functionName = _currentIdentifier;

            List<VariableDecAST> args = null;

            while (true)
            {
                ReadNext();

                if (IsSymbol(Symbol.R_PARAN))
                    break;

                ExpectType(TokenType.IDENTIFIER);

                args = new List<VariableDecAST>();

                var variableName = _currentIdentifier;

                ReadNext();

                ExpectSymbol(Symbol.COLON);

                ReadNext();

                ExpectVariableType();

                var variableType = _currentTokenInfo.token;

                args.Add(new VariableDecAST
                {
                    Name = variableName,
                    Type = ParseType(true)
                });

                ReadNext();

                if (IsSymbol(Symbol.R_PARAN))
                    break;

                ExpectSymbol(Symbol.COMMA);
            }

            ReadNext();

            var functionProtoAST = new FunctionProtoAST();

            if (IsSymbol(Symbol.L_CURL_BRACKET))
            {
                functionProtoAST.Name = functionName;
                functionProtoAST.Args = args;
                functionProtoAST.ReturnType = new TypeAST
                {
                    TypeName = Enum.GetName(typeof(Keyword), Keyword.VOID).ToLower()
                };

                var functionBodyAST = new FunctionBodyAST();
                functionBodyAST.Prototype = functionProtoAST;
                var lastScope = _scopeKind;
                _scopeKind = ScopeKind.FUNCTION;
                functionBodyAST.Scope = ParseScope();
                _scopeKind = lastScope;
                return functionBodyAST;
            }

            // ->
            ExpectSymbol(Symbol.SUB);

            ReadNext();

            ExpectSymbol(Symbol.GREATER);

            ReadNext();

            ExpectVariableType();

            functionProtoAST.Name = functionName;
            functionProtoAST.Args = args;
            functionProtoAST.ReturnType = ParseType(true);

            ReadNext();

            if (!IsSymbol(Symbol.SEMICOLON))
            {
                ExpectSymbol(Symbol.L_CURL_BRACKET);

                var functionBodyAST = new FunctionBodyAST();
                functionBodyAST.Prototype = functionProtoAST;
                var lastScope = _scopeKind;
                _scopeKind = ScopeKind.FUNCTION;
                functionBodyAST.Scope = ParseScope();
                _scopeKind = lastScope;
                return functionBodyAST;
            }

            return functionProtoAST;
        }

        private ASTNode ParseEnum()
        {
            if (!IsKeyword(Keyword.ENUM))
                return null;

            var enumName = _currentIdentifier;

            ReadNext();

            ExpectSymbol(Symbol.L_CURL_BRACKET);

            var enumAST = new EnumAST
            {
                Name = enumName,
            };

            var enumVariables = new List<ASTNode>();

            while (true)
            {
                ReadNext();

                if (IsSymbol(Symbol.R_CURL_BRACKET))
                    break;

                ExpectType(TokenType.IDENTIFIER);

                if (PeekNext().token[0] == (int)Symbol.EQUAL)
                {
                    ReadNext();

                    var variableName = _currentIdentifier;

                    ReadNext();

                    BaseExprAST value;

                    if (IsTokenType(TokenType.IDENTIFIER))
                    {
                        value = new ExprAST { expr = new VariableNameAST { Name = _currentIdentifier } };
                    }
                    else if (IsTokenType(TokenType.INT_CONST))
                    {
                        value = new ExprAST { expr = new NumberAST { Value = int.Parse(_currentTokenInfo.token) } };
                    }
                    else
                    {
                        throw new Exception(
                            string.Format("Invalid enum value {0} : expected constant name or integer",
                            _currentTokenInfo));
                    }

                    enumVariables.Add(new VariableAssignAST
                    {
                        VariableName = variableName,
                        ExpressionValue = value
                    });
                }
                else
                {
                    enumVariables.Add(new VariableNameAST { Name = _currentIdentifier });
                }

                ReadNext();

                if (!IsSymbol(Symbol.COMMA))
                {
                    ExpectSymbol(Symbol.R_CURL_BRACKET);
                    break;
                }
            }

            enumAST.Values = enumVariables;

            return enumAST;
        }

        private ASTNode ParseStruct()
        {
            if (!IsKeyword(Keyword.STRUCT))
                return null;

            var structName = _currentIdentifier;

            ReadNext();

            ExpectSymbol(Symbol.L_CURL_BRACKET);

            var structAST = new StructAST
            {
                Name = structName,
            };

            var structVariables = new List<ASTNode>();

            while (true)
            {
                ReadNext();

                if (IsSymbol(Symbol.R_CURL_BRACKET))
                    break;

                ExpectType(TokenType.IDENTIFIER);

                ReadNext();

                ExpectSymbol(Symbol.COLON);

                structVariables.Add(ParseVariableDec());
            }

            structAST.Variables = structVariables;

            return structAST;
        }

        /* x[expr].y.z[expr] = */
        private StructMemberCallAST ParseStructMember(ASTNode headExpr = null)
        {
            ExpectSymbol(Symbol.POINT);

            var structMember = new StructMemberCallAST();

            structMember.variableNames = new List<ASTNode>();

            if (headExpr == null)
                structMember.variableNames.Add(new VariableNameAST { Name = _currentIdentifier });
            else
                structMember.variableNames.Add(headExpr);

            while (true)
            {
                ReadNext();

                if (!IsTokenType(TokenType.IDENTIFIER))
                    break;

                /* its an array : x.z[expr] */
                if (PeekNext().token[0] == (int)Symbol.L_BRACKET)
                {
                    ReadNext();
                    structMember.variableNames.Add(ParseArrayCall());
                }
                else
                {
                    structMember.variableNames.Add(new VariableNameAST { Name = _currentIdentifier });
                }

                ReadNext();

                if (!IsSymbol(Symbol.POINT))
                    break;
            }

            return structMember;
        }

        private ASTNode ParseStructMemberAssign(StructMemberCallAST structMember)
        {
            ExpectSymbol(Symbol.EQUAL);

            ReadNext();

            var structMemberAssign = new StructMemberAssignAST
            {
                StructMember = structMember,
                AssignExpr = ParseExpr()
            };

            ExpectSymbol(Symbol.SEMICOLON);

            return structMemberAssign;
        }

        private ASTNode ParseStatement()
        {
            ExpectType(TokenType.KEYWORD);

            if (IsKeyword(Keyword.IF))
            {
                return ParseIf();
            }
            
            if (IsKeyword(Keyword.FOR))
            {
                return ParseFor();
            }

            if (IsKeyword(Keyword.WHILE))
            {
                return ParseWhile();
            }

            if (IsKeyword(Keyword.DO))
            {
                return ParseDoWhile();
            }

            if (IsKeyword(Keyword.RETURN))
            {
                return ParseReturn();
            }

            if (IsKeyword(Keyword.CONTINUE))
            {
                if (_scopeKind != ScopeKind.LOOP)
                    throw new Exception("Cannot 'Continue' outside a loop");

                ReadNext();

                ExpectSymbol(Symbol.SEMICOLON);

                return new ContinueAST();
            }

            if (IsKeyword(Keyword.BREAK))
            {
                if (_scopeKind != ScopeKind.LOOP)
                    throw new Exception("Cannot 'Break' outside a loop");

                ReadNext();

                ExpectSymbol(Symbol.SEMICOLON);

                return new BreakAST();
            }

            if (IsKeyword(Keyword.DEFER))
            {
                return ParseDefer();
            }

            if (IsKeyword(Keyword.DELETE))
            {
                return ParseDelete();
            }

            return null;
        }

        private ASTNode ParseIf()
        {
            if (!IsKeyword(Keyword.IF))
                return null;

            ReadNext();

            var ifAST = new IfAST();

            ifAST.ConditionExpr = ParseExpr();

            ASTNode innerCode;

            if (IsTokenType(TokenType.KEYWORD))
            {
                innerCode = ParseStatement();
            }
            else if (IsTokenType(TokenType.IDENTIFIER))
            {
                innerCode = ParseIdentifier();
            }
            /* new inner scope */
            else if (IsSymbol(Symbol.L_CURL_BRACKET))
            {
                innerCode = ParseScope();
            }
            else
                throw new Exception("Unexpected token " + _currentTokenInfo);

            ifAST.IfInnerCode = innerCode;

            ReadNext();

            if (IsKeyword(Keyword.ELSE))
            {
                ReadNext();

                if (IsTokenType(TokenType.KEYWORD))
                {
                    innerCode = ParseStatement();
                }
                else if (IsTokenType(TokenType.IDENTIFIER))
                {
                    innerCode = ParseIdentifier();
                }
                /* new inner scope */
                else if (IsSymbol(Symbol.L_CURL_BRACKET))
                {
                    innerCode = ParseScope();
                }
                else
                    throw new Exception("Unexpected token " + _currentTokenInfo);

                ifAST.ElseInnerCode = innerCode;
            }

            return ifAST;
        }

        private ASTNode ParseFor()
        {
            if (!IsKeyword(Keyword.FOR))
                return null;

            ReadNext();

            var variableNameAST = new VariableNameAST();

            if (IsTokenType(TokenType.IDENTIFIER) && PeekNext().token[0] == (int)Symbol.COLON)
            {
                ReadNext();
                ReadNext();
                variableNameAST.Name = _currentIdentifier;
            }

            var forExpr = ParseExpr();

            if (IsSymbol(Symbol.L_CURL_BRACKET))
            {
                var foreachAST =  new ForeachAST
                {
                    ArrayExpr = forExpr,
                };

                var lastScopeKind = _scopeKind;
                _scopeKind = ScopeKind.LOOP;

                foreachAST.Scope = ParseScope();

                _scopeKind = lastScopeKind;
            }

            ExpectSymbol(Symbol.POINT);

            ReadNext();

            ExpectSymbol(Symbol.POINT);

            ReadNext();

            var forAST = new ForAST()
            {
                FromExpr = forExpr,
                ToExpr = ParseExpr(),
            };

            ExpectSymbol(Symbol.L_CURL_BRACKET);

            var lastScope = _scopeKind;
            _scopeKind = ScopeKind.LOOP;

            forAST.Scope = ParseScope();
            _scopeKind = lastScope;

            return forAST;
        }

        private ASTNode ParseWhile()
        {
            if (!IsKeyword(Keyword.WHILE))
                return null;

            ReadNext();

            var whileAST = new WhileAST();

            whileAST.ConditionExpr = ParseExpr();

            ExpectSymbol(Symbol.L_CURL_BRACKET);

            var lastScope = _scopeKind;
            _scopeKind = ScopeKind.LOOP;

            whileAST.Scope = ParseScope();

            _scopeKind = lastScope;

            return whileAST;
        }

        private ASTNode ParseDoWhile()
        {
            if (!IsKeyword(Keyword.DO))
                return null;

            ReadNext();

            ExpectSymbol(Symbol.L_CURL_BRACKET);

            var doWhileAST = new DoWhileAST();

            var lastScope = _scopeKind;
            _scopeKind = ScopeKind.LOOP;

            doWhileAST.Scope = ParseScope();

            _scopeKind = lastScope;

            ExpectSymbol(Symbol.R_CURL_BRACKET);

            ReadNext();

            if (!IsKeyword(Keyword.WHILE))
            {
                throw new Exception(
                    "DoWhile error : Expected While statement, found " + _currentTokenInfo);
            }

            ReadNext();

            doWhileAST.ConditionExpr = ParseExpr();

            ExpectSymbol(Symbol.SEMICOLON);

            return doWhileAST;
        }

        private ASTNode ParseReturn()
        {
            if (!IsKeyword(Keyword.RETURN))
                return null;

            if (_scopeKind == ScopeKind.GLOBAL)
                throw new Exception("Cannot return in global Scope");

            ReadNext();

            var returnAST = new ReturnAST();

            if (IsSymbol(Symbol.SEMICOLON))
                return returnAST;

            returnAST.ReturnExpr = ParseExpr();

            ExpectSymbol(Symbol.SEMICOLON);

            return returnAST;
        }

        private ASTNode ParseDefer()
        {
            if (!IsKeyword(Keyword.DEFER))
                return null;

            ReadNext();

            ASTNode deferredExpr;

            if (IsTokenType(TokenType.KEYWORD))
            {
                deferredExpr = ParseStatement();
            }
            else if (IsTokenType(TokenType.IDENTIFIER))
            {
                deferredExpr = ParseIdentifier();
            }
            else
            {
                throw new Exception(
                    string.Format("Invalid expression '{0}' after a defer statement", _currentTokenInfo));
            }

            var deferAST = new DeferAST
            {
                DeferredExpression = deferredExpr
            };

            return deferAST;
        }

        private ASTNode ParseDelete()
        {
            if (!IsKeyword(Keyword.DELETE))
                return null;

            ReadNext();

            ExpectType(TokenType.IDENTIFIER);

            var deleteAST = new DeleteAST
            {
                VariableName = new VariableNameAST { Name = _currentIdentifier }
            };

            ReadNext();

            ExpectSymbol(Symbol.SEMICOLON);

            return deleteAST;
        }

        private BaseExprAST ParseExpr()
        {
            if (IsKeyword(Keyword.NEW))
            {
                ReadNext();

                var newExpr = new NewExprAST { Type = ParseType() };

                ReadNext();

                return newExpr;
            }

            ExprAST expr = new ExprAST();

            var term = ParseTerm();

            if (IsOperator())
            {
                expr.expr = ParseOperator(term);
            }
            else
            {
                expr.expr = term;
            }

            return expr;
        }

        private ASTNode ParseOperator(ASTNode lhs)
        {
            var op = new OperatorAST();
            op.OperatorString = _currentTokenInfo.token;

            if (!IsAdditiveOperator())
            {
                op.Lhs = lhs;
                ReadNext();
                op.Rhs = ParseTerm();

                if (!IsOperator())
                    return op;

                return ParseOperator(op);
            }
            else
            {
                op.Lhs = lhs;
                ReadNext();

                var nextTerm = ParseTerm();

                if (!IsOperator())
                {
                    op.Rhs = nextTerm;
                    return op;
                }

                if (!IsAdditiveOperator())
                {
                    var operatorast = new OperatorAST();
                    operatorast.OperatorString = _currentTokenInfo.token;
                    operatorast.Lhs = nextTerm;
                    ReadNext();
                    operatorast.Rhs = ParseTerm();
                    op.Rhs = operatorast;

                    if (!IsOperator())
                        return op;

                    return ParseOperator(op);
                }

                op.Rhs = nextTerm;

                return ParseOperator(op);
            }
        }

        private ASTNode ParseTerm(bool lastIsUnaryOp = false)
        {
            if (IsTokenType(TokenType.IDENTIFIER))
            {
                ReadNext();

                /* function call : fn() */
                if (IsSymbol(Symbol.L_PARAN))
                {
                    var functionCall = ParseFunctionCall();

                    ReadNext();

                    /* we check if we are accessing a struct member and we are not in a for loop */
                    if (IsSymbol(Symbol.POINT) && PeekNext().token[0] != (int)Symbol.POINT)
                    {
                        return ParseStructMember(functionCall);
                    }

                    return functionCall;
                }
                /* array : arr[expr] */
                else if (IsSymbol(Symbol.L_BRACKET))
                {
                    var arrayCallAST = ParseArrayCall();

                    ReadNext();

                    /* we check if we are accessing a struct member and we are not in a for loop */
                    if (IsSymbol(Symbol.POINT) && PeekNext().token[0] != (int)Symbol.POINT)
                    {
                        return ParseStructMember(arrayCallAST);
                    }

                    return arrayCallAST;
                }
                /* struct member: x.y.z */
                else if (IsSymbol(Symbol.POINT))
                {
                    return ParseStructMember();
                }
                else
                {
                    return new VariableNameAST
                    {
                        Name = _currentIdentifier
                    };
                }
            }
            else if (IsTokenType(TokenType.INT_CONST))
            {
                return ParseNumber();
            }
            else if (IsTokenType(TokenType.STRING_CONST))
            {
                var strValue = _currentTokenInfo.token;

                ReadNext();

                return new StringAST
                {
                    Value = strValue
                };
            }
            else if (IsTokenType(TokenType.KEYWORD))
            {
                if (IsKeyword(Keyword.TRUE) || IsKeyword(Keyword.FALSE) || IsKeyword(Keyword.NULL))
                {
                    var keywordStr = _currentTokenInfo.token;

                    ReadNext();

                    return new ConstantKeywordAST
                    {
                        keyword = keywordStr
                    };
                }
            }
            else if (IsSymbol(Symbol.L_PARAN))
            {
                ReadNext();

                var expr = ParseExpr();

                ExpectSymbol(Symbol.R_PARAN);

                ReadNext();

                /* we check if we are accessing a struct member and we are not in a for loop */
                if (IsSymbol(Symbol.POINT) && PeekNext().token[0] != (int)Symbol.POINT)
                {
                    return ParseStructMember(expr);
                }

                if (IsSymbol(Symbol.L_BRACKET))
                {
                    var arrayCallAST = ParseArrayCall(expr);

                    ReadNext();

                    /* we check if we are accessing a struct member and we are not in a for loop */
                    if (IsSymbol(Symbol.POINT) && PeekNext().token[0] != (int)Symbol.POINT)
                    {
                        return ParseStructMember(arrayCallAST);
                    }

                    return arrayCallAST;
                }

                return expr;
            }
            else if (IsUnaryOperator())
            {
                if (IsSymbol(Symbol.EXCLAMATION))
                {
                    var unaryOp = new UnaryOperator
                    {
                        Operator = "!",
                    };

                    ReadNext();

                    unaryOp.term = ParseTerm();

                    return unaryOp;
                }
                else if (!lastIsUnaryOp)
                {
                    var unaryOp = new UnaryOperator
                    {
                        Operator = _currentTokenInfo.token,
                    };

                    ReadNext();

                    unaryOp.term = ParseTerm(true);

                    return unaryOp;
                }
            }

            throw new Exception(string.Format("Invalid Expression '{0}'", _currentTokenInfo));
        }

        private ASTNode ParseNumber()
        {
            string explicitType = "";

            /* check for float */
            if (PeekNext().token[0] == (int)Symbol.POINT && Peek(1).type == TokenType.INT_CONST)
            {
                var floatStr = _currentTokenInfo.token;

                ReadNext(); // eat .

                floatStr += _currentTokenInfo.token;

                ReadNext(); // eat the right part of the number

                floatStr += _currentTokenInfo.token;

                var floatValue = float.Parse(floatStr, CultureInfo.InvariantCulture);

                ReadNext();

                // terminate type : 5.3f32, more to do here
                if (Vocabulary.Floats.Contains(_currentTokenInfo.token))
                {
                    explicitType = _currentTokenInfo.token;
                    ReadNext();
                }

                return new FloatAST
                {
                    Value = floatValue,
                    ExplicitType = explicitType
                };
            }

            var intValue = int.Parse(_currentTokenInfo.token);

            ReadNext();

            // hex value : 0x2F727975i64;
            if (intValue == 0 && _currentTokenInfo.token[0] == 'x')
            {
                var stringHex = _currentTokenInfo.token.Substring(1);

                var mayBeExplicitType = stringHex.Substring(stringHex.Length - 3, 3);

                if (Vocabulary.Ints.Contains(mayBeExplicitType))
                {
                    explicitType = mayBeExplicitType;
                    stringHex = stringHex.Substring(0, stringHex.Length - 3);
                }

                Func<char, bool> isHex = (c) =>
                {
                    return
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');
                };

                if (stringHex.All(isHex))
                {
                    var hexVal = string.Concat(intValue, 'x', stringHex);

                    ReadNext();

                    return new HexNumberAST
                    {
                        Value = hexVal,
                        ExplicitType = explicitType
                    };
                }
            }

            // termination explicit type : 5i64
            if (Vocabulary.Ints.Contains(_currentTokenInfo.token))
            {
                explicitType = _currentTokenInfo.token;
                ReadNext();
            }

            return new NumberAST
            {
                Value = intValue,
                ExplicitType = explicitType
            };
        }

        private ASTNode ParseFunctionCall(ArrayAccessAST arrayCall = null)
        {
            ExpectSymbol(Symbol.L_PARAN);

            ASTNode functionName = arrayCall;

            if (functionName == null)
                functionName = new VariableNameAST { Name = _currentIdentifier };

            ReadNext();

            var functionCall = new FunctionCallAST
            {
                Name = functionName,
                ExpressionList = ParseExprList()
            };

            ExpectSymbol(Symbol.R_PARAN);

            /* we check if we are accessing an array */
            if (PeekNext().token[0] == (int) Symbol.L_BRACKET)
            {
                ReadNext();

                return ParseArrayCall(functionCall);
            }

            return functionCall;
        }

        private ASTNode ParseArrayCall(ASTNode headExpr = null)
        {
            ExpectSymbol(Symbol.L_BRACKET);

            ASTNode arrayVariable = headExpr;

            if (arrayVariable == null)
            {
                arrayVariable = new VariableNameAST { Name = _currentIdentifier };
            }

            var arrayAST = new ArrayAccessAST
            {
                ArrayVariableName = arrayVariable
            };

            arrayAST.AccessExprList = new List<BaseExprAST>();

            while (true)
            {
                ReadNext();

                arrayAST.AccessExprList.Add(ParseExpr());

                ExpectSymbol(Symbol.R_BRACKET);

                if (PeekNext().token[0] != (int)Symbol.L_BRACKET)
                    break;

                ReadNext();
            }

            ExpectSymbol(Symbol.R_BRACKET);

            ASTNode ast = arrayAST;

            /* array[expr]() : array of functions */
            if (PeekNext().token[0] == (int) Symbol.L_PARAN)
            {
                ReadNext();
                ast = ParseFunctionCall(arrayAST);
            }

            return ast;
        }

        private List<BaseExprAST> ParseExprList()
        {
            var expressionList = new List<BaseExprAST>();

            while (true)
            {
                if (IsSymbol(Symbol.R_PARAN))
                    return expressionList;

                expressionList.Add(ParseExpr());

                if (IsSymbol(Symbol.COMMA))
                    ReadNext();
            }
        }

        private bool IsUnaryOperator()
        {
            return (IsSymbol(Symbol.ADD) || IsSymbol(Symbol.SUB) || IsSymbol(Symbol.EXCLAMATION));
        }

        private bool IsOperator()
        {
            if (!IsTokenType(TokenType.SYMBOL))
                return false;

            if (IsArithmeticOperator() || IsLogicalOperator())
                return true;

            return false;
        }

        private bool IsArithmeticOperator()
        {
            return (IsSymbol(Symbol.ADD) ||
                    IsSymbol(Symbol.SUB) ||
                    IsSymbol(Symbol.MUL) ||
                    IsSymbol(Symbol.DIV) ||
                    IsSymbol(Symbol.MOD));
        }

        private bool IsAdditiveOperator()
        {
            if (_currentTokenInfo.token == "&&")
            {
                return true;
            }

            if (_currentTokenInfo.token == "||")
            {
                return true;
            }

            if (IsSymbol(Symbol.ADD) || IsSymbol(Symbol.SUB))
                return true;

            return false;
        }

        private bool IsLogicalOperator()
        {
            if (IsSymbol(Symbol.AND) && PeekNext().token[0] == (int)Symbol.AND)
            {
                ReadNext();
                _currentTokenInfo.token = "&&";
                return true;
            }

            if (IsSymbol(Symbol.OR) && PeekNext().token[0] == (int)Symbol.OR)
            {
                ReadNext();
                _currentTokenInfo.token = "||";
                return true;
            }

            if (IsSymbol(Symbol.GREATER))
            {
                if (PeekNext().token[0] == (int)Symbol.EQUAL)
                {
                    ReadNext();
                    _currentTokenInfo.token = ">=";
                }

                return true;
            }

            if (IsSymbol(Symbol.LOWER))
            {
                if (PeekNext().token[0] == (int)Symbol.EQUAL)
                {
                    ReadNext();
                    _currentTokenInfo.token = "<=";
                }

                return true;
            }

            if (IsSymbol(Symbol.EXCLAMATION))
            {
                if (PeekNext().token[0] == (int)Symbol.EQUAL)
                {
                    ReadNext();
                    _currentTokenInfo.token = "!=";
                }

                return true;
            }

            if (IsSymbol(Symbol.EQUAL) && PeekNext().token[0] == (int)Symbol.EQUAL)
            {
                ReadNext();
                _currentTokenInfo.token = "==";
                return true;
            }

            return false;
        }

        private void ExpectType(TokenType type)
        {
            if (!_currentTokenInfo.type.Equals(type))
                throw new Exception("Unexpected " + _currentTokenInfo);
        }

        private void ExpectSymbol(Symbol value)
        {
            if (_currentTokenInfo.token[0] != (int)value)
                throw new Exception(string.Format("Expected '{0}', found {1}", (char)(int)value, _currentTokenInfo));
        }

        private void ExpectVariableType()
        {
            if (!IsType())
                throw new Exception("{0} is not a valid type : " + _currentTokenInfo);
        }

        private bool IsKeyword(Keyword keyword)
        {
            if (Enum.GetName(typeof(Keyword), keyword).ToLower() == _currentTokenInfo.token)
                return true;

            return false;
        }

        private bool IsTokenType(TokenType tokenType)
        {
            if (_currentTokenInfo.type.Equals(tokenType))
                return true;

            return false;
        }

        private bool IsSymbol(Symbol symbol)
        {
            if (_currentTokenInfo.token[0] == (int)symbol)
                return true;

            return false;
        }

        private bool IsType(TokenInfo? tokenInfo = null)
        {
            if (!tokenInfo.HasValue)
                tokenInfo = _currentTokenInfo;

            var type = tokenInfo.Value.type;
            var token = tokenInfo.Value.token;

            if (Vocabulary.Types.Any(x => x == token)
                || type == TokenType.IDENTIFIER 
                || token[0] == (int)Symbol.L_BRACKET // array: [] int
                || token[0] == (int)Symbol.L_PARAN) // function: () -> x
                return true;

            return false;
        }
    }
}
