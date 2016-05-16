using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryu
{
    public class IdentifierInfo
    {
        public string name;
        public TypeAST typeAST;
        public int position;
        public int scopeId;
        public bool isFunctionType;
        public bool isConstant;
    }

    public struct IdentifierLocation
    {
        public string identifierName;
        public int scopeId;

        public override bool Equals(object obj)
        {
            var identLocation = obj as IdentifierLocation?;

            if (!identLocation.HasValue)
                return false;

            return identifierName == identLocation.Value.identifierName &&
                 scopeId == identLocation.Value.scopeId;
        }

        public override int GetHashCode()
        {
            return identifierName.GetHashCode() + scopeId;
        }
    }

    public class ScopeInfo
    {
        public int id;
        public ScopeInfo parent;
    }

    public enum TypeKind
    {
        STRUCT,
        ENUM,
    }

    public class CustomTypeInfo
    {
        public TypeAST type;
        public TypeKind kind;
        public int scopeId;
        public int position;
        public Dictionary<string, TypeAST> memberNameType;
    }

    public class SymbolTable
    {
        public Dictionary<IdentifierLocation, IdentifierInfo> IdentInfoDictionary { get; set; }
        public Dictionary<string, CustomTypeInfo> TypeInfoDictionary { get; set; }
        public Dictionary<int, ScopeInfo> ScopeInfoDictionary { get; set; }

        public string FilePath { get; set; }
        public List<string> FileDependencies { get; set; }

        /* position <= 0 means we don't really care about it */
        public IdentifierInfo LookupIdentifierInfo(string identifier, int scopeId, 
                                                    int position, bool isConstant = false)
        {
            IdentifierInfo identifierInfo;
            ScopeInfo scopeInfo;

            var identLocation = new IdentifierLocation { identifierName = identifier, scopeId = scopeId };

            while (!IdentInfoDictionary.TryGetValue(identLocation, out identifierInfo) || 
                (isConstant == true && identifierInfo.isConstant != isConstant) ||
               (!identifierInfo.isFunctionType && position > 0 && identifierInfo.position > position))
            {
                var scopeExists = ScopeInfoDictionary.
                    TryGetValue(identLocation.scopeId, out scopeInfo);

                if (!scopeExists || scopeInfo.parent == null)
                    return null;

                identLocation.scopeId = scopeInfo.parent.id;
            }

            return identifierInfo;
        }

        public IdentifierInfo LookupFunctionInfo(string identifier, int scopeId, List<TypeAST> argsType)
        {
            IdentifierInfo identifierInfo;
            ScopeInfo scopeInfo;

            var identLocation = new IdentifierLocation { identifierName = identifier, scopeId = scopeId };

            while (!IdentInfoDictionary.TryGetValue(identLocation, out identifierInfo) || 
                identifierInfo.isFunctionType == false || InvalidArgs(identifierInfo, argsType))
            {
                var scopeExists = ScopeInfoDictionary.
                    TryGetValue(identLocation.scopeId, out scopeInfo);

                if (!scopeExists || scopeInfo.parent == null)
                    return null;

                identLocation.scopeId = scopeInfo.parent.id;
            }

            return identifierInfo;
        }

        private bool InvalidArgs(IdentifierInfo identifierInfo, List<TypeAST> argsType)
        {
            var functionType = identifierInfo.typeAST as FunctionTypeAST;

            Debug.Assert(functionType != null);

            if (argsType != null && ((!functionType.IsVarArgsFn && functionType.ArgumentTypes.Count != argsType.Count) ||
                (functionType.IsVarArgsFn && functionType.ArgumentTypes.Count > argsType.Count)))
                return true;

            if (argsType == null && functionType.ArgumentTypes.Count != 0)
                return true;

            for (var i = 0; i < functionType.ArgumentTypes.Count; i++)
            {
                if (functionType.ArgumentTypes[i].ToString() != argsType[i].ToString())
                    return true;
            }

            return false;
        }

        public CustomTypeInfo LookupTypeInfo(string typeString)
        {
            CustomTypeInfo CustomTypeInfo;

            if (!TypeInfoDictionary.TryGetValue(typeString, out CustomTypeInfo))
                return null;

            return CustomTypeInfo;
        }
    }
}
