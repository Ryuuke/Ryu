using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryu
{
    class CodeGenVisitor : AbstractVisitor
    {
        public override void Visit(RootScopeAST rootScope) { }
        public override void Visit(ScopeAST scope) { }
        public override void Visit(NumberAST number) { }
        public override void Visit(HexNumberAST hexNumber) { }
        public override void Visit(FloatAST floatNumber) { }
        public override void Visit(StringAST stringConstant) { }
        public override void Visit(OperatorAST op) { }
        public override void Visit(VariableNameAST variableName) { }
        public override void Visit(VariableDecAST variableDec) { }
        public override void Visit(VariableDecAssignAST variableDecAssign) { }
        public override void Visit(VariableAssignAST variableAssign) { }
        public override void Visit(ArrayAccessAST arrayAcess) { }
        public override void Visit(ConstantVariable constantVariable) { }
        public override void Visit(ArrayAcessAssignAST arrayAcessAssign) { }
        public override void Visit(ConstantKeywordAST constantKeyword) { }
        public override void Visit(ExprAST expr) { }
        public override void Visit(FunctionProtoAST functionProto) { }
        public override void Visit(FunctionBodyAST functionBody) { }
        public override void Visit(FunctionCallAST functionCall) { }
        public override void Visit(StructAST structAST) { }
        public override void Visit(StructMemberCallAST structMemberCall) { }
        public override void Visit(StructMemberAssignAST structMemberAssign) { }
        public override void Visit(EnumAST enumAST) { }
        public override void Visit(IfAST ifStatement) { }
        public override void Visit(ForAST forStatement) { }
        public override void Visit(ForeachAST foreachStatement) { }
        public override void Visit(WhileAST whileStatement) { }
        public override void Visit(DoWhileAST doWhileStatement) { }
        public override void Visit(ReturnAST returnStatement) { }
        public override void Visit(ContinueAST continueStatement) { }
        public override void Visit(BreakAST breakStatement) { }
        public override void Visit(NewExprAST newStatement) { }
        public override void Visit(DeleteAST deleteStatement) { }
        public override void Visit(DeferAST deferStatement) { }
        public override void Visit(TypeAST type) { }
        public override void Visit(FunctionTypeAST functionType) { }
        public override void Visit(StaticArrayTypeAST staticArrayType) { }
        public override void Visit(DynamicArrayTypeAST dynamicArrayType) { }
        public override void Visit(ArrayTypeAST arrayAST) { }
        public override void Visit(UnaryOperator unaryOperator) { }
    }
}
