using System;
using TuskLang.Parser.Ast;

namespace TuskLang.Parser
{
    /// <summary>
    /// Generic interface for AST visitors
    /// </summary>
    /// <typeparam name="T">The return type of the visit operation</typeparam>
    public interface IAstVisitor<T>
    {
        T VisitConfiguration(ConfigurationNode node);
        T VisitComment(CommentNode node);
        T VisitSection(SectionNode node);
        T VisitGlobalVariable(GlobalVariableNode node);
        T VisitAssignment(AssignmentNode node);
        T VisitInclude(IncludeNode node);
        T VisitLiteral(LiteralNode node);
        T VisitString(StringNode node);
        T VisitVariableReference(VariableReferenceNode node);
        T VisitBinaryOperator(BinaryOperatorNode node);
        T VisitUnaryOperator(UnaryOperatorNode node);
        T VisitTernary(TernaryNode node);
        T VisitRange(RangeNode node);
        T VisitArray(ArrayNode node);
        T VisitObject(ObjectNode node);
        T VisitNamedObject(NamedObjectNode node);
        T VisitAtOperator(AtOperatorNode node);
        T VisitCrossFileOperator(CrossFileOperatorNode node);
        T VisitPropertyAccess(PropertyAccessNode node);
        T VisitMethodCall(MethodCallNode node);
        T VisitIndexAccess(IndexAccessNode node);
        T VisitGrouping(GroupingNode node);
        T VisitExpression(ExpressionNode node);
    }
} 