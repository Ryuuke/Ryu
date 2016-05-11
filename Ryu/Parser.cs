using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ryu
{
    public class Parser
    {
        struct ASTInfo
        {
            public RootScopeAST rootScope;
            public string filePath;
        }

        Dictionary<string, RootScopeAST> _ASTDictionnary;
        HashSet<string> _operatedFiles;
        List<Task<ASTInfo>> _parseTasks;
        TaskCompletionSource<Dictionary<string, RootScopeAST>> _tcs;
        

        public Parser()
        {
            _ASTDictionnary = new Dictionary<string, RootScopeAST>();
            _operatedFiles = new HashSet<string>();
            _parseTasks = new List<Task<ASTInfo>>();
            _tcs = new TaskCompletionSource<Dictionary<string, RootScopeAST>>();
        }

        private ASTInfo GenerateAST(string filePath)
        {
            var tokenInfoQueue = new Queue<TokenInfo>();

            using (Tokenizer tokenizer = new Tokenizer(filePath))
            {
                TokenInfo tokenInfo = new TokenInfo();

                while (tokenizer.HasMoreTokens())
                {
                    tokenizer.Advance(ref tokenInfo);

                    if (tokenInfo.type == TokenType.UNDEFINED)
                        continue;

                    tokenInfoQueue.Enqueue(tokenInfo);
                }
            }

            var fileParser = new FileParser(filePath, tokenInfoQueue);

            fileParser.OnLoadFile += LoadFileASTAsync;

            var rootScope = fileParser.Parse();

            fileParser.OnLoadFile -= LoadFileASTAsync;

            return new ASTInfo { rootScope = rootScope, filePath = filePath };
        }

        public Task<Dictionary<string, RootScopeAST>> ParseProgramAsync(string filePath)
        {
            var fileDirectory = Path.GetDirectoryName(filePath);

            var fullPath =
              Path.GetFullPath(filePath);

            LoadFileASTAsync(fullPath);

            return _tcs.Task;
        }

        public Dictionary<string, RootScopeAST> ParseProgram(string filePath)
        {
            var fileDirectory = Path.GetDirectoryName(filePath);

            var fullPath =
              Path.GetFullPath(filePath);

            LoadFileAST(fullPath);

            return _ASTDictionnary;
        }

        private void LoadFileAST(string fullPath)
        {
            if (_operatedFiles.Contains(fullPath))
                return;

            _operatedFiles.Add(fullPath);

            var ast = GenerateAST(fullPath);

            _ASTDictionnary.Add(ast.filePath, ast.rootScope);
        }

        private void LoadFileASTAsync(string fullPath)
        {
            lock (_operatedFiles)
            {
                if (_operatedFiles.Contains(fullPath))
                    return;

                _operatedFiles.Add(fullPath);
            }

            var task = Task.Factory.StartNew(() => GenerateAST(fullPath));

            task.ContinueWith((result) =>
            {
                lock (_parseTasks)
                    _parseTasks.Add(task);

                if (_parseTasks.Count != _operatedFiles.Count)
                    return;

                try
                {
                    foreach (var completedTask in _parseTasks)
                    {
                        var astInfo = completedTask.Result;
                        _ASTDictionnary.Add(astInfo.filePath, astInfo.rootScope);
                    }

                    _tcs.SetResult(_ASTDictionnary);
                }
                catch(Exception e)
                {
                    //Console.WriteLine(e.ToString());
                    _tcs.SetException(e);
                }
            });
        }
    }
}
