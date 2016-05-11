using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryu
{
    public class IdentExpr
    {
        public IdentifierInfo identInfo;
        public BaseExprAST expr;
        public string file;
    }

    public class TypeInferer
    {
        SymbolTableManager _symTableManager;
        List<IdentExpr> _identifiersToBeInferred;
        ExprTypeVisitor _typeVisitor;

        public TypeInferer(SymbolTableManager symTableManager)
        {
            _symTableManager = symTableManager;
            _identifiersToBeInferred = symTableManager.IdentifiersToBeInferred;

            Func<IdentifierInfo, TypeAST> GetVariableTypeFunc = (IdentifierInfo identInfo) =>
            {
                var identToBeInfered = _identifiersToBeInferred.Find(x => x.identInfo == identInfo);

                if (identToBeInfered == null)
                    throw new Exception("Undeclared identifier " + identInfo);

                return _typeVisitor.GetExprType(identToBeInfered.file, identToBeInfered.identInfo, identToBeInfered.expr);
            };

            _typeVisitor = new ExprTypeVisitor(symTableManager, GetVariableTypeFunc);
        }

        public void InferTypes()
        {
            foreach (var identExpr in _identifiersToBeInferred)
            {
                if (identExpr.identInfo.typeAST != null)
                    continue;

                var exprType = _typeVisitor.GetExprType(identExpr.file, identExpr.identInfo, identExpr.expr);

                if (exprType.ToString() == Enum.GetName(typeof(Keyword), Keyword.NULL).ToLower())
                    throw new Exception("Cannot Infer 'null' expression type");

                identExpr.identInfo.typeAST = exprType;
            }
        }
    }
}
