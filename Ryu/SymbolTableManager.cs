using System;
using System.Collections.Generic;

namespace Ryu
{
    public class SymbolTableManager
    {
        Dictionary<string, SymbolTable> _programSymbolTable;
        SymbolTableGenerator _symTableGen;

        public Dictionary<string, RootScopeAST> ProgramAST { get; }
        public List<IdentExpr> IdentifiersToBeInferred { get; private set; }


        public SymbolTableManager(Dictionary<string, RootScopeAST> programAST)
        {
            _symTableGen = new SymbolTableGenerator();
            ProgramAST = programAST;
        }

        public void GenerateSymbolTables()
        {
            _programSymbolTable = new Dictionary<string, SymbolTable>();
            IdentifiersToBeInferred = new List<IdentExpr>();
            var globalIdentifiers = new HashSet<string>();
            var globalTypes = new HashSet<string>();

            foreach (var entry in ProgramAST)
            {
                _programSymbolTable.Add(entry.Key, _symTableGen.GenerateSymTable(entry.Value, entry.Key));
                IdentifiersToBeInferred.AddRange(_symTableGen.IdentifiersToBeInferred);

                foreach (var globalIdentifier in _symTableGen.GlobalIdentifiers)
                {
                    var isUnique = globalIdentifiers.Add(globalIdentifier);

                    if (!isUnique)
                        throw new Exception(string.Format("Identifier {0} in file {1} is already declared in an other file", globalIdentifier, entry.Key));
                }

                foreach (var globalType in _symTableGen.GlobalTypes)
                {
                    var isUnique = globalTypes.Add(globalType);

                    if (!isUnique)
                        throw new Exception(string.Format("Struct or Enum {0} in file {1} is already declared in an other file", globalType, entry.Key));
                }
            }
        }

        public IdentifierInfo LookupIdentifierInfo(string filePath, string identifier, 
                                                int scopeId, int position, bool isConstant = false,
                                                List<string> processedFiles = null)
        {
            var symTable = _programSymbolTable[filePath];

            var identifierInfo = symTable.LookupIdentifierInfo(identifier, scopeId, position, isConstant);

            if (identifierInfo != null)
                return identifierInfo;

            if (processedFiles == null)
                processedFiles = new List<string> { filePath };

            /* we search in the global scope of the other files recursively */
            scopeId = 0;

            foreach (var file in symTable.FileDependencies)
            {
                if (processedFiles.Contains(file))
                    continue;

                identifierInfo = LookupIdentifierInfo(file, identifier, scopeId, -1, isConstant, processedFiles);

                if (identifierInfo != null)
                    return identifierInfo;

                processedFiles.Add(file);
            }

            return identifierInfo;
        }

        public IdentifierInfo LookupFunctionInfo(string filePath, string identifier,
                                                int scopeId, List<TypeAST> argsType = null,
                                                List<string> processedFiles = null)
        {
            var symTable = _programSymbolTable[filePath];

            var functionInfo = symTable.LookupFunctionInfo(identifier, scopeId, argsType);

            if (functionInfo != null)
                return functionInfo;

            if (processedFiles == null)
                processedFiles = new List<string> { filePath };

            /* we search in the global scope of the other files recursively */
            scopeId = 0;

            foreach (var file in symTable.FileDependencies)
            {
                if (processedFiles.Contains(file))
                    continue;

                functionInfo = LookupFunctionInfo(file, identifier, scopeId, argsType, processedFiles);

                if (functionInfo != null)
                    return functionInfo;

                processedFiles.Add(file);
            }

            return functionInfo;
        }

        public CustomTypeInfo LookupTypeInfo(string filePath, string typeString, 
                                                List<string> processedFiles = null)
        {
            var symTable = _programSymbolTable[filePath];

            var typeInfo = symTable.LookupTypeInfo(typeString);

            if (typeInfo != null)
                return typeInfo;

            if (processedFiles == null)
                processedFiles = new List<string> { filePath };

            foreach (var file in symTable.FileDependencies)
            {
                if (processedFiles.Contains(file))
                    continue;

                typeInfo = LookupTypeInfo(file, typeString, processedFiles);

                if (typeInfo != null)
                    return typeInfo;

                processedFiles.Add(file);
            }

            return typeInfo;
        }
    }
}
