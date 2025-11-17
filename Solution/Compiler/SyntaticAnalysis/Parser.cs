using Compiler.IO;
using Compiler.Nodes;
using Compiler.Nodes.CommandNodes;
using Compiler.Tokenization;
using System.Collections.Generic;
using static Compiler.Tokenization.TokenType;

namespace Compiler.SyntacticAnalysis
{
    /// <summary>
    /// A recursive descent parser
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// The error reporter
        /// </summary>
        public ErrorReporter Reporter { get; }

        /// <summary>
        /// The tokens to be parsed
        /// </summary>
        private List<Token> tokens;

        /// <summary>
        /// The index of the current token in tokens
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// The current token
        /// </summary>
        private Token CurrentToken { get { return tokens[currentIndex]; } }

        /// <summary>
        /// Advances the current token to the next one to be parsed
        /// </summary>
        private void MoveNext()
        {
            if (currentIndex < tokens.Count - 1)
                currentIndex += 1;
        }

        /// <summary>
        /// Creates a new parser
        /// </summary>
        /// <param name="reporter">The error reporter to use</param>
        public Parser(ErrorReporter reporter)
        {
            Reporter = reporter;
        }

        /// <summary>
        /// Checks the current token is the expected kind and moves to the next token
        /// </summary>
        /// <param name="expectedType">The expected token type</param>
        private void Accept(TokenType expectedType)
        {
            if (CurrentToken.Type == expectedType)
            {
                Debugger.Write($"Accepted {CurrentToken}");
                MoveNext();
            }
        }

