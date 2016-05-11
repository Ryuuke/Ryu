using System;
using System.Diagnostics;

namespace Ryu.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var parser = new Parser();

            var rootAST = parser.ParseProgramAsync("src/testSymbolTable.ryu").Result;

            var symTableManager = new SymbolTableManager(rootAST);

            symTableManager.GenerateSymbolTables();

            var typeInferer = new TypeInferer(symTableManager);
            var typeChecker = new TypeChecker(symTableManager);

            typeInferer.InferTypes();
            typeChecker.TypeCheck();

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);

            //foreach (var item in rootAST)
            //{
            //    Console.WriteLine(item.Value);
            //}

            Console.ReadKey();
        }
    }
}
