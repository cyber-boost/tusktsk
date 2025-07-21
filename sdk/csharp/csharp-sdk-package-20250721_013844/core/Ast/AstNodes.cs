using System;
using System.Collections.Generic;
using System.Linq;

namespace TuskLang.Parser.Ast
{
    /// <summary>
    /// Base AST node class
    /// </summary>
    public abstract class AstNode
    {
        public int Line { get; }
        
        protected AstNode(int line)
        {
            Line = line;
        }
        
        public abstract T Accept<T>(IAstVisitor<T> visitor);
        public abstract void Accept(IAstVisitor visitor);
    }
    
    /// <summary>
    /// Base expression node
    /// </summary>
    public abstract class ExpressionNode : AstNode
    {
        protected ExpressionNode(int line) : base(line) { }
    }
    
    /// <summary>
    /// Base statement node
    /// </summary>
    public abstract class StatementNode : AstNode
    {
        protected StatementNode(int line) : base(line) { }
    }
    
    /// <summary>
    /// Configuration root node
    /// </summary>
    public class ConfigurationNode : AstNode
    {
        public List<AstNode> Statements { get; }
        
        public ConfigurationNode() : base(1)
        {
            Statements = new List<AstNode>();
        }
        
        public void AddStatement(AstNode statement)
        {
            Statements.Add(statement);
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitConfiguration(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitConfiguration(this);
        }
    }
    
    /// <summary>
    /// Comment node
    /// </summary>
    public class CommentNode : StatementNode
    {
        public string Text { get; }
        
        public CommentNode(string text, int line) : base(line)
        {
            Text = text;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitComment(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitComment(this);
        }
    }
    
    /// <summary>
    /// Section declaration node: [section_name]
    /// </summary>
    public class SectionNode : StatementNode
    {
        public string Name { get; }
        
        public SectionNode(string name, int line) : base(line)
        {
            Name = name;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitSection(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitSection(this);
        }
    }
    
    /// <summary>
    /// Global variable node: $var: value
    /// </summary>
    public class GlobalVariableNode : StatementNode
    {
        public string Name { get; }
        public ExpressionNode Value { get; }
        
        public GlobalVariableNode(string name, ExpressionNode value, int line) : base(line)
        {
            Name = name;
            Value = value;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitGlobalVariable(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitGlobalVariable(this);
        }
    }
    
    /// <summary>
    /// Assignment node: key: value
    /// </summary>
    public class AssignmentNode : StatementNode
    {
        public string Key { get; }
        public ExpressionNode Value { get; }
        
        public AssignmentNode(string key, ExpressionNode value, int line) : base(line)
        {
            Key = key;
            Value = value;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitAssignment(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }
    }
    
    /// <summary>
    /// Include/import node
    /// </summary>
    public class IncludeNode : StatementNode
    {
        public ExpressionNode Path { get; }
        public bool IsImport { get; }
        
        public IncludeNode(ExpressionNode path, bool isImport, int line) : base(line)
        {
            Path = path;
            IsImport = isImport;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitInclude(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitInclude(this);
        }
    }
    
    /// <summary>
    /// Literal value node
    /// </summary>
    public class LiteralNode : ExpressionNode
    {
        public object Value { get; }
        
        public LiteralNode(object value, int line) : base(line)
        {
            Value = value;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitLiteral(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitLiteral(this);
        }
    }
    
    /// <summary>
    /// String literal node with template support
    /// </summary>
    public class StringNode : ExpressionNode
    {
        public string Value { get; }
        public bool IsTemplate { get; }
        
        public StringNode(string value, bool isTemplate, int line) : base(line)
        {
            Value = value;
            IsTemplate = isTemplate;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitString(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitString(this);
        }
    }
    
    /// <summary>
    /// Variable reference node
    /// </summary>
    public class VariableReferenceNode : ExpressionNode
    {
        public string Name { get; }
        public bool IsGlobal { get; }
        
        public VariableReferenceNode(string name, bool isGlobal, int line) : base(line)
        {
            Name = name;
            IsGlobal = isGlobal;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitVariableReference(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitVariableReference(this);
        }
    }
    
    /// <summary>
    /// Binary operator node: left op right
    /// </summary>
    public class BinaryOperatorNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public string Operator { get; }
        public ExpressionNode Right { get; }
        
        public BinaryOperatorNode(ExpressionNode left, string operatorSymbol, ExpressionNode right, int line) : base(line)
        {
            Left = left;
            Operator = operatorSymbol;
            Right = right;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitBinaryOperator(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitBinaryOperator(this);
        }
    }
    
    /// <summary>
    /// Unary operator node: op expression
    /// </summary>
    public class UnaryOperatorNode : ExpressionNode
    {
        public string Operator { get; }
        public ExpressionNode Expression { get; }
        
        public UnaryOperatorNode(string operatorSymbol, ExpressionNode expression, int line) : base(line)
        {
            Operator = operatorSymbol;
            Expression = expression;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitUnaryOperator(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitUnaryOperator(this);
        }
    }
    
