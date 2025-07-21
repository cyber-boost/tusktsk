using System;

namespace TuskLang.Parser.Ast
{
    /// <summary>
    /// Generic AST visitor interface for operations that return values
    /// </summary>
    /// <typeparam name="T">Return type for visitor methods</typeparam>
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
    }
    
    /// <summary>
    /// Void AST visitor interface for operations that don't return values
    /// </summary>
    public interface IAstVisitor
    {
        void VisitConfiguration(ConfigurationNode node);
        void VisitComment(CommentNode node);
        void VisitSection(SectionNode node);
        void VisitGlobalVariable(GlobalVariableNode node);
        void VisitAssignment(AssignmentNode node);
        void VisitInclude(IncludeNode node);
        void VisitLiteral(LiteralNode node);
        void VisitString(StringNode node);
        void VisitVariableReference(VariableReferenceNode node);
        void VisitBinaryOperator(BinaryOperatorNode node);
        void VisitUnaryOperator(UnaryOperatorNode node);
        void VisitTernary(TernaryNode node);
        void VisitRange(RangeNode node);
        void VisitArray(ArrayNode node);
        void VisitObject(ObjectNode node);
        void VisitNamedObject(NamedObjectNode node);
        void VisitAtOperator(AtOperatorNode node);
        void VisitCrossFileOperator(CrossFileOperatorNode node);
        void VisitPropertyAccess(PropertyAccessNode node);
        void VisitMethodCall(MethodCallNode node);
        void VisitIndexAccess(IndexAccessNode node);
        void VisitGrouping(GroupingNode node);
    }
} 