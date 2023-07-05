﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    using ParseFunc = Action<bool>;
    internal class Compiler
    {
        private enum Precedence
        {
            None,
            Assignment, // =
            Or,         // or
            And,        // and
            Equality,   // == !=
            Comparison, // < > <= >=
            Term,       // + -
            Factor,     // * /
            Unary,      // ! -
            Call,       // . ()
            Primary,
        }

        private class ParseRule
        {
            public ParseFunc? Prefix { get; private set; } = null;
            public ParseFunc? Infix { get; private set; } = null;
            public Precedence Precedence { get; private set; } = Precedence.None;

            public ParseRule(ParseFunc? prefix, ParseFunc? infix, Precedence precedence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precedence;
            }
        }

        private class LocalVariabal
        {
            public string Name { get; private set; }
            public int Depth { get; set; }  

            public LocalVariabal(string name, int depth)
            {
                Name = name;
                Depth = depth;
            }   
        }

        private class CompilerState
        {
            public List<LocalVariabal> LocalVars { get; private set; }
            public int ScopeDepth { get; set; }

            public CompilerState()
            {
                LocalVars = new (16);
            }
        }

        private readonly ParseRule[] _rules;

        private Token _previousToken;
        private Token _currentToken;
        private int _tokenIndex;
        private List<Token>? _tokens;

        private Chunk? _compilingChunk;
        private CompilerState? _currentSate;  

        public Chunk CurrentChunk => _compilingChunk!;
        public Compiler()
        {
            _rules = new ParseRule[Enum.GetValues(typeof(TokenType)).Length];

            _rules[(int)TokenType.LEFT_PAREN] = new ParseRule(Grouping, null, Precedence.None);
            _rules[(int)TokenType.RIGHT_PAREN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LEFT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RIGHT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.COMMA] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.DOT] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.MINUS] = new ParseRule(Unary, Binary, Precedence.Term);
            _rules[(int)TokenType.PLUS] = new ParseRule(null, Binary, Precedence.Term);
            _rules[(int)TokenType.SEMICOLON] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SLASH] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.STAR] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            _rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            _rules[(int)TokenType.EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EQUAL_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            _rules[(int)TokenType.GREATER] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.LESS] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.IDENTIFIER] = new ParseRule(Variable, null, Precedence.None);
            _rules[(int)TokenType.STRING] = new ParseRule(String, null, Precedence.None);
            _rules[(int)TokenType.NUMBER] = new ParseRule(Number, null, Precedence.None);
            _rules[(int)TokenType.AND] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.CLASS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.ELSE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.FOR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FUN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.IF] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.NIL] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.OR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.PRINT] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RETURN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SUPER] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.THIS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.VAR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.WHILE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EOF] = new ParseRule(null, null, Precedence.None);
        }

        public Chunk Compile(List<Token> tokens)
        {
            _tokens = tokens;

            Chunk chunk = new Chunk();  
            _compilingChunk = chunk;    

            CompilerState scope = new CompilerState();
            InitScope(scope);   

            Advance();
            while (_currentToken.Type != TokenType.EOF)
            {
                Declaration();
            }

            EndCompiler();
            return chunk;   
        }

        public void Reset()
        {
            _tokenIndex = 0;
            _tokens = null;
            _compilingChunk = null;
        }

        private void InitScope(CompilerState scope)
        {
            scope.ScopeDepth = 0;
            _currentSate = scope;  
        }

        private void BeginScope()
        {
            Debug.Assert(_currentSate != null);   
            
            ++_currentSate.ScopeDepth;
        }

        private void EndScope()
        {
            Debug.Assert( _currentSate != null );  

            --_currentSate.ScopeDepth;

            while (_currentSate.LocalVars.Count > 0 &&
                _currentSate.LocalVars[_currentSate.LocalVars.Count - 1].Depth > 
                _currentSate.ScopeDepth)
            {
                EmitByte((byte)OpCode.POP);
                _currentSate.LocalVars.RemoveAt(_currentSate.LocalVars.Count - 1);
            }
        }

        private void EndCompiler()
        {
            EmitReturn();
        }

        #region utility method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private void Advance()
        {
            Debug.Assert(_tokens != null);

            _previousToken = _currentToken;
            _currentToken = _tokens[_tokenIndex];
            ++_tokenIndex;
        }

        [method:MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Consume(TokenType type, string message)
        {
            if(_currentToken.Type == type)
            {
                Advance();
            }
            else
            {
                throw new CompilerException(_currentToken, message);
            }
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Check(TokenType type)
        {
            return _currentToken.Type == type;   
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(TokenType type)
        {
            if(!Check(type))
            {
                return false;
            }
            Advance();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitByte(byte b)
        {
            CurrentChunk.WriteByte(b, _previousToken.Line);  
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitBytes(byte b1, byte b2)
        {
            EmitByte(b1);
            EmitByte(b2);   
        }

        private void EmitReturn()
        {
            EmitByte((byte)OpCode.RETURN);
        }

        [MethodImpl(methodImplOptions:MethodImplOptions.AggressiveInlining)]    
        private void EmitConstant(Value val)
        {
            EmitBytes((byte)OpCode.CONSTANT, MakeConstant(val));
        }

        private byte MakeConstant(Value val)
        {
            int index = CurrentChunk.AddConstant(val);
            if(index > byte.MaxValue)
            {
                throw new CompilerException(_previousToken, "Too many constants in one chunk.");
            }
            return (byte)index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ParseRule GetRule(TokenType type)
        {
            return _rules[(int)type];
        }

        #endregion

        #region Parse literal method

        private void Literal(bool canAssign)
        {
            switch(_previousToken.Type)
            {
                case TokenType.FALSE:
                    EmitByte((byte)OpCode.FALSE);
                    break;
                case TokenType.TRUE:
                    EmitByte((byte)OpCode.TRUE);
                    break;
                case TokenType.NIL:
                    EmitByte((byte)OpCode.NIL);
                    break;
                default: return; // Unreachable.
            }
        }
        private void Number(bool canAssign)
        {
            double value = (double)_previousToken.Literal!;
            EmitConstant(new Value(value));
        }

        private void String(bool canAssign)
        {
            EmitConstant(new Value((string)_previousToken.Literal!));
        }

        private void Variable(bool canAssign)
        {
            NamedVariable(_previousToken, canAssign);
        }

        private void NamedVariable(Token variable, bool canAssign)
        {
            Debug.Assert(_currentSate != null);

            OpCode getOp, setOp;

            int localIndex = ResolveLocalVar(_currentSate, variable.Name);
            if(localIndex != -1) 
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                localIndex = MakeConstant(new Value(variable.Name));    
                getOp = OpCode.GET_GLOBAL;
                setOp = OpCode.SET_GLOBAL;
            }

            if(canAssign && Match(TokenType.EQUAL))
            {
                Expression();
                EmitBytes((byte)setOp, (byte)localIndex);
            }
            else
            {
                EmitBytes((byte)getOp, (byte)localIndex);
            }
        }
        private void Unary(bool canAssign)
        {
            TokenType operatorType = _previousToken.Type;

            // Compile the operand.
            ParsePrecedence(Precedence.Unary);

            switch (operatorType) 
            {
                case TokenType.BANG:
                    EmitByte((byte)OpCode.NOT);
                    break;
                case TokenType.MINUS:
                    EmitByte((byte)OpCode.NEGATE);
                    break;
                default:
                    return;// Unreachable.
            }
        }

        private void Binary(bool canAssign)
        {
            TokenType operatorType = _previousToken.Type;
            ParseRule rule = GetRule(operatorType);
            ParsePrecedence(rule.Precedence + 1);

            switch (operatorType) 
            {
                case TokenType.BANG_EQUAL:
                    EmitBytes((byte)OpCode.EQUAL, (byte)OpCode.NOT);
                    break;
                case TokenType.EQUAL_EQUAL:
                    EmitByte((byte)OpCode.EQUAL);
                    break;
                case TokenType.GREATER:
                    EmitByte((byte)OpCode.GREATER);
                    break;
                case TokenType.GREATER_EQUAL:
                    EmitBytes((byte)OpCode.LESS, (byte)OpCode.NOT);
                    break;
                case TokenType.LESS:
                    EmitByte((byte)OpCode.LESS);
                    break;
                case TokenType.LESS_EQUAL:
                    EmitBytes((byte)OpCode.GREATER, (byte)OpCode.NOT);  
                    break;
                case TokenType.PLUS:
                    EmitByte((byte)OpCode.ADD);
                    break;
                case TokenType.MINUS:
                    EmitByte((byte)OpCode.SUBTRACT);
                    break;
                case TokenType.STAR:
                    EmitByte((byte)OpCode.MULTIPLY);
                    break;
                case TokenType.SLASH: 
                    EmitByte((byte)OpCode.DIVIDE);
                    break;
                default: return; // Unreachable.
            }
        }

        private void Grouping(bool canAssign)
        {
            Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
        }


        #endregion

        #region Parse expression or statement method

        private void Block()
        {
            while (!Check(TokenType.RIGHT_BRACE) && !Check(TokenType.EOF))
            {
                Declaration();  
            }
            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        }

        /// <summary>
        /// We simply parse the lowest precedence level, 
        /// which subsumes all of the higher-precedence expressions too. 
        /// </summary>
        private void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        private void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            EmitByte((byte)OpCode.POP);
        }
        private void Statement()
        {
            if (Match(TokenType.PRINT)) 
            {
                PrintStatement();
            }
            else if(Match(TokenType.LEFT_BRACE))
            {
                BeginScope();
                Block();
                EndScope();
            }
            else
            {
                ExpressionStatement();  
            }
        }

        private void PrintStatement()
        {
            Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value.");
            EmitByte((byte)OpCode.Print);
        }
        private void Declaration()
        {
            if (Match(TokenType.VAR))
            {
                VarDeclaration();   
            }
            else 
            {
                Statement();    
            }
        }

        private void VarDeclaration()
        {
            byte global = ParseVariable("Expect variable name.");

            if (Match(TokenType.EQUAL)) 
            {
                Expression();
            }
            else
            {
                EmitByte((byte)OpCode.NIL);
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            DefineVariable(global); 
        }

        private byte ParseVariable(string errorMessage)
        {
            Debug.Assert(_currentSate != null);

            Consume(TokenType.IDENTIFIER, errorMessage);

            DeclareVariable();
            if (_currentSate.ScopeDepth > 0)
            {
                return 0;
            }

            return MakeConstant(new Value(_previousToken.Name));
        }

        private void ParsePrecedence(Precedence precedence)
        {
            Advance();

            ParseFunc? prefixRule = GetRule(_previousToken.Type).Prefix;
            if (prefixRule == null)
            {
                throw new CompilerException(_previousToken, "Expect expression.");
            }

            bool canAssign = precedence <= Precedence.Assignment;
            prefixRule(canAssign);

            while (precedence <= GetRule(_currentToken.Type).Precedence) 
            {
                Advance();
                ParseFunc infixRule = GetRule(_previousToken.Type).Infix!;
                infixRule(canAssign);
            }
            if (canAssign && Match(TokenType.EQUAL))
            {
                throw new CompilerException(_previousToken, "Invalid assignment target.");
            }
        }

        #endregion

        #region assistant method 

        private void DefineVariable(byte global)
        {
            Debug.Assert(_currentSate != null);

            // local variable is already at the top of the stack.
            if (_currentSate.ScopeDepth > 0)
            {
                // mark variable initialized.
                _currentSate.LocalVars[_currentSate.LocalVars.Count - 1].Depth = _currentSate.ScopeDepth;
                return;
            }
            EmitBytes((byte)OpCode.DEFINE_GLOBAL, global);
        }

        private void DeclareVariable()
        {
            Debug.Assert(_currentSate != null);

            if (_currentSate.ScopeDepth == 0)
            {
                return;
            }

            for (int i = _currentSate.LocalVars.Count - 1; i >= 0; --i)
            {
                LocalVariabal local = _currentSate.LocalVars[i];
                if (local.Depth != -1 && local.Depth < _currentSate.ScopeDepth)
                {
                    break;
                }

                if (_previousToken.Name == local.Name)
                {
                    throw new CompilerException(_previousToken, "Already a variable with this name in this scope.");
                }
            }

            AddLocalVariable(_previousToken.Name);
        }

        private void AddLocalVariable(string variableName)
        {
            Debug.Assert(_currentSate != null);

            if (_currentSate.LocalVars.Count == Byte.MaxValue + 1)
            {
                throw new CompilerException(_previousToken, "Too many local variables in function.");
            }

            LocalVariabal local = new(variableName, -1);
            _currentSate.LocalVars.Add(local);
        }

        private int ResolveLocalVar(CompilerState state, string varName)
        {
            for (int i = state.LocalVars.Count - 1; i >= 0; --i)
            {
                LocalVariabal local = state.LocalVars[i];
                if(local.Name == varName)
                {
                    if(local.Depth == -1)
                    {
                        throw new CompilerException(_previousToken, 
                            "Can't read local variable in its own initializer.");
                    }
                    return i;
                }
            }

            // we return -1 to signal that it wasn’t found
            // and should be assumed to be a global variable instead.
            return -1;
        }

        #endregion
    }
}