    /// <summary>
    /// Ternary conditional node: condition ? true_expr : false_expr
    /// </summary>
    public class TernaryNode : ExpressionNode
    {
        public ExpressionNode Condition { get; }
        public ExpressionNode TrueExpression { get; }
        public ExpressionNode FalseExpression { get; }
        
        public TernaryNode(ExpressionNode condition, ExpressionNode trueExpr, ExpressionNode falseExpr, int line) : base(line)
        {
            Condition = condition;
            TrueExpression = trueExpr;
            FalseExpression = falseExpr;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitTernary(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitTernary(this);
        }
    }
    
    /// <summary>
    /// Range node: start-end
    /// </summary>
    public class RangeNode : ExpressionNode
    {
        public ExpressionNode Start { get; }
        public ExpressionNode End { get; }
        
        public RangeNode(ExpressionNode start, ExpressionNode end, int line) : base(line)
        {
            Start = start;
            End = end;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitRange(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitRange(this);
        }
    }
    
    /// <summary>
    /// Array node: [item1, item2, item3]
    /// </summary>
    public class ArrayNode : ExpressionNode
    {
        public List<ExpressionNode> Elements { get; }
        
        public ArrayNode(List<ExpressionNode> elements, int line) : base(line)
        {
            Elements = elements;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitArray(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitArray(this);
        }
    }
    
    /// <summary>
    /// Object node: { key: value, key2: value2 }
    /// </summary>
    public class ObjectNode : ExpressionNode
    {
        public Dictionary<string, ExpressionNode> Properties { get; }
        
        public ObjectNode(Dictionary<string, ExpressionNode> properties, int line) : base(line)
        {
            Properties = properties;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitObject(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitObject(this);
        }
    }
    
    /// <summary>
    /// Named object node: name > key: value &lt;
    /// </summary>
    public class NamedObjectNode : ExpressionNode
    {
        public string Name { get; }
        public Dictionary<string, ExpressionNode> Properties { get; }
        
        public NamedObjectNode(string name, Dictionary<string, ExpressionNode> properties, int line) : base(line)
        {
            Name = name;
            Properties = properties;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitNamedObject(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitNamedObject(this);
        }
    }
    
    /// <summary>
    /// @ operator node: @operator(args)
    /// </summary>
    public class AtOperatorNode : ExpressionNode
    {
        public string OperatorName { get; }
        public ExpressionNode[] Arguments { get; }
        
        public AtOperatorNode(string operatorName, ExpressionNode[] arguments, int line) : base(line)
        {
            OperatorName = operatorName;
            Arguments = arguments;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitAtOperator(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitAtOperator(this);
        }
    }
    
    /// <summary>
    /// Cross-file operator node: @file.tsk.method(args)
    /// </summary>
    public class CrossFileOperatorNode : ExpressionNode
    {
        public string FileName { get; }
        public string MethodName { get; }
        public ExpressionNode[] Arguments { get; }
        
        public CrossFileOperatorNode(string fileName, string methodName, ExpressionNode[] arguments, int line) : base(line)
        {
            FileName = fileName;
            MethodName = methodName;
            Arguments = arguments;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitCrossFileOperator(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitCrossFileOperator(this);
        }
    }
    
    /// <summary>
    /// Property access node: object.property
    /// </summary>
    public class PropertyAccessNode : ExpressionNode
    {
        public ExpressionNode Object { get; }
        public string PropertyName { get; }
        
        public PropertyAccessNode(ExpressionNode objectExpr, string propertyName, int line) : base(line)
        {
            Object = objectExpr;
            PropertyName = propertyName;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitPropertyAccess(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitPropertyAccess(this);
        }
    }
    
    /// <summary>
    /// Method call node: object.method(args)
    /// </summary>
    public class MethodCallNode : ExpressionNode
    {
        public ExpressionNode Object { get; }
        public ExpressionNode[] Arguments { get; }
        
        public MethodCallNode(ExpressionNode objectExpr, ExpressionNode[] arguments, int line) : base(line)
        {
            Object = objectExpr;
            Arguments = arguments;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitMethodCall(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitMethodCall(this);
        }
    }
    
    /// <summary>
    /// Index access node: array[index]
    /// </summary>
    public class IndexAccessNode : ExpressionNode
    {
        public ExpressionNode Object { get; }
        public ExpressionNode Index { get; }
        
        public IndexAccessNode(ExpressionNode objectExpr, ExpressionNode index, int line) : base(line)
        {
            Object = objectExpr;
            Index = index;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitIndexAccess(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitIndexAccess(this);
        }
    }
    
    /// <summary>
    /// Grouping node: (expression)
    /// </summary>
    public class GroupingNode : ExpressionNode
    {
        public ExpressionNode Expression { get; }
        
        public GroupingNode(ExpressionNode expression, int line) : base(line)
        {
            Expression = expression;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitGrouping(this);
        }
        
        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitGrouping(this);
        }
    }
} 