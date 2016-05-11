using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryu
{
    public abstract class AbstractVisitor
    {
        public virtual void Visit(RootScopeAST rootScope) { }
        public virtual void Visit(ScopeAST scope) { }
        public virtual void Visit(NumberAST number) { }
        public virtual void Visit(HexNumberAST hexNumber) { }
        public virtual void Visit(FloatAST floatNumber) { }
        public virtual void Visit(StringAST stringConstant) { }
        public virtual void Visit(OperatorAST op) { }
        public virtual void Visit(VariableNameAST variableName) { }
        public virtual void Visit(VariableDecAST variableDec) { }
        public virtual void Visit(VariableDecAssignAST variableDecAssign) { }
        public virtual void Visit(VariableAssignAST variableAssign) { }
        public virtual void Visit(ArrayAccessAST arrayAcess) { }
        public virtual void Visit(ConstantVariable constantVariable) { }
        public virtual void Visit(ArrayAcessAssignAST arrayAcessAssign) { }
        public virtual void Visit(ConstantKeywordAST constantKeyword) { }
        public virtual void Visit(ExprAST expr) { }
        public virtual void Visit(FunctionProtoAST functionProto) { }
        public virtual void Visit(FunctionBodyAST functionBody) { }
        public virtual void Visit(FunctionCallAST functionCall) { }
        public virtual void Visit(StructAST structAST) { }
        public virtual void Visit(StructMemberCallAST structMemberCall) { }
        public virtual void Visit(StructMemberAssignAST structMemberAssign) { }
        public virtual void Visit(EnumAST enumAST) { }
        public virtual void Visit(IfAST ifStatement) { }
        public virtual void Visit(ForAST forStatement) { }
        public virtual void Visit(ForeachAST foreachStatement) { }
        public virtual void Visit(WhileAST whileStatement) { }
        public virtual void Visit(DoWhileAST doWhileStatement) { }
        public virtual void Visit(ReturnAST returnStatement) { }
        public virtual void Visit(ContinueAST continueStatement) { }
        public virtual void Visit(BreakAST breakStatement) { }
        public virtual void Visit(NewExprAST newStatement) { }
        public virtual void Visit(DeleteAST deleteStatement) { }
        public virtual void Visit(DeferAST deferStatement) { }
        public virtual void Visit(TypeAST type) { }
        public virtual void Visit(FunctionTypeAST functionType) { }
        public virtual void Visit(StaticArrayTypeAST staticArrayType) { }
        public virtual void Visit(DynamicArrayTypeAST dynamicArrayType) { }
        public virtual void Visit(ArrayTypeAST arrayAST) { }
        public virtual void Visit(UnaryOperator unaryOperator) { }
    }
}
