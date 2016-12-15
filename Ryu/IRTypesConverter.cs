using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;

namespace Ryu
{
    class IRTypesConverter
    {
        public static Dictionary<string, LLVMTypeRef> PrimitivesTypesDic = new Dictionary<string, LLVMTypeRef>
        {
            { Enum.GetName(typeof(Keyword), Keyword.S16).ToLower(), LLVM.Int16Type() },
            { Enum.GetName(typeof(Keyword), Keyword.S32).ToLower(), LLVM.Int32Type() },
            { Enum.GetName(typeof(Keyword), Keyword.S64).ToLower(), LLVM.Int64Type() },
            { Enum.GetName(typeof(Keyword), Keyword.U16).ToLower(), LLVM.Int16Type() },
            { Enum.GetName(typeof(Keyword), Keyword.U32).ToLower(), LLVM.Int32Type() },
            { Enum.GetName(typeof(Keyword), Keyword.U64).ToLower(), LLVM.Int64Type() },
            { Enum.GetName(typeof(Keyword), Keyword.F32).ToLower(), LLVM.FloatType() },
            { Enum.GetName(typeof(Keyword), Keyword.F64).ToLower(), LLVM.DoubleType() },
            { Enum.GetName(typeof(Keyword), Keyword.VOID).ToLower(), LLVM.VoidType() },
            { Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower(), LLVM.Int16Type() },
            { Enum.GetName(typeof(Keyword), Keyword.CHAR).ToLower(), LLVM.Int8Type() },
        };

        public static LLVMTypeRef GetStringType()
        {
            return LLVM.PointerType(LLVM.Int8Type(), 0);
        }

        public static LLVMTypeRef GetStructType(StructAST structAST, bool packed = false)
        {
            return new LLVMTypeRef();
        }

        public static LLVMTypeRef GetFunctionType(FunctionTypeAST functionType)
        {
            var args = new LLVMTypeRef[Math.Max(functionType.ArgumentTypes.Count, 1)];

            for (var i = 0; i < functionType.ArgumentTypes.Count; i++)
            {
                LLVMTypeRef value;

                if (!PrimitivesTypesDic.TryGetValue(functionType.ArgumentTypes[i].ToString(), out value))
                {
                    args[i] = GetStringType();
                }
                else
                {
                    args[i] = value;
                }
            }

            var returnType = PrimitivesTypesDic[functionType.ReturnType.ToString()];

            return LLVM.FunctionType(returnType, out args[0], (uint)functionType.ArgumentTypes.Count, functionType.IsVarArgsFn ? new LLVMBool(1) : new LLVMBool(0));
        }
    }
}
