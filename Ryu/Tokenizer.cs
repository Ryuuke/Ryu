using System;
using System.Linq;
using System.IO;

namespace Ryu
{
    public static class Vocabulary
    {
        public static string[] Floats = 
            {
                Enum.GetName(typeof(Keyword), Keyword.F32).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.F64).ToLower()
            };

        public static string[] Ints =
            {
                Enum.GetName(typeof(Keyword), Keyword.S16).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.S32).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.S64).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.U16).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.U32).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.U64).ToLower()
            };

        public static string[] Types =
            {
                Enum.GetName(typeof(Keyword), Keyword.U16).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.U32).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.U64).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.S16).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.S32).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.S64).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.F32).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.F64).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.STR).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.CHAR).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.BOOL).ToLower(),
                Enum.GetName(typeof(Keyword), Keyword.VOID).ToLower()
            };
    }

    public enum TokenType
    {
        UNDEFINED,
        KEYWORD,
        SYMBOL,
        IDENTIFIER, // variable name / function name etc..
        INT_CONST,
        STRING_CONST,
        CHAR_CONST
    }

    public enum Keyword
    {
        U16,
        U32,
        U64,
        S16,
        S32,
        S64,
        F32,
        F64,
        STR,
        CHAR,
        BOOL,
        VOID,
        TRUE,
        FALSE,
        NULL,
        IF,
        ELSE,
        WHILE,
        CONTINUE,
        BREAK,
        DO,
        FOR,
        RETURN,
        DEFER,
        STRUCT,
        ENUM,
        NEW,
        DELETE,
        DECLARE,
    }

    public enum Symbol
    {
        L_PARAN = 0x28,
        R_PARAN = 0x29,
        L_BRACKET = 0x5b,
        R_BRACKET = 0x5d,
        L_CURL_BRACKET = 0x7b,
        R_CURL_BRACKET = 0x7d,
        POINT = 0x2e,
        COMMA = 0x2c,
        SEMICOLON = 0x3b,
        ADD = 0x2b,
        SUB = 0x2d,
        MUL = 0x2a,
        DIV = 0x2f,
        MOD = 0x25,
        AND = 0x26,
        OR = 0x7c,
        GREATER = 0x3e,
        LOWER = 0x3c,
        EQUAL = 0x3d,
        COLON = 0x3a,
        INTEROGATION = 0x3f,
        EXCLAMATION = 0x21,
        QUOTATION = 0x22,
        APOSTROPHE = 0x27,
        NUMBERSIGN = 0x23,
        CARET = 0x5e,
    }

    public struct TokenInfo
    {
        public string token;
        public TokenType type;
        public int lineNumber;
        public int colNumber;

        public override string ToString()
        {
            return string.Format("'{1}', line {2}, column {3}", type, token, lineNumber, colNumber);
        }
    }

    public class Tokenizer : IDisposable
    {
        StreamReader _fileReader;
        char _currentChar;
        int _currentLine = 1;
        int _currentCol;
        

        public Tokenizer(string fileName)
        {
            _fileReader = new StreamReader(File.OpenRead(fileName));
        }

        public bool HasMoreTokens()
        {
            return !_fileReader.EndOfStream;
        }
        
        public void Advance(ref TokenInfo tokenInfo)
        {
            _currentChar = ReadNext();

            while (char.IsWhiteSpace(_currentChar))
            {
                _currentChar = ReadNext();
            }

            /* comments */
            int slash = (int)Symbol.DIV;
            int star = (int)Symbol.MUL;

            if (_currentChar == slash && NextChar() == slash)
            {
                SkipLine();
                Advance(ref tokenInfo);
                return;
            }

            if (_currentChar == slash && NextChar() == star)
            {
                _currentChar = ReadNext();
                _currentChar = ReadNext();

                char lastChar = _currentChar;

                while (lastChar != star || _currentChar != slash)
                {
                    lastChar = _currentChar;
                    _currentChar = ReadNext();
                }

                Advance(ref tokenInfo);
                return;
            }

            AssignLineAndCol(ref tokenInfo);

            if (char.IsDigit(_currentChar))
            {
                ScanNumber(ref tokenInfo);
            }
            else if (Enum.IsDefined(typeof(Symbol), (int)_currentChar))
            {
                /* @string \\\\\" -> \\" */
                if (_currentChar == (int)Symbol.QUOTATION)
                {
                    _currentChar = ReadNext();
                    char lastChar = _currentChar;
                    tokenInfo.token = string.Empty;
                    bool escapeNext = false;

                    while (_currentChar != (int)Symbol.QUOTATION || escapeNext == true)
                    {
                        if (_currentChar == '\\' && escapeNext == false)
                        {
                            escapeNext = true;
                            _currentChar = ReadNext();
                            continue;
                        }

                        tokenInfo.token += _currentChar;
                        lastChar = _currentChar;
                        escapeNext = false;
                        _currentChar = ReadNext();
                    }

                    tokenInfo.type = TokenType.STRING_CONST;
                }
                else if (_currentChar == (int) Symbol.APOSTROPHE)
                {

                }
                else
                {
                    tokenInfo.token = _currentChar.ToString();
                    tokenInfo.type = TokenType.SYMBOL;
                }
            }
            else if (char.IsLetter(_currentChar) || _currentChar == '_')
            {
                ScanIdentifierOrKeyword(ref tokenInfo);
            }
            else
            {
                tokenInfo.token = _currentChar.ToString();
                tokenInfo.type = TokenType.UNDEFINED;
            }
        }

        private void AssignLineAndCol(ref TokenInfo tokenInfo)
        {
            tokenInfo.lineNumber = _currentLine;
            tokenInfo.colNumber = _currentCol;
        }

        private void ScanNumber(ref TokenInfo tokenInfo)
        {
            if (!char.IsDigit(_currentChar))
                return;

            tokenInfo.token = string.Empty;

            tokenInfo.token += _currentChar;

            while (char.IsDigit(NextChar()))
            {
                _currentChar = ReadNext();
                tokenInfo.token += _currentChar;
            }

            tokenInfo.type = TokenType.INT_CONST;
        }

        private void ScanIdentifierOrKeyword(ref TokenInfo tokenInfo)
        {
            if (!char.IsLetterOrDigit(_currentChar) && _currentChar != '_')
                return;

            string token = string.Empty;
            token += _currentChar;

            while (char.IsLetterOrDigit(NextChar()) || NextChar() == '_')
            {
                _currentChar = ReadNext();
                token += _currentChar;
            }

            bool isKeyword = Enum.GetNames(typeof(Keyword)).Any(x => x.ToLower() == token);

            tokenInfo.type = (isKeyword) ? TokenType.KEYWORD : TokenType.IDENTIFIER;
            tokenInfo.token = token;
        }

        private void SkipLine()
        {
            _fileReader.ReadLine();
            _currentLine++;
            _currentCol = 0;
        }

        private char NextChar()
        {
            return (char)_fileReader.Peek();
        }

        private char ReadNext()
        {
            var next = (char)_fileReader.Read();

            if (next == '\n')
            {
                _currentLine++;
                _currentCol = 0;
            }
            else
            {
                _currentCol++;
            }

            return next;
        }

        public string GetKeyword(string token)
        {
            return Enum.GetNames(typeof(Keyword)).First(x => x.ToLower() == token);
        }

        public void Dispose()
        {
            _fileReader.Close();
        }
    }
}
