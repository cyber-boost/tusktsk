using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators
{
    /// <summary>
    /// Registry for managing operators
    /// </summary>
    public class OperatorRegistry
    {
        private readonly Dictionary<string, IOperator> _operators = new Dictionary<string, IOperator>();

        public void RegisterOperator(string name, IOperator operatorInstance)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Operator name cannot be null or empty", nameof(name));

            if (operatorInstance == null)
                throw new ArgumentNullException(nameof(operatorInstance));

            _operators[name] = operatorInstance;
        }

        public IOperator GetOperator(string name)
        {
            return _operators.TryGetValue(name, out var op) ? op : null;
        }

        public bool HasOperator(string name)
        {
            return _operators.ContainsKey(name);
        }

        public IEnumerable<string> GetRegisteredOperators()
        {
            return _operators.Keys;
        }
    }

    /// <summary>
    /// Base interface for operators
    /// </summary>
    public interface IOperator
    {
        string Name { get; }
        Task<object> ExecuteAsync(object[] parameters);
    }
} 