using System.Collections.Generic;
using System;

namespace Ryu
{
    public abstract class ASTNode
    {
        public int lineNumber, columNumber;
        public abstract void Accept(AbstractVisitor v);
    }

    public class ScopeAST : ASTNode
    {
        public List<ASTNode> elements;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("NEW SCOPE \n{0}\n ENDSCOPE", string.Join("\n", elements));
        }
    }

    public class RootScopeAST : ScopeAST
    {
        public List<string> FileDependencies;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public class ConstantVariable : ASTNode
    {
        public string VariableName;
        public BaseExprAST ExpressionValue;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("VariableDecAssign : name {0}, value : {1}", 
                VariableName, ExpressionValue);
        }
    }

    public class NumberAST : ASTNode
    {
        public int Value;
        public string ExplicitType;

        public override string ToString()
        {
            return
                string.Format("(int) : {0} {1}", Value,
                ExplicitType != string.Empty ?
                string.Format("explicitType : {0}", ExplicitType) :
                ExplicitType);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public class HexNumberAST : ASTNode
    {
        public string Value;
        public string ExplicitType;

        public override string ToString()
        {
            return
                string.Format("(hex) : {0} {1}", Value,
                ExplicitType != string.Empty ?
                string.Format("explicitType : {0}", ExplicitType) :
                ExplicitType);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public class FloatAST : ASTNode
    {
        public float Value;
        public string ExplicitType;

        public override string ToString()
        {
            return
                string.Format("(float) : {0} {1}", Value,
                ExplicitType != string.Empty ?
                string.Format("explicitType : {0}", ExplicitType) :
                ExplicitType);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public class StringAST : ASTNode
    {
        public string Value;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("(str) : {0}", Value);
        }
    }

    /* arithmetic : + * - / && or logical : == >= > < <= !=  */
    public class OperatorAST : ASTNode
    {
        public string OperatorString;
        public ASTNode Lhs;
        public ASTNode Rhs;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}", Lhs, OperatorString, Rhs);
        }
    }
    
    /* +X -X !X */
    public class UnaryOperator : ASTNode
    {
        public string Operator;
        public ASTNode term;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", Operator, term);
        }
    }

    public class VariableNameAST : ASTNode
    {
        public string Name;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /* myVar: type; */
    public class VariableDecAST : VariableNameAST
    {
        public TypeAST Type;

        public override string ToString()
        {
            return string.Format("VariableDec : name : {0}, type : {1}", Name, Type);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /* myVar: type = expr | myVar := expr */
    public class VariableDecAssignAST : VariableDecAST
    {
        public BaseExprAST ExpressionValue;

        public override string ToString()
        {
            return string.Format("VariableDecAssign : name {0}, type : {1}, value : {2}",
                Name, Type != null ? Type.ToString() : "?", ExpressionValue);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /* myVar = expr */
    public class VariableAssignAST : ASTNode
    {
        public string VariableName;
        public BaseExprAST ExpressionValue;

        /* = += -= *= /= */
        public string Operator;

        public override string ToString()
        {
            return string.Format("VariableAssign : name {0}, value : {1}", VariableName, ExpressionValue);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /* myArray[expr] */
    public class ArrayAccessAST : ASTNode
    {
        /* can be a functionCall or variableName or expr */
        public ASTNode ArrayVariableName;

        /*
         * why list? : arr[x][y][z]...
         * so the list length is the number of dimensions 
         */
        public List<BaseExprAST> AccessExprList;

        public override string ToString()
        {
            return string.Format("ArrayAccess : name {0}, accessExpr : [{1}]",
                ArrayVariableName, string.Join("] , [", AccessExprList));
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /* myArray[expr] = assignExpr */
    public class ArrayAcessAssignAST : ASTNode
    {
        public ArrayAccessAST ArrayAcess;
        public BaseExprAST AssignmentExpr;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("ArrayAccessAssign: {0}, assignExpr : {1}",
                ArrayAcess, AssignmentExpr);
        }
    }

    /* Can be : true false null */
    public class ConstantKeywordAST : ASTNode
    {
        public string keyword;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("ConstantKeyword : {0}", keyword);
        }
    }

    public class BaseExprAST : ASTNode
    {
        public override void Accept(AbstractVisitor v)
        {
            throw new NotImplementedException();
        }
    }

    public class ExprAST : BaseExprAST
    {
        public ASTNode expr;

        public override void Accept(AbstractVisitor v)
        {
            expr.Accept(v);
        }

        public override string ToString()
        {
            return expr.ToString();
        }
    }

    /* func := (variable : type) -> returnType {} */
    public class FunctionProtoAST : ASTNode
    {
        public string Name;
        public List<VariableDecAST> Args;
        public TypeAST ReturnType;

        public override string ToString()
        {
            return string.Format("FunctionPrototype : Name {0}, Args : {1}, Return type : {2}",
                Name, Args == null ? "()" : string.Join("\n\t", Args), 
                ReturnType == null ? "?" : ReturnType.ToString());
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public FunctionTypeAST GetFunctionType()
        {
            var functionType = new FunctionTypeAST();
            functionType.ReturnType = ReturnType;
            functionType.ArgumentTypes = new List<TypeAST>();
            functionType.kind = FunctionTypeKind.FUNCTION;

            if (Args != null)
            {
                foreach (var arg in Args)
                {
                    functionType.ArgumentTypes.Add(arg.Type);
                }
            }

            functionType.TypeName = functionType.ToString();

            return functionType;
        }
    }

    /* func := (variable : type) -> returnType { scopeAST } */
    public class FunctionBodyAST : ASTNode
    {
        public FunctionProtoAST Prototype;
        public ScopeAST Scope;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("FunctionDecl : Prototype {0}, Body : \n \t {1}",
                Prototype, Scope);
        }
    }

    /* myFunc(Expr, Expr) */
    public class FunctionCallAST : ASTNode
    {
        /* can be variable name or arrayAccessCall : arr[expr]() */
        public ASTNode Name;
        public List<BaseExprAST> ExpressionList;

        public override string ToString()
        {
            return string.Format("FunctionCall : Name {0}, Args : {1}", Name,
                ExpressionList == null || ExpressionList.Count == 0 ? "()" : string.Join("\n\t", ExpressionList));
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /* structName :: struct {} */
    public class StructAST : ASTNode
    {
        public string Name;
        public List<ASTNode> Variables;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Struct : Name {0}, Variables : \n \t {1}",
                Name, Variables == null ? "" : string.Join("\n\t", Variables));
        }
    }

    /* x.y.z */
    public class StructMemberCallAST : ASTNode
    {
        /* can be variable name, functionCall, array access call or expr */
        public List<ASTNode> variableNames;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("StructMemberCall : {0}", string.Join(" . ", variableNames));
        }
    }

    /* x.y.z = Expr */
    public class StructMemberAssignAST : ASTNode
    {
        public StructMemberCallAST StructMember;
        public ASTNode AssignExpr;

        public override string ToString()
        {
            return string.Format("StructMemberAssign : {0} Assign : {1}", StructMember, AssignExpr);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /* enumName :: Enum {} */
    public class EnumAST : ASTNode
    {
        public string Name;
        public List<ASTNode> Values;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Struct : Name {0}, Variables : \n \t {1}",
                Name, Values == null ? "" : string.Join("\n\t", Values));
        }
    }

    /* if thing scope
     * if thing
     */
    public class IfAST : ASTNode
    {
        public BaseExprAST ConditionExpr;
        public ASTNode IfInnerCode;
        public ASTNode ElseInnerCode;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("If : Condition : {0}, Body : \n \t {1}", ConditionExpr, IfInnerCode);
        }
    }

    /**
     * for x .. y { 
     *  print(it);
     * }
     * OR
     * for n: x..y {
     *  print(n);
     * }
     * x and y are expressions
     */
    public class ForAST : ASTNode
    {
        public BaseExprAST FromExpr;
        public BaseExprAST ToExpr;
        public ASTNode Scope;
        public ASTNode VariableName;

        public override string ToString()
        {
            return string.Format("For : from : {0}, to : {1}, scope \n\t {2}", FromExpr, ToExpr, Scope);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    /**
     * for array {
     *  print(it);
     * }
     * OR
     * for x: array {
     *  print(x);
     * }
     */
    public class ForeachAST : ASTNode
    {
        public BaseExprAST ArrayExpr;
        public ASTNode Scope;
        public ASTNode VariableName;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Foreach : Expression : {0}, scope: \n\t {1}", ArrayExpr, Scope);
        }
    }

    /* return x; */
    public class ReturnAST : ASTNode
    {
        public BaseExprAST ReturnExpr;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Return : {0}", ReturnExpr);
        }
    }

    /* while expr {
     *  write(thing)
     * }
     */
    public class WhileAST : ASTNode
    {
        public BaseExprAST ConditionExpr;
        public ASTNode Scope;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("While : Expression : {0}, scope: \n\t {1}", ConditionExpr, Scope);
        }
    }

    /* 
     * do {
     *  write(thing)
     * } while expr;
     */
    public class DoWhileAST : ASTNode
    {
        public BaseExprAST ConditionExpr;
        public ASTNode Scope;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("DoWhile : Expression : {0}, scope: \n\t {1}", ConditionExpr, Scope);
        }
    }

    public class DeferAST : ASTNode
    {
        public ASTNode DeferredExpression;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Deferred : {0}", DeferredExpression);
        }
    }

    public class ContinueAST : ASTNode
    {
        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return "Continue";
        }
    }

    public class BreakAST : ASTNode
    {
        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return "Break";
        }
    }

    public class NewExprAST : BaseExprAST
    {
        public TypeAST Type;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("New : {0}", Type);
        }
    }

    public class DeleteAST : ASTNode
    {
        public VariableNameAST VariableName;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return string.Format("Delete : {0}", VariableName);
        }
    }

    public class TypeAST : ASTNode
    {
        public string TypeName;

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }

        public override string ToString()
        {
            return /*IsPointer ? string.Concat("^", TypeName) :*/ TypeName;
        }
    }

    public class ArrayTypeAST : TypeAST
    {
        public TypeAST TypeOfContainedValues;

        public override string ToString()
        {
            return string.Format("[] {0}", TypeOfContainedValues.ToString());
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public class StaticArrayTypeAST : ArrayTypeAST
    {
        public uint Size;

        public override string ToString()
        {
            return string.Format("[{0}] {1}", Size, TypeOfContainedValues.ToString());
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public class DynamicArrayTypeAST : ArrayTypeAST
    {
        public override string ToString()
        {
            return string.Format("[..] {0}", TypeOfContainedValues.ToString());
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }

    public enum FunctionTypeKind
    {
        FUNCTION,
        FUNCTION_PTR
    }

    public class FunctionTypeAST : TypeAST
    {
        public List<TypeAST> ArgumentTypes;
        public TypeAST ReturnType;
        public FunctionTypeKind kind;

        public override string ToString()
        {
            return string.Format("({0}) -> {1}",
                ArgumentTypes != null ? string.Join(", ", ArgumentTypes) : "",
                ReturnType);
        }

        public override void Accept(AbstractVisitor v)
        {
            v.Visit(this);
        }
    }
}
