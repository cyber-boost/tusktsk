using System;
using System.Collections.Generic;

namespace TuskLang.Parser.Ast
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Source { get; set; } = string.Empty;
        
        public abstract T Accept<T>(IAstVisitor<T> visitor);
    }

    public class ConfigurationNode : AstNode
    {
        public List<AstNode> Children { get; set; } = new List<AstNode>();
        public List<AstNode> Statements => Children; // Alias for compatibility
        
        public ConfigurationNode() { }
        
        public ConfigurationNode(int line = 0, int column = 0)
        {
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitConfiguration(this);
        }
        
        public void AddStatement(AstNode statement)
        {
            Children.Add(statement);
        }
        
        public void AddChild(AstNode child)
        {
            Children.Add(child);
        }
    }

    public class CommentNode : AstNode
    {
        public string Comment { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty; // Alias for compatibility
        public string Content { get; set; } = string.Empty; // Alias for compatibility
        
        public CommentNode() { }
        
        public CommentNode(string comment, int line = 0, int column = 0)
        {
            Comment = comment;
            Text = comment;
            Content = comment;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitComment(this);
        }
    }

    public class SectionNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public List<AstNode> Children { get; set; } = new List<AstNode>();
        
        public SectionNode() { }
        
        public SectionNode(string name, int line = 0, int column = 0)
        {
            Name = name;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitSection(this);
        }
    }

    public class GlobalVariableNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public AstNode? Value { get; set; }
        
        public GlobalVariableNode() { }
        
        public GlobalVariableNode(string name, AstNode? value, int line = 0, int column = 0)
        {
            Name = name;
            Value = value;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitGlobalVariable(this);
        }
    }

    public class AssignmentNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // Alias for compatibility
        public AstNode? Value { get; set; }
        
        public AssignmentNode() { }
        
        public AssignmentNode(string name, AstNode? value, int line = 0, int column = 0)
        {
            Name = name;
            Key = name;
            Value = value;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitAssignment(this);
        }
    }

    public class IncludeNode : AstNode
    {
        public string FilePath { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty; // Alias for compatibility
        public bool IsImport { get; set; } = false;
        
        public IncludeNode() { }
        
        public IncludeNode(string filePath, bool isImport = false, int line = 0, int column = 0)
        {
            FilePath = filePath;
            Path = filePath;
            IsImport = isImport;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitInclude(this);
        }
    }

    public class LiteralNode : ExpressionNode
    {
        public object? Value { get; set; }
        
        public LiteralNode() { }
        
        public LiteralNode(object? value, int line = 0, int column = 0)
        {
            Value = value;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitLiteral(this);
        }
    }

    public class StringNode : ExpressionNode
    {
        public string Value { get; set; } = string.Empty;
        public bool IsTemplate { get; set; } = false;
        
        public StringNode() { }
        
        public StringNode(string value, bool isTemplate = false, int line = 0, int column = 0)
        {
            Value = value;
            IsTemplate = isTemplate;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitString(this);
        }
    }

    public class VariableReferenceNode : ExpressionNode
    {
        public string Name { get; set; } = string.Empty;
        public bool IsGlobal { get; set; } = false;
        
        public VariableReferenceNode() { }
        
        public VariableReferenceNode(string name, bool isGlobal = false, int line = 0, int column = 0)
        {
            Name = name;
            IsGlobal = isGlobal;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitVariableReference(this);
        }
    }

    public class BinaryOperatorNode : ExpressionNode
    {
        public string Operator { get; set; } = string.Empty;
        public AstNode? Left { get; set; }
        public AstNode? Right { get; set; }
        
        public BinaryOperatorNode() { }
        
        public BinaryOperatorNode(string op, AstNode? left, AstNode? right, int line = 0, int column = 0)
        {
            Operator = op;
            Left = left;
            Right = right;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitBinaryOperator(this);
        }
    }

    public class UnaryOperatorNode : ExpressionNode
    {
        public string Operator { get; set; } = string.Empty;
        public AstNode? Operand { get; set; }
        public AstNode? Expression { get; set; } // Alias for compatibility
        
        public UnaryOperatorNode() { }
        
        public UnaryOperatorNode(string op, AstNode? operand, int line = 0, int column = 0)
        {
            Operator = op;
            Operand = operand;
            Expression = operand;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitUnaryOperator(this);
        }
    }

    public class TernaryNode : ExpressionNode
    {
        public AstNode? Condition { get; set; }
        public AstNode? TrueValue { get; set; }
        public AstNode? FalseValue { get; set; }
        
        public TernaryNode() { }
        
        public TernaryNode(AstNode? condition, AstNode? trueValue, AstNode? falseValue, int line = 0, int column = 0)
        {
            Condition = condition;
            TrueValue = trueValue;
            FalseValue = falseValue;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitTernary(this);
        }
    }

    public class RangeNode : ExpressionNode
    {
        public AstNode? Start { get; set; }
        public AstNode? End { get; set; }
        public AstNode? Step { get; set; }
        
        public RangeNode() { }
        
        public RangeNode(AstNode? start, AstNode? end, int line = 0, int column = 0)
        {
            Start = start;
            End = end;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitRange(this);
        }
    }

    public class ArrayNode : ExpressionNode
    {
        public List<AstNode> Elements { get; set; } = new List<AstNode>();
        
        public ArrayNode()
        {
        }
        
        public ArrayNode(List<AstNode> elements)
        {
            Elements = elements;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitArray(this);
        }
    }

    public class ObjectNode : ExpressionNode
    {
        public Dictionary<string, AstNode> Properties { get; set; } = new Dictionary<string, AstNode>();
        
        public ObjectNode()
        {
        }
        
        public ObjectNode(Dictionary<string, AstNode> properties)
        {
            Properties = properties;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitObject(this);
        }
    }

    public class NamedObjectNode : ExpressionNode
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, AstNode> Properties { get; set; } = new Dictionary<string, AstNode>();
        
        public NamedObjectNode()
        {
        }
        
        public NamedObjectNode(string name, Dictionary<string, AstNode> properties)
        {
            Name = name;
            Properties = properties;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitNamedObject(this);
        }
    }

    public class AtOperatorNode : ExpressionNode
    {
        public string Operator { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty; // Alias for compatibility
        public AstNode? Operand { get; set; }
        public AstNode[] Arguments { get; set; } = Array.Empty<AstNode>(); // For compatibility
        
        public AtOperatorNode()
        {
        }
        
        public AtOperatorNode(string operatorName, AstNode[] arguments)
        {
            Operator = operatorName;
            OperatorName = operatorName;
            Arguments = arguments;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitAtOperator(this);
        }
    }

    public class CrossFileOperatorNode : AtOperatorNode
    {
        public string FilePath { get; set; } = string.Empty;
        public string PropertyPath { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty; // For compatibility
        
        public CrossFileOperatorNode()
        {
        }
        
        public CrossFileOperatorNode(string fileName, string methodName, AstNode[] arguments)
        {
            FilePath = fileName;
            MethodName = methodName;
            Arguments = arguments;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitCrossFileOperator(this);
        }
    }

    public class PropertyAccessNode : ExpressionNode
    {
        public AstNode? Object { get; set; }
        public string Property { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty; // Alias for compatibility
        
        public PropertyAccessNode() { }
        
        public PropertyAccessNode(AstNode? obj, string property, int line = 0, int column = 0)
        {
            Object = obj;
            Property = property;
            PropertyName = property;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitPropertyAccess(this);
        }
    }

    public class MethodCallNode : ExpressionNode
    {
        public AstNode? Object { get; set; }
        public string Method { get; set; } = string.Empty;
        public List<AstNode> Arguments { get; set; } = new List<AstNode>();
        
        public MethodCallNode() { }
        
        public MethodCallNode(AstNode? obj, string method, int line = 0, int column = 0)
        {
            Object = obj;
            Method = method;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitMethodCall(this);
        }
    }

    public class IndexAccessNode : ExpressionNode
    {
        public AstNode? Object { get; set; }
        public AstNode? Index { get; set; }
        
        public IndexAccessNode() { }
        
        public IndexAccessNode(AstNode? obj, AstNode? index, int line = 0, int column = 0)
        {
            Object = obj;
            Index = index;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitIndexAccess(this);
        }
    }

    public class GroupingNode : ExpressionNode
    {
        public AstNode? Expression { get; set; }
        
        public GroupingNode() { }
        
        public GroupingNode(AstNode? expression, int line = 0, int column = 0)
        {
            Expression = expression;
            Line = line;
            Column = column;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitGrouping(this);
        }
    }

    public class ExpressionNode : AstNode
    {
        public AstNode? Expression { get; set; }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }
    }
} 