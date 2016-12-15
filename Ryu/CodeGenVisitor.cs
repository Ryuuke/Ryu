using System;
using System.Collections.Generic;
using LLVMSharp;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Ryu
{
    public class CodeGenVisitor : AbstractVisitor
    {
        string _currentFileName;
        int _scopeIdGen;
        int _currentNodePosition;
        int _currentScopeId;

        LLVMModuleRef _module;
        LLVMBuilderRef _builder;
        SymbolTableManager _symTableManager;
        LLVMValueRef _currentValue;
        LLVMValueRef _currentFunction;
        LLVMBool _true;
        LLVMBool _false;
        Dictionary<string, RootScopeAST> _programAST;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void program();

        public CodeGenVisitor(SymbolTableManager symTableManager)
        {
            _symTableManager = symTableManager;

            _module = LLVM.ModuleCreateWithName("Ryu");
            _builder = LLVM.CreateBuilder();
            _programAST = symTableManager.ProgramAST;

            _true = new LLVMBool(1);
            _false = new LLVMBool(0);
        }

        public void CodeGen()
        {
            foreach (var entry in _programAST)
            {
                _currentFileName = entry.Key;
                _scopeIdGen = 0;
                _currentScopeId = 0;
                _currentScopeId = 0;

                entry.Value.Accept(this);
            }

            LLVM.DumpModule(_module);
            Optimize();
            EmitExecutable();
        }

        private void Optimize()
        {
            var passManager = LLVM.CreatePassManager();

            LLVM.AddBasicAliasAnalysisPass(passManager);
            LLVM.AddInstructionCombiningPass(passManager);
            LLVM.AddGVNPass(passManager);
            LLVM.AddReassociatePass(passManager);

            LLVM.RunPassManager(passManager, _module);
        }

        private void EmitExecutable()
        {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86AsmPrinter();
            LLVM.LinkInGC();

            var x86Triple = "i686-pc-windows-msvc";
            var x64Triple = "x86_64-pc-windows-msvc";

            IntPtr error;
            LLVMTargetRef target;
            LLVM.GetTargetFromTriple(x86Triple, out target, out error);
            LLVM.SetTarget(_module, Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple()) + "-elf");

            var targetRef = LLVM.CreateTargetMachine(target,
                Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple()), "", "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);
            
            LLVM.TargetMachineEmitToFile(targetRef, _module,
                Marshal.StringToHGlobalAnsi("hello.obj"),
                LLVMCodeGenFileType.LLVMObjectFile, out error);

            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = "cmd.exe",
                Arguments = "/C gcc hello.obj -o hello.exe"
            };

            using (var process = Process.Start(processInfo))
                process.WaitForExit();
        }

        private void RunJit()
        {
            LLVMExecutionEngineRef engine;
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86AsmPrinter();

            var options = new LLVMMCJITCompilerOptions();
            var optionsSize = (4 * sizeof(int)) + IntPtr.Size; // LLVMMCJITCompilerOptions has 4 ints and a pointer

            IntPtr error;
            LLVM.VerifyModule(_module, LLVMVerifierFailureAction.LLVMAbortProcessAction, out error);
            LLVM.DisposeMessage(error);
            LLVM.SetTarget(_module, Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple()) + "-elf");
            LLVM.CreateMCJITCompilerForModule(out engine, _module, out options, optionsSize, out error);

            //LLVM.DumpModule(_module);

            var main = (program)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(engine, _currentFunction), typeof(program));
            main();
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

        public override void Visit(NumberAST number) 
		{
            _currentValue = LLVM.ConstInt(LLVM.Int32Type(), (ulong)number.Value, false);
		}

        public override void Visit(HexNumberAST hexNumber) 
		{
            _currentValue = LLVM.ConstInt(LLVM.Int32Type(), (ulong)hexNumber.Value, false);
        }

        public override void Visit(FloatAST floatNumber) 
		{
            _currentValue = LLVM.ConstReal(LLVM.FloatType(), floatNumber.Value);
		}

        public override void Visit(StringAST stringConstant) 
		{
            var stringPtr = LLVM.BuildAlloca(_builder, LLVM.ArrayType(LLVM.Int8Type(), (uint)stringConstant.Value.Length+1), "aString");
            var constString = LLVM.ConstString(stringConstant.Value, (uint)stringConstant.Value.Length, _false);

            LLVM.BuildStore(_builder, constString, stringPtr);

            _currentValue = LLVM.BuildGEP(_builder, stringPtr, new LLVMValueRef[] { LLVM.ConstInt(LLVM.Int32Type(), 0, _false), LLVM.ConstInt(LLVM.Int32Type(), 0, _false) }, "gepCast");
        }

        public override void Visit(OperatorAST op) 
		{
			
		}

        public override void Visit(VariableNameAST variableName) 
		{
            var ident = _symTableManager.LookupIdentifierInfo(_currentFileName, variableName.Name, _currentScopeId, _currentNodePosition);

            if (ident.isFnParam)
            {
                _currentValue = LLVM.GetParam(_currentFunction, (uint)ident.paramIndex);
            }
		}

        public override void Visit(VariableDecAST variableDec)
		{
            var variableDecIdentifier = _symTableManager.LookupIdentifierInfo(_currentFileName, variableDec.Name, 
                _currentScopeId, _currentNodePosition);

            if (variableDecIdentifier.valueRef.Pointer != IntPtr.Zero)
                return;

            LLVMTypeRef type;

            if (variableDecIdentifier.isFunctionType)
            {
                type = LLVM.PointerType(IRTypesConverter.GetFunctionType(variableDecIdentifier.typeAST as FunctionTypeAST), 0);
            }
            else
            {
                type = IRTypesConverter.PrimitivesTypesDic[variableDecIdentifier.typeAST.ToString()];
            }

            variableDecIdentifier.valueRef = LLVM.BuildAlloca(_builder, type, variableDec.Name);
        }

        public override void Visit(VariableDecAssignAST variableDecAssign) 
		{
			
		}

        public override void Visit(VariableAssignAST variableAssign) 
		{
			
		}

        public override void Visit(ArrayAccessAST arrayAcess) 
		{
			
		}

        public override void Visit(DeclareAST declareAST)
		{
            var identifier = _symTableManager.LookupIdentifierInfo(_currentFileName, declareAST.VariableDec.Name, _currentScopeId, _currentNodePosition);

            if (identifier.valueRef.Pointer != IntPtr.Zero)
                return;

            var fnType = IRTypesConverter.GetFunctionType(declareAST.VariableDec.Type as FunctionTypeAST);

            identifier.valueRef = LLVM.AddFunction(_module, identifier.name, fnType);
            LLVM.SetFunctionCallConv(identifier.valueRef, 0);
            LLVM.SetLinkage(identifier.valueRef, LLVMLinkage.LLVMExternalLinkage);
        }

        public override void Visit(ConstantVariable constantVariable) 
		{
			
		}

        public override void Visit(ArrayAcessAssignAST arrayAcessAssign) 
		{
			
		}

        public override void Visit(CastAST castAST) 
		{
            castAST.Expression.Accept(this);
            
            //if (castAST.Type.ToString().StartsWith("^"))
            //{
            //    if (_currentValue.IsConstant())
            //    {
            //        var tmp = LLVM.BuildAlloca(_builder, LLVM.PointerType(_currentValue.TypeOf(), 0), "tmp");
            //        LLVM.BuildStore(_builder, _currentValue, tmp);
            //        _currentValue = LLVM.BuildGEP(_builder, tmp, new LLVMValueRef[] { LLVM.ConstInt(LLVM.Int16Type(), 0, _false), LLVM.ConstInt(LLVM.Int16Type(), 0, _false) }, "gepCast");
            //    }
            //    else
            //    _currentValue = LLVM.BuildGEP(_builder, _currentValue, new LLVMValueRef[] { LLVM.ConstInt(LLVM.Int16Type(), 0, _false), LLVM.ConstInt(LLVM.Int16Type(), 0, _false) }, "gepCast");
            //}
		}

        public override void Visit(ConstantKeywordAST constantKeyword) 
		{
			
		}

        public override void Visit(ExprAST expr) 
		{
            expr.expr.Accept(this);
		}

        public override void Visit(FunctionProtoAST functionProto)
        {
            var functionIden = _symTableManager.LookupFunctionInfo(_currentFileName, functionProto.Name, _currentScopeId, functionProto.GetFunctionType().ArgumentTypes);

            if (functionIden.valueRef.Pointer == IntPtr.Zero)
            {
                var fnType = IRTypesConverter.GetFunctionType(functionProto.GetFunctionType());

                functionIden.valueRef = LLVM.AddFunction(_module, functionProto.Name, fnType);
            }

            _currentFunction = functionIden.valueRef;
        }

        public override void Visit(FunctionBodyAST functionBody) 
		{
            Visit(functionBody.Prototype);

            var block = LLVM.AppendBasicBlock(_currentFunction, "entry");

            LLVM.PositionBuilderAtEnd(_builder, block);

            Visit(functionBody.Scope);

            if (functionBody.Prototype.ReturnType.ToString() == Enum.GetName(typeof(Keyword), Keyword.VOID).ToLower())
                LLVM.BuildRetVoid(_builder);
        }

        public override void Visit(FunctionCallAST functionCall) 
		{
            var functionIdent = _symTableManager.LookupFunctionInfo(_currentFileName, (functionCall.Name as VariableNameAST).Name, _currentScopeId, functionCall.argTypes);

            if (functionIdent.valueRef.Pointer == IntPtr.Zero)
            {
                var fnType = IRTypesConverter.GetFunctionType(functionIdent.typeAST as FunctionTypeAST);

                functionIdent.valueRef = LLVM.AddFunction(_module, functionIdent.name, fnType);
            }

            var expressions = new LLVMValueRef[Math.Max(functionCall.ExpressionList.Count, 1)];

            for (var i = 0; i < functionCall.ExpressionList.Count; i++)
            {
                functionCall.ExpressionList[i].Accept(this);

                expressions[i] = _currentValue;

                if (LLVM.IsConstant(_currentValue))
                {
                    expressions[i] = LLVM.BuildAlloca(_builder, _currentValue.TypeOf(), "val");
                    LLVM.BuildStore(_builder, _currentValue, expressions[i]);
                    expressions[i].Dump();
                }
            }
            LLVM.BuildCall(_builder, functionIdent.valueRef, out expressions[0], (uint)functionCall.ExpressionList.Count, (functionIdent.typeAST as FunctionTypeAST).ReturnType.ToString() == "void" ? "" : functionIdent.name);
        }

        public override void Visit(StructAST structAST) 
		{
			
		}

        public override void Visit(StructMemberCallAST structMemberCall) 
		{
			
		}

        public override void Visit(StructMemberAssignAST structMemberAssign) 
		{
			
		}

        public override void Visit(EnumAST enumAST) 
		{
			
		}

        public override void Visit(IfAST ifStatement) 
		{
			
		}

        public override void Visit(ForAST forStatement) 
		{
			
		}

        public override void Visit(ForeachAST foreachStatement) 
		{
			
		}

        public override void Visit(WhileAST whileStatement) 
		{
			
		}

        public override void Visit(DoWhileAST doWhileStatement) 
		{
			
		}

        public override void Visit(ReturnAST returnStatement) 
		{
			
		}

        public override void Visit(ContinueAST continueStatement) 
		{
			
		}

        public override void Visit(BreakAST breakStatement) 
		{
			
		}

        public override void Visit(NewExprAST newStatement) 
		{
			
		}

        public override void Visit(DeleteAST deleteStatement) 
		{
			
		}

        public override void Visit(DeferAST deferStatement) 
		{
			
		}

        public override void Visit(TypeAST type) 
		{
			
		}

        public override void Visit(PtrTypeAST type) 
		{
			
		}

        public override void Visit(FunctionTypeAST functionType) 
		{
			
		}

        public override void Visit(StaticArrayTypeAST staticArrayType) 
		{
			
		}

        public override void Visit(DynamicArrayTypeAST dynamicArrayType) 
		{
			
		}

        public override void Visit(ArrayTypeAST arrayAST) 
		{
			
		}

        public override void Visit(UnaryOperator unaryOperator) 
		{
			
		}

        public override void Visit(PtrDerefAST ptrDerefAST) 
		{
			
		}

        public override void Visit(AddressOfAST addressOf) 
		{
			
		}
    }
}