        /// <summary>
        /// Parses a program
        /// </summary>
        /// <param name="tokens">The tokens to parse</param>
        /// <returns>The abstract syntax tree resulting from the parse</returns>
        public ProgramNode Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            ProgramNode program = ParseProgram();
            return program;
        }



        /// <summary>
        /// Parses a program
        /// </summary>
        /// <returns>An abstract syntax tree representing the program</returns>
        private ProgramNode ParseProgram()
        {
            Debugger.Write("Parsing program");
            ICommandNode command = ParseCommand();
            ProgramNode program = new ProgramNode(command);
            return program;
        }



        /// <summary>
        /// Parses a command
        /// </summary>
        /// <returns>An abstract syntax tree representing the command</returns>
        private ICommandNode ParseCommand()
        {
            Debugger.Write("Parsing command");
            List<ICommandNode> commands = new List<ICommandNode>();
            commands.Add(ParseSingleCommand());
            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                commands.Add(ParseSingleCommand());
            }
            if (commands.Count == 1)
                return commands[0];
            else
                return new SequentialCommandNode(commands);
        }

        /// <summary>
        /// Parses a single command
        /// </summary>
        /// <returns>An abstract syntax tree representing the single command</returns>
        private ICommandNode ParseSingleCommand()
        {
            Debugger.Write("Parsing Single Command");
            switch (CurrentToken.Type)
            {
                case Identifier:
                    return ParseAssignmentOrCallCommand();
                case Pass:
                    Position pos = CurrentToken.Position;
                    Accept(Pass);
                    return new BlankCommandNode(pos);
                case Let:
                    return ParseLetCommand();
                case If:
                    return ParseIfCommand();
                case While:
                    return ParseWhileCommand();
                case Repeat:
                    return ParseRepeatCommand();
                case Unless:
                    return ParseUnlessCommand();
                case LeftBrace:
                    return ParseBlockCommand();

                default:
                    //return ParseSkipCommand();
                    Reporter.ReportError("Invalid Command");
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an assignment or call command
        /// </summary>
        /// <returns>An abstract syntax tree representing the command</returns>
        private ICommandNode ParseAssignmentOrCallCommand()
        {
            Debugger.Write("Parsing Assignment Command or Call Command");
            Position startPosition = CurrentToken.Position;
            IdentifierNode identifier = ParseIdentifier();
            if (CurrentToken.Type == LeftBracket)
            {
                Debugger.Write("Parsing Call Command");
                Accept(LeftBracket);
                IParameterNode parameter = ParseParameter();
                Accept(RightBracket);
                return new CallCommandNode(identifier, parameter);
            }
            else if (CurrentToken.Type == EqualEquals)
            {
                Debugger.Write("Parsing Assignment Command");
                Accept(EqualEquals);
                IExpressionNode expression = ParseExpression();
                return new AssignCommandNode(identifier, expression);
            }
            else
            {
                return new ErrorNode(startPosition);
            }
        }

        /// <summary>
        /// Parses a skip command
        /// </summary>
        /// <returns>An abstract syntax tree representing the skip command</returns>
        private ICommandNode ParseSkipCommand()
        {
            Debugger.Write("Parsing Skip Command");
            Position startPosition = CurrentToken.Position;
            return new BlankCommandNode(startPosition);
        }

        /// <summary>
        /// Parses a while command
        /// </summary>
        /// <returns>An abstract syntax tree representing the while command</returns>
        private ICommandNode ParseWhileCommand()
        {
            Debugger.Write("Parsing While Command");
            Position startPosition = CurrentToken.Position;
            Accept(While);
            IExpressionNode expression = ParseExpression();
            Accept(Do);
            ICommandNode command = ParseSingleCommand();
            return new WhileCommandNode(expression, command, startPosition);
        }

        /// <summary>
        /// Parses an if command
        /// </summary>
        /// <returns>An abstract syntax tree representing the if command</returns>
        private ICommandNode ParseIfCommand()
        {
            Debugger.Write("Parsing If Command");
            Position startPosition = CurrentToken.Position;
            Accept(If);
            IExpressionNode expression = ParseExpression();
            Accept(Then);
            ICommandNode thenCommand = ParseSingleCommand();
            Accept(Else);
            ICommandNode elseCommand = ParseSingleCommand();
            return new IfCommandNode(expression, thenCommand, elseCommand, startPosition);
        }

        /// <summary>
        /// Parses a let command
        /// </summary>
        /// <returns>An abstract syntax tree representing the let command</returns>
        private ICommandNode ParseLetCommand()
        {
            Debugger.Write("Parsing Let Command");
            Position startPosition = CurrentToken.Position;
            Accept(Let);
            Accept(Local);
            IDeclarationNode declaration = ParseDeclaration();
            Accept(In);
            ICommandNode command = ParseSingleCommand();
            return new LetCommandNode(declaration, command, startPosition);
        }

        /// <summary>
        /// Parses a declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the declaration</returns>
        private IDeclarationNode ParseDeclaration()
        {
            Debugger.Write("Parsing Declaration");
            // Collect all single declarations into a list
            List<IDeclarationNode> declarations = new List<IDeclarationNode>();

            // First do the single-declaration
            declarations.Add(ParseSingleDeclaration());

            // then to any ones seperated by ';'
            while (CurrentToken.Type == Semicolon)
            {
                Accept(Semicolon);
                declarations.Add(ParseSingleDeclaration());
            }

            // If there is only one declaration then no need to wrap it
            if (declarations.Count == 1)
                return declarations[0];
            else
                return new SequentialDeclarationNode(declarations);
        }

        /// <summary>
        /// Parses a single declaration
        /// </summary>
        /// <returns>An abstract syntax tree representing the single declaration</returns>
        private IDeclarationNode ParseSingleDeclaration()
        {
            Debugger.Write("Parsing Single Declaration");
            Position pos = CurrentToken.Position;

            // Must start with an identifier always
            if (CurrentToken.Type != Identifier)
            {
                Reporter.ReportError("Declaration must start with an identifier");
                return new ErrorNode(pos);
            }

            // First identifier — could be:
            //   - type-denoter for a var declaration
            //   - constant name for a const declaration
            IdentifierNode firstId = ParseIdentifier();

            // CASE 1 — CONST DECLARATION:   <identifier> == <expression>
            // If the next token is ==, this is CONST.
            if (CurrentToken.Type == EqualEquals)
            {
                Accept(EqualEquals);
                IExpressionNode expr = ParseExpression();
                return new ConstDeclarationNode(firstId, expr, pos);
            }

            // OTHERWISE: VAR DECLARATION:   <type-denoter> <identifier>
            // next token MUST be an identifier (the variable name)
            if (CurrentToken.Type == Identifier)
            {
                IdentifierNode varId = ParseIdentifier();
                TypeDenoterNode type = new TypeDenoterNode(firstId);
                return new VarDeclarationNode(varId, type, pos);
            }

            // NO MATCH  ERROR
            Reporter.ReportError("Invalid declaration syntax");
            return new ErrorNode(pos);
        }

        /// <summary>
        /// Parses a type denoter
        /// </summary>
        /// <returns>An abstract syntax tree representing the type denoter</returns>
        //private TypeDenoterNode ParseTypeDenoter()
        //{
        //    Debugger.Write("Parsing Type Denoter");
        //    IdentifierNode identifier = ParseIdentifier();
        //    return new TypeDenoterNode(identifier);
        //}

        /// <summary>
        /// Parses an expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression</returns>
        private IExpressionNode ParseExpression()
        {
            Debugger.Write("Parsing Expression");
            IExpressionNode leftExpression = ParsePrimaryExpression();
            while (CurrentToken.Type == Operator)
            {
                OperatorNode operation = ParseOperator();
                IExpressionNode rightExpression = ParsePrimaryExpression();
                leftExpression = new BinaryExpressionNode(leftExpression, operation, rightExpression);
            }
            return leftExpression;
        }

        /// <summary>
        /// Parses a primary expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the primary expression</returns>
        private IExpressionNode ParsePrimaryExpression()
        {
            Debugger.Write("Parsing Primary Expression");
            switch (CurrentToken.Type)
            {
                case IntLiteral:
                    return ParseIntExpression();
                case CharLiteral:
                    return ParseCharExpression();
                case Identifier:
                    return ParseIdExpression();
                case Operator:
                    return ParseUnaryExpression();
                case LeftBracket:
                    return ParseBracketExpression();
                default:
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an int expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the int expression</returns>
        private IExpressionNode ParseIntExpression()
        {
            Debugger.Write("Parsing Int Expression");
            IntegerLiteralNode intLit = ParseIntegerLiteral();
            return new IntegerExpressionNode(intLit);
        }

        /// <summary>
        /// Parses a char expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the char expression</returns>
        private IExpressionNode ParseCharExpression()
        {
            Debugger.Write("Parsing Char Expression");
            CharacterLiteralNode charLit = ParseCharacterLiteral();
            return new CharacterExpressionNode(charLit);
        }

        /// <summary>
        /// Parses an ID expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression</returns>
        private IExpressionNode ParseIdExpression()
        {
            Debugger.Write("Parsing Identifier Expression");
            IdentifierNode identifier = ParseIdentifier();
            return new IdExpressionNode(identifier);
        }

        /// <summary>
        /// Parses a unary expresion
        /// </summary>
        /// <returns>An abstract syntax tree representing the unary expression</returns>
        private IExpressionNode ParseUnaryExpression()
        {
            Debugger.Write("Parsing Unary Expression");
            OperatorNode operation = ParseOperator();
            IExpressionNode expression = ParsePrimaryExpression();
            return new UnaryExpressionNode(operation, expression);
        }

        /// <summary>
        /// Parses a bracket expression
        /// </summary>
        /// <returns>An abstract syntax tree representing the bracket expression</returns>
        private IExpressionNode ParseBracketExpression()
        {
            Debugger.Write("Parsing Bracket Expression");
            Accept(LeftBracket);
            IExpressionNode expression = ParseExpression();
            Accept(RightBracket);
            return expression;
        }



        /// <summary>
        /// Parses a parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the parameter</returns>
        private IParameterNode ParseParameter()
        {
            Debugger.Write("Parsing Parameter");
            switch (CurrentToken.Type)
            {
                case Identifier:
                case IntLiteral:
                case CharLiteral:
                case Operator:
                case LeftBracket:
                    return ParseExpressionParameter();
                case Var:
                    return ParseVarParameter();
                case RightBracket:
                    return new BlankParameterNode(CurrentToken.Position);
                default:
                    return new ErrorNode(CurrentToken.Position);
            }
        }

        /// <summary>
        /// Parses an expression parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the expression parameter</returns>
        private IParameterNode ParseExpressionParameter()
        {
            Debugger.Write("Parsing Value Parameter");
            IExpressionNode expression = ParseExpression();
            return new ExpressionParameterNode(expression);
        }

        /// <summary>
        /// Parses a variable parameter
        /// </summary>
        /// <returns>An abstract syntax tree representing the variable parameter</returns>
        private IParameterNode ParseVarParameter()
        {
            Debugger.Write("Parsing Variable Parameter");
            Position startPosition = CurrentToken.Position;
            Accept(Var);
            IdentifierNode identifier = ParseIdentifier();
            return new VarParameterNode(identifier, startPosition);
        }



        /// <summary>
        /// Parses an integer literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the integer literal</returns>
        private IntegerLiteralNode ParseIntegerLiteral()
        {
            Debugger.Write("Parsing integer literal");
            Token integerLiteralToken = CurrentToken;
            Accept(IntLiteral);
            return new IntegerLiteralNode(integerLiteralToken);
        }

        /// <summary>
        /// Parses a character literal
        /// </summary>
        /// <returns>An abstract syntax tree representing the character literal</returns>
        private CharacterLiteralNode ParseCharacterLiteral()
        {
            Debugger.Write("Parsing character literal");
            Token CharacterLiteralToken = CurrentToken;
            Accept(CharLiteral);
            return new CharacterLiteralNode(CharacterLiteralToken);
        }

        /// <summary>
        /// Parses an identifier
        /// </summary>
        /// <returns>An abstract syntax tree representing the identifier</returns>
        private IdentifierNode ParseIdentifier()
        {
            Debugger.Write("Parsing identifier");
            Token IdentifierToken = CurrentToken;
            Accept(Identifier);
            return new IdentifierNode(IdentifierToken);
        }

        /// <summary>
        /// Parses an operator
        /// </summary>
        /// <returns>An abstract syntax tree representing the operator</returns>
        private OperatorNode ParseOperator()
        {
            Debugger.Write("Parsing operator");
            Token OperatorToken = CurrentToken;
            Accept(Operator);
            return new OperatorNode(OperatorToken);
        }

        /// <summary>
        /// Parses an operator
        /// </summary>
        /// <returns>An abstract syntax tree representing the command</returns>
        private ICommandNode ParseBlockCommand()
        {
            Debugger.Write("Parsing Block Command");
            Accept(LeftBrace);
            ICommandNode command = ParseCommand();
            Accept(RightBrace);
            return command;
        }

        /// <summary>
        /// Parses Repeat 
        /// </summary>
        /// <returns>An abstract syntax tree representing the RepeatCommandNode</returns>
        private ICommandNode ParseRepeatCommand()
        {
            Debugger.Write("Parsing Repeat Command");
            Position startPosition = CurrentToken.Position;
            Accept(Repeat);
            ICommandNode commandBody = ParseSingleCommand();
            Accept(Until);
            // parse condition/expression
            IExpressionNode condition = ParseExpression();
            return new RepeatCommandNode(commandBody, condition, startPosition);
        }

        /// <summary>
        /// Parses Unless 
        /// </summary>
        /// <returns>An abstract syntax tree representing the UnlessCommandNode</returns>
        private ICommandNode ParseUnlessCommand()
        {
            Debugger.Write("Parsing Unless Command");
            Position position = CurrentToken.Position;
            Accept(Unless);
            // parse condition/expression
            IExpressionNode condition = ParseExpression();
            Accept(Do);
            ICommandNode commandBody = ParseSingleCommand();
            return new UnlessCommandNode(condition, commandBody, position);
        }

    }
}