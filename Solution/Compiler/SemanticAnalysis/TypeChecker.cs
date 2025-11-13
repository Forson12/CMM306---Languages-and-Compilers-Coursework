using Compiler.IO;
using Compiler.Nodes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using static System.Reflection.BindingFlags;

namespace Compiler.SemanticAnalysis
{
    /// <summary>
    /// A type checker
    /// </summary>
    public class TypeChecker
    {
        /// <summary>
        /// The error reporter
        /// </summary>
        public ErrorReporter Reporter { get; }

        /// <summary>
        /// Creates a new type checker
        /// </summary>
        /// <param name="reporter">The error reporter to use</param>
        public TypeChecker(ErrorReporter reporter)
        {
            Reporter = reporter;
        }

        /// <summary>
        /// Carries out type checking on a program
        /// </summary>
        /// <param name="tree">The program to check</param>
        public void PerformTypeChecking(ProgramNode tree)
        {
            PerformTypeCheckingOnProgram(tree);
        }

        /// <summary>
        /// Carries out type checking on a node
        /// </summary>
        /// <param name="node">The node to perform type checking on</param>
        private void PerformTypeChecking(IAbstractSyntaxTreeNode node)
        {
            if (node is null)
                // Shouldn't have null nodes - there is a problem with your parsing
                Debugger.Write("Tried to perform type checking on a null tree node");
            else if (node is ErrorNode)
                // Shouldn't have error nodes - there is a problem with your parsing
                Debugger.Write("Tried to perform type checking on an error tree node");
            else
            {
                string functionName = "PerformTypeCheckingOn" + node.GetType().Name.Remove(node.GetType().Name.Length - 4);
                MethodInfo function = this.GetType().GetMethod(functionName, NonPublic | Public | Instance | Static);
                if (function == null)
                    // There is not a correctly named function below
                    Debugger.Write($"Couldn't find the function {functionName} when type checking");
                else
                    function.Invoke(this, new[] { node });
            }
        }



        /// <summary>
        /// Carries out type checking on a program node
        /// </summary>
        /// <param name="programNode">The node to perform type checking on</param>
        private void PerformTypeCheckingOnProgram(ProgramNode programNode)
        {
            PerformTypeChecking(programNode.Command);
        }



        /// <summary>
        /// Carries out type checking on an assign command node
        /// </summary>
        /// <param name="assignCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnAssignCommand(AssignCommandNode assignCommand)
        {
            PerformTypeChecking(assignCommand.Identifier);
            PerformTypeChecking(assignCommand.Expression);
            if (!(assignCommand.Identifier.Declaration is IVariableDeclarationNode varDeclaration))
            {
                Reporter.ReportError($"Trying to assign to something which is not a variable " +
                    $"at line {assignCommand.Position.LineNumber}, column {assignCommand.Position.PositionInLine}" +
                    $": {assignCommand.Identifier.IdentifierToken.Spelling}");
            }
            else if (varDeclaration.EntityType != assignCommand.Expression.Type)
            {
                Reporter.ReportError($"Trying to assign a {assignCommand.Expression.Type.Name} to a {varDeclaration.EntityType.Name} variable" +
                    $"at line {assignCommand.Position.LineNumber}, column {assignCommand.Position.PositionInLine}");
            }
        }

        /// <summary>
        /// Carries out type checking on a blank command node
        /// </summary>
        /// <param name="blankCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnBlankCommand(BlankCommandNode blankCommand)
        {
        }

        /// <summary>
        /// Carries out type checking on a call command node
        /// </summary>
        /// <param name="callCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnCallCommand(CallCommandNode callCommand)
        {
            PerformTypeChecking(callCommand.Identifier);
            PerformTypeChecking(callCommand.Parameter);
            if (!(callCommand.Identifier.Declaration is FunctionDeclarationNode functionDeclaration))
            {
                Reporter.ReportError($"{callCommand.Identifier.IdentifierToken.Spelling} is being used as a function but is not one " +
                    $"at line {callCommand.Position.LineNumber}, column {callCommand.Position.PositionInLine}");
            }
            else if (GetNumberOfArguments(functionDeclaration.Type) == 0)
            {
                if (!(callCommand.Parameter is BlankParameterNode))
                {
                    Reporter.ReportError($"{functionDeclaration.Name} takes no arguments but is being called with some " +
                        $"at line {callCommand.Position.LineNumber}, column {callCommand.Position.PositionInLine}");
                }
            }
            else
            {
                if (callCommand.Parameter is BlankParameterNode)
                {
                    Reporter.ReportError($"{functionDeclaration.Name} takes an argument but is being called without any " +
                        $"at line {callCommand.Position.LineNumber}, column {callCommand.Position.PositionInLine}");
                }
                else
                {
                    if (GetArgumentType(functionDeclaration.Type, 0) != callCommand.Parameter.Type)
                    {
                        Reporter.ReportError($"{functionDeclaration.Name} expects a " +
                            $"{functionDeclaration.Type.Parameters[0].type.Name} as a parameter but was given a {callCommand.Parameter.Type.Name} " +
                            $"at line {functionDeclaration.Position.LineNumber}, column {functionDeclaration.Position.PositionInLine}");
                    }
                    if (ArgumentPassedByReference(functionDeclaration.Type, 0))
                    {
                        if (!(callCommand.Parameter is VarParameterNode))
                        {
                            Reporter.ReportError($"{functionDeclaration.Name} expects a var parameter but was given an expression " +
                                $"at line {functionDeclaration.Position.LineNumber}, column {functionDeclaration.Position.PositionInLine}");
                        }
                    }
                    else
                    {
                        if (!(callCommand.Parameter is ExpressionParameterNode))
                        {
                            Reporter.ReportError($"{functionDeclaration.Name} expects an expression but was given a var parameter " +
                                $"at line {functionDeclaration.Position.LineNumber}, column {functionDeclaration.Position.PositionInLine}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carries out type checking on an if command node
        /// </summary>
        /// <param name="ifCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIfCommand(IfCommandNode ifCommand)
        {
            PerformTypeChecking(ifCommand.Expression);
            PerformTypeChecking(ifCommand.ThenCommand);
            PerformTypeChecking(ifCommand.ElseCommand);
            if (ifCommand.Expression.Type != StandardEnvironment.BooleanType)
            {
                Reporter.ReportError($"Condition in if command is not a boolean " +
                    $"at line {ifCommand.Position.LineNumber}, column {ifCommand.Position.PositionInLine}");
            }
        }

        /// <summary>
        /// Carries out type checking on a let command node
        /// </summary>
        /// <param name="letCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnLetCommand(LetCommandNode letCommand)
        {
            PerformTypeChecking(letCommand.Declaration);
            PerformTypeChecking(letCommand.Command);
        }

        /// <summary>
        /// Carries out type checking on a sequential command node
        /// </summary>
        /// <param name="sequentialCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnSequentialCommand(SequentialCommandNode sequentialCommand)
        {
            foreach (ICommandNode command in sequentialCommand.Commands)
                PerformTypeChecking(command);
        }

        /// <summary>
        /// Carries out type checking on a while command node
        /// </summary>
        /// <param name="whileCommand">The node to perform type checking on</param>
        private void PerformTypeCheckingOnWhileCommand(WhileCommandNode whileCommand)
        {
            PerformTypeChecking(whileCommand.Expression);
            PerformTypeChecking(whileCommand.Command);
            if (whileCommand.Expression.Type != StandardEnvironment.BooleanType)
            {
                Reporter.ReportError($"Condition in while command is not a boolean " +
                    $"at line {whileCommand.Position.LineNumber}, column {whileCommand.Position.PositionInLine}");
            }
        }



        /// <summary>
        /// Carries out type checking on a const declaration node
        /// </summary>
        /// <param name="constDeclaration"The node to perform type checking on></param>
        private void PerformTypeCheckingOnConstDeclaration(ConstDeclarationNode constDeclaration)
        {
            PerformTypeChecking(constDeclaration.Identifier);
            PerformTypeChecking(constDeclaration.Expression);
        }

        /// <summary>
        /// Carries out type checking on a sequential declaration node
        /// </summary>
        /// <param name="sequentialDeclaration">The node to perform type checking on</param>
        private void PerformTypeCheckingOnSequentialDeclaration(SequentialDeclarationNode sequentialDeclaration)
        {
            foreach (IDeclarationNode declaration in sequentialDeclaration.Declarations)
                PerformTypeChecking(declaration);
        }

        /// <summary>
        /// Carries out type checking on a var declaration node
        /// </summary>
        /// <param name="varDeclaration">The node to perform type checking on</param>
        private void PerformTypeCheckingOnVarDeclaration(VarDeclarationNode varDeclaration)
        {
            PerformTypeChecking(varDeclaration.TypeDenoter);
            PerformTypeChecking(varDeclaration.Identifier);
        }



        /// <summary>
        /// Carries out type checking on a binary expression node
        /// </summary>
        /// <param name="binaryExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnBinaryExpression(BinaryExpressionNode binaryExpression)
        {
            PerformTypeChecking(binaryExpression.Op);
            PerformTypeChecking(binaryExpression.LeftExpression);
            PerformTypeChecking(binaryExpression.RightExpression);
            if (!(binaryExpression.Op.Declaration is BinaryOperationDeclarationNode opDeclaration))
            {
                Reporter.ReportError($"{binaryExpression.Op.OperatorToken.Spelling} is being used as a binary operator but is not one " +
                    $"at line {binaryExpression.Position.LineNumber}, column {binaryExpression.Position.PositionInLine}");
            }
            else
            {
                if (GetArgumentType(opDeclaration.Type, 0) == StandardEnvironment.AnyType)
                {
                    if (binaryExpression.LeftExpression.Type != binaryExpression.RightExpression.Type)
                    {
                        Reporter.ReportError($"{opDeclaration.Name} expects arguments of the same type but received " +
                            $"{binaryExpression.LeftExpression.Type.Name} and {binaryExpression.RightExpression.Type.Name} " +
                            $"at line {binaryExpression.Position.LineNumber}, column {binaryExpression.Position.PositionInLine}");
                    }
                }
                else
                {
                    if (GetArgumentType(opDeclaration.Type, 0) != binaryExpression.LeftExpression.Type)
                    {
                        Reporter.ReportError($"{opDeclaration.Name} expects a " +
                            $"{opDeclaration.Type.Parameters[0].type.Name} on the lefthand side but was given a {binaryExpression.LeftExpression.Type.Name} " +
                            $"at line {binaryExpression.Position.LineNumber}, column {binaryExpression.Position.PositionInLine}");
                    }
                    if (GetArgumentType(opDeclaration.Type, 1) != binaryExpression.RightExpression.Type)
                    {
                        Reporter.ReportError($"{opDeclaration.Name} expects a " +
                            $"{opDeclaration.Type.Parameters[1].type.Name} on the righthand side but was given a {binaryExpression.RightExpression.Type.Name} " +
                            $"at line {binaryExpression.Position.LineNumber}, column {binaryExpression.Position.PositionInLine}");
                    }
                }
                binaryExpression.Type = GetReturnType(opDeclaration.Type);
            }
        }

        /// <summary>
        /// Carries out type checking on a character expression node
        /// </summary>
        /// <param name="characterExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnCharacterExpression(CharacterExpressionNode characterExpression)
        {
            PerformTypeChecking(characterExpression.CharLit);
            characterExpression.Type = StandardEnvironment.CharType;
        }

        /// <summary>
        /// Carries out type checking on an ID expression node
        /// </summary>
        /// <param name="idExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIdExpression(IdExpressionNode idExpression)
        {
            PerformTypeChecking(idExpression.Identifier);
            if (!(idExpression.Identifier.Declaration is IEntityDeclarationNode declaration))
            {
                Reporter.ReportError($"{idExpression.Identifier.IdentifierToken.Spelling} is not a variable or constant" +
                    $"at line {idExpression.Position.LineNumber}, column {idExpression.Position.PositionInLine}");
            }
            else
                idExpression.Type = declaration.EntityType;
        }

        /// <summary>
        /// Carries out type checking on a  node
        /// </summary>
        /// <param name="integerExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIntegerExpression(IntegerExpressionNode integerExpression)
        {
            PerformTypeChecking(integerExpression.IntLit);
            integerExpression.Type = StandardEnvironment.IntegerType;
        }

        /// <summary>
        /// Carries out type checking on a unary expression node
        /// </summary>
        /// <param name="unaryExpression">The node to perform type checking on</param>
        private void PerformTypeCheckingOnUnaryExpression(UnaryExpressionNode unaryExpression)
        {
            PerformTypeChecking(unaryExpression.Op);
            PerformTypeChecking(unaryExpression.Expression);
            if (!(unaryExpression.Op.Declaration is UnaryOperationDeclarationNode opDeclaration))
            {
                Reporter.ReportError($"{unaryExpression.Op.OperatorToken.Spelling} is being used as a unary operator but is not one " +
                    $"at line {unaryExpression.Position.LineNumber}, column {unaryExpression.Position.PositionInLine}");
            }
            else
            {
                if (GetArgumentType(opDeclaration.Type, 0) != unaryExpression.Expression.Type)
                {
                    Reporter.ReportError($"{opDeclaration.Name} expects a " +
                        $"{opDeclaration.Type.Parameters.First().type.Name} but was given a {unaryExpression.Expression.Type.Name} " +
                        $"at line {unaryExpression.Position.LineNumber}, column {unaryExpression.Position.PositionInLine}");
                }
                unaryExpression.Type = GetReturnType(opDeclaration.Type);
            }
        }



        /// <summary>
        /// Carries out type checking on a blank parameter
        /// </summary>
        /// <param name="blankParameter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnBlankParameter(BlankParameterNode blankParameter)
        {
        }

        /// <summary>
        /// Carries out type checking on an expression parameter node
        /// </summary>
        /// <param name="expressionParameter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnExpressionParameter(ExpressionParameterNode expressionParameter)
        {
            PerformTypeChecking(expressionParameter.Expression);
            expressionParameter.Type = expressionParameter.Expression.Type;
        }

        /// <summary>
        /// Carries out type checking on a var parameter node
        /// </summary>
        /// <param name="varParameter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnVarParameter(VarParameterNode varParameter)
        {
            PerformTypeChecking(varParameter.Identifier);
            if (!(varParameter.Identifier.Declaration is IVariableDeclarationNode varDeclaration))
            {
                Reporter.ReportError($"Trying to pass something which is not a variable to a function's var parameter " +
                    $"at line {varParameter.Position.LineNumber}, column {varParameter.Position.PositionInLine}" +
                    $": {varParameter.Identifier.IdentifierToken.Spelling}");
            }
            else
                varParameter.Type = varDeclaration.EntityType;
        }



        /// <summary>
        /// Carries out type checking on a type denoter node
        /// </summary>
        /// <param name="typeDenoter">The node to perform type checking on</param>
        private void PerformTypeCheckingOnTypeDenoter(TypeDenoterNode typeDenoter)
        {
            PerformTypeChecking(typeDenoter.Identifier);
            if (!(typeDenoter.Identifier.Declaration is SimpleTypeDeclarationNode declaration))
            {
                Reporter.ReportError($"Unknown type used in declaration " +
                    $"at line {typeDenoter.Position.LineNumber}, column {typeDenoter.Position.PositionInLine}" +
                    $": {typeDenoter.Identifier.IdentifierToken.Spelling}");
            }
            else
                typeDenoter.Type = declaration;
        }



        /// <summary>
        /// Carries out type checking on a character literal node
        /// </summary>
        /// <param name="characterLiteral">The node to perform type checking on</param>
        private void PerformTypeCheckingOnCharacterLiteral(CharacterLiteralNode characterLiteral)
        {
            if (characterLiteral.Value < short.MinValue || characterLiteral.Value > short.MaxValue)
            {
                Reporter.ReportError($"Value is outwith permitted range " +
                    $"at line {characterLiteral.Position.LineNumber}, column {characterLiteral.Position.PositionInLine}");
            }
        }

        /// <summary>
        /// Carries out type checking on an identifier node
        /// </summary>
        /// <param name="identifier">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIdentifier(IdentifierNode identifier)
        {
        }

        /// <summary>
        /// Carries out type checking on an integer literal node
        /// </summary>
        /// <param name="integerLiteral">The node to perform type checking on</param>
        private void PerformTypeCheckingOnIntegerLiteral(IntegerLiteralNode integerLiteral)
        {
            if (integerLiteral.Value < short.MinValue || integerLiteral.Value > short.MaxValue)
            {
                Reporter.ReportError($"Value is outwith permitted range " +
                    $"at line {integerLiteral.Position.LineNumber}, column {integerLiteral.Position.PositionInLine}");
            }
        }

        /// <summary>
        /// Carries out type checking on an operation node
        /// </summary>
        /// <param name="operation">The node to perform type checking on</param>
        private void PerformTypeCheckingOnOperator(OperatorNode operation)
        {
        }



        /// <summary>
        /// Gets the number of arguments that a function takes
        /// </summary>
        /// <param name="node">The function</param>
        /// <returns>The number of arguments taken by the function</returns>
        private static int GetNumberOfArguments(FunctionTypeDeclarationNode node)
        {
            return node.Parameters.Length;
        }

        /// <summary>
        /// Gets the type of a function's argument
        /// </summary>
        /// <param name="node">The function</param>
        /// <param name="argument">The index of the argument</param>
        /// <returns>The type of the given argument to the function</returns>
        private static SimpleTypeDeclarationNode GetArgumentType(FunctionTypeDeclarationNode node, int argument)
        {
            return node.Parameters[argument].type;
        }

        /// <summary>
        /// Gets the whether an argument to a function is passed by reference
        /// </summary>
        /// <param name="node">The function</param>
        /// <param name="argument">The index of the argument</param>
        /// <returns>True if and only if the argument is passed by reference</returns>
        private static bool ArgumentPassedByReference(FunctionTypeDeclarationNode node, int argument)
        {
            return node.Parameters[argument].byRef;
        }

        /// <summary>
        /// Gets the return type of a function
        /// </summary>
        /// <param name="node">The function</param>
        /// <returns>The return type of the function</returns>
        private static SimpleTypeDeclarationNode GetReturnType(FunctionTypeDeclarationNode node)
        {
            return node.ReturnType;
        }
    }
}
