using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Ryu.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var parser = new Parser();

            var rootAST = parser.ParseProgramAsync("src/hello.ryu").Result;

            var symTableManager = new SymbolTableManager(rootAST);

            symTableManager.GenerateSymbolTables();

            var typeInferer = new TypeInferer(symTableManager);
            var typeChecker = new TypeChecker(symTableManager);
            var codeGen = new CodeGenVisitor(symTableManager);

            typeInferer.InferTypes();
            typeChecker.TypeCheck();
            codeGen.CodeGen();

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);

            Console.ReadKey();
        }
    }
}
