#nullable enable
using Flee.InternalTypes;
using Flee.Resources;
using System.ComponentModel;
using System.Reflection;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Manages the variables available to an expression
    /// </summary>
    /// <remarks>
    /// Use this class to manage the variables that an expression can use.  Variable names are matched as
    /// <see cref="ExpressionOptions.CaseSensitive"/> says.
    /// </remarks>
    public sealed class VariableCollection : IDictionary<string, object?>
    {
        // Set by CreateDictionary, which the constructor calls.
        private IDictionary<string, IVariable> _variables = null!;
        private readonly ExpressionContext _context;

        /// <summary>
        /// Occurs when an expression needs the type of a variable.
        /// </summary>
        /// <remarks>
        /// This event is raised when an expression references a variable that doesn't exist in its variable collection.  You can handle this event to provide on-demand variables.
        /// It is raised while the expression is compiled.
        /// </remarks>
        public event EventHandler<ResolveVariableTypeEventArgs>? ResolveVariableType;

        /// <summary>
        /// Occurs when an expression needs the value of a variable.
        /// </summary>
        /// <remarks>
        /// This event is raised when an expression references a variable that doesn't exist in its variable collection.  You can handle this event to provide on-demand variables.
        /// It is raised each time the expression is evaluated.
        /// </remarks>
        public event EventHandler<ResolveVariableValueEventArgs>? ResolveVariableValue;

        /// <summary>
        /// Occurs when an expression needs the return type of a function.
        /// </summary>
        /// <remarks>
        /// This event is raised when an expression references a function that doesn't exist on the expression owner or imports.  By handling this event and providing a value for the <see cref="ResolveFunctionEventArgs.ReturnType"/> property, you can implement an on-demand function.
        /// </remarks>
        public event EventHandler<ResolveFunctionEventArgs>? ResolveFunction;

        /// <summary>
        /// Occurs when an expression needs the return value of a function.
        /// </summary>
        /// <remarks>
        /// This event is raised when an expression needs the return value of an on-demand function.  By handling this event and providing a value for the <see cref="InvokeFunctionEventArgs.Result"/> property, you can invoke your on-demand function.
        /// </remarks>
        public event EventHandler<InvokeFunctionEventArgs>? InvokeFunction;

        internal VariableCollection(ExpressionContext context)
        {
            _context = context;
            this.CreateDictionary();
            this.HookOptions();
        }

        #region "Methods - Non Public"

        private void HookOptions()
        {
            _context.Options.CaseSensitiveChanged += OnOptionsCaseSensitiveChanged;
        }

        private void CreateDictionary()
        {
            _variables = new Dictionary<string, IVariable>(_context.Options.StringComparer);
        }

        private void OnOptionsCaseSensitiveChanged(object? sender, EventArgs e)
        {
            this.CreateDictionary();
        }

        internal void Copy(VariableCollection dest)
        {
            dest.CreateDictionary();
            dest.HookOptions();

            foreach (KeyValuePair<string, IVariable> pair in _variables)
            {
                IVariable copyVariable = pair.Value.Clone();
                dest._variables.Add(pair.Key, copyVariable);
            }
        }

        internal void DefineVariableInternal(string name, Type variableType, object? variableValue)
        {
            Utility.AssertNotNull(variableType, "variableType");

            if (_variables.ContainsKey(name))
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.VariableWithNameAlreadyDefined, name);
                throw new ArgumentException(msg);
            }

            IVariable v = this.CreateVariable(variableType, variableValue);
            if (variableValue == null)
            {
                // DefineVariable: start with the type's default, as the indexer does for null (D-27).
                // Done here rather than in GenericVariable<T>, whose instances the on-demand path
                // creates on every read (R-061).
                v.ValueAsObject = null;
            }
            _variables.Add(name, v);
        }

        internal Type? GetVariableTypeInternal(string name)
        {
            IVariable? value = null;
            bool success = _variables.TryGetValue(name, out value);

            if (success)
            {
                // TryGetValue succeeded.
                return value!.VariableType;
            }

            ResolveVariableTypeEventArgs args = new ResolveVariableTypeEventArgs(name);
            ResolveVariableType?.Invoke(this, args);

            return args.VariableType;
        }

        private IVariable? GetVariable(string name, bool throwOnNotFound)
        {
            IVariable? value = null;
            bool success = _variables.TryGetValue(name, out value);

            if (!success && throwOnNotFound)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.UndefinedVariable, name);
                throw new ArgumentException(msg);
            }
            else
            {
                return value;
            }
        }

        private IVariable CreateVariable(Type variableValueType, object? variableValue)
        {
            Type? variableType = default(Type);

            // Is the variable value an expression?
            IExpression? expression = variableValue as IExpression;
            ExpressionOptions? options = null;

            if (expression != null)
            {
                options = expression.Context.Options;
                // Get its result type
                // A compiled expression's own options always have a result type (Expression.Compile sets it).
                variableValueType = options.ResultType!;

                // Create a variable that wraps the expression

                if (!options.IsGeneric)
                {
                    variableType = typeof(DynamicExpressionVariable<>);
                }
                else
                {
                    variableType = typeof(GenericExpressionVariable<>);
                }
            }
            else
            {
                // Create a variable for a regular value
                _context.AssertTypeIsAccessible(variableValueType);
                variableType = typeof(GenericVariable<>);
            }

            // Create the generic variable instance
            variableType = variableType.MakeGenericType(variableValueType);
            // CreateInstance returns null only for Nullable<T>; variableType is one of Flee's variable classes.
            IVariable v = (IVariable)Activator.CreateInstance(variableType)!;

            return v;
        }

        internal Type? ResolveOnDemandFunction(string name, Type[] argumentTypes)
        {
            ResolveFunctionEventArgs args = new ResolveFunctionEventArgs(name, argumentTypes);
            ResolveFunction?.Invoke(this, args);
            return args.ReturnType;
        }

        private static T? ReturnGenericValue<T>(object? value)
        {
            if (value == null)
            {
                return default(T);
            }
            else
            {
                return (T)value;
            }
        }

        private static void ValidateSetValueType(Type requiredType, object? value)
        {
            if (value == null)
            {
                // Can always assign null value
                return;
            }

            Type valueType = value.GetType();

            if (!requiredType.IsAssignableFrom(valueType))
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.VariableValueNotAssignableToType, valueType.Name, requiredType.Name);
                throw new ArgumentException(msg);
            }
        }

        internal static MethodInfo GetVariableLoadMethod(Type variableType)
        {
            // The method is declared on this class.
            MethodInfo mi = typeof(VariableCollection).GetMethod("GetVariableValueInternal", BindingFlags.Public | BindingFlags.Instance)!;
            mi = mi.MakeGenericMethod(variableType);
            return mi;
        }

        internal static MethodInfo GetFunctionInvokeMethod(Type returnType)
        {
            // The method is declared on this class.
            MethodInfo mi = typeof(VariableCollection).GetMethod("GetFunctionResultInternal", BindingFlags.Public | BindingFlags.Instance)!;
            mi = mi.MakeGenericMethod(returnType);
            return mi;
        }

        internal static MethodInfo GetVirtualPropertyLoadMethod(Type returnType)
        {
            // The method is declared on this class.
            MethodInfo mi = typeof(VariableCollection).GetMethod("GetVirtualPropertyValueInternal", BindingFlags.Public | BindingFlags.Instance)!;
            mi = mi.MakeGenericMethod(returnType);
            return mi;
        }

        private Dictionary<string, object?> GetNameValueDictionary()
        {
            Dictionary<string, object?> dict = new Dictionary<string, object?>();

            foreach (KeyValuePair<string, IVariable> pair in _variables)
            {
                dict.Add(pair.Key, pair.Value.ValueAsObject);
            }

            return dict;
        }

        #endregion "Methods - Non Public"

        #region "Methods - Public"

        /// <summary>
        /// Gets the type of a variable.
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>The type of the variable's value</returns>
        /// <remarks>Use this method to get the type of the value of a variable.</remarks>
        /// <exception cref="ArgumentException">No variable with the given name is defined.</exception>
        public Type GetVariableType(string name)
        {
            // With throwOnNotFound set, GetVariable throws instead of returning null.
            IVariable v = this.GetVariable(name, true)!;
            return v.VariableType;
        }

        /// <summary>
        /// Defines a variable with a specific type.
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="variableType">The type of the new variable</param>
        /// <remarks>
        /// Use this method when you want to add a variable with a type that is different than what would be inferred from defining it using the indexer.
        /// The new variable starts with the default value of its type (null for reference types), as if it had been set to null with the indexer.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// A variable with the given name is already defined -or- <paramref name="variableType"/> is not accessible to the context's expressions.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="variableType"/> is <see langword="null"/>.</exception>
        public void DefineVariable(string name, Type variableType)
        {
            this.DefineVariableInternal(name, variableType, null);
        }

        // Called from generated IL. NoInlining keeps the .NET 10 JIT from inlining this method into
        // every compiled expression, which made each expression's first call cost about 1.4 ms (R-019).
        /// <summary>Gets the value of a variable</summary>
        /// <typeparam name="T">The type of the variable's value</typeparam>
        /// <param name="name">The name of the variable</param>
        /// <returns>The variable's value</returns>
        /// <remarks>
        /// This method is used by the expression to retrieve the values of variables during evaluation.  It must be public so that all expressions
        /// can access it.  It is meant for internal use and you shouldn't depend on any of its functionality.
        /// </remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public T GetVariableValueInternal<T>(string name)
        {
            if (_variables.TryGetValue(name, out IVariable? variable))
            {
                if (variable is IGenericVariable<T> generic)
                {
                    return (T)generic.GetValue();
                }
            }

            GenericVariable<T> result = new GenericVariable<T>();
            GenericVariable<T> vTemp = new GenericVariable<T>();
            ResolveVariableValueEventArgs args = new ResolveVariableValueEventArgs(name, typeof(T));
            ResolveVariableValue?.Invoke(this, args);

            ValidateSetValueType(typeof(T), args.VariableValue);
            vTemp.ValueAsObject = args.VariableValue;
            result = vTemp;
            return (T)result.GetValue();
        }

        // Called from generated IL. NoInlining keeps the .NET 10 JIT from inlining this method into
        // every compiled expression, which made each expression's first call cost about 1.4 ms (R-019).
        /// <summary>Gets the result of a virtual property</summary>
        /// <typeparam name="T">The type of the result's value</typeparam>
        /// <param name="name">The name of the property</param>
        /// <param name="component">The object whose property value to get</param>
        /// <returns>The property's value</returns>
        /// <remarks>
        /// This method is used by the expression to retrieve the values of virtual properties during evaluation.  It must be public so that all expressions
        /// can access it.  It is meant for internal use and you shouldn't depend on any of its functionality.
        /// </remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public T? GetVirtualPropertyValueInternal<T>(string name, object component)
        {
            PropertyDescriptorCollection coll = TypeDescriptor.GetProperties(component);
            PropertyDescriptor? pd = coll.Find(name, true);

            // The compiler emits this call only for a property it found through TypeDescriptor.
            object? value = pd!.GetValue(component);
            ValidateSetValueType(typeof(T), value);
            return ReturnGenericValue<T>(value);
        }

        // Called from generated IL. NoInlining keeps the .NET 10 JIT from inlining this method into
        // every compiled expression, which made each expression's first call cost about 1.4 ms (R-019).
        /// <summary>Gets the result of an on-demand function</summary>
        /// <typeparam name="T">The type of the result's value</typeparam>
        /// <param name="name">The name of the function</param>
        /// <param name="arguments">The values of the function's arguments</param>
        /// <returns>The function's result</returns>
        /// <remarks>
        /// This method is used by the expression to retrieve the values of on-demand functions during evaluation.  It must be public so that all expressions
        /// can access it.  It is meant for internal use and you shouldn't depend on any of its functionality.
        /// </remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public T? GetFunctionResultInternal<T>(string name, object?[] arguments)
        {
            InvokeFunctionEventArgs args = new InvokeFunctionEventArgs(name, arguments);
            if (InvokeFunction != null)
            {
                InvokeFunction(this, args);
            }

            object? result = args.Result;
            ValidateSetValueType(typeof(T), result);

            return ReturnGenericValue<T>(result);
        }

        #endregion "Methods - Public"

        #region "IDictionary Implementation"

        private void Add1(System.Collections.Generic.KeyValuePair<string, object?> item)
        {
            this.Add(item.Key, item.Value);
        }

        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Add(System.Collections.Generic.KeyValuePair<string, object?> item)
        {
            Add1(item);
        }

        /// <summary>Removes all variables from the collection.</summary>
        /// <remarks>Use this method to remove all variables from the collection</remarks>
        public void Clear()
        {
            _variables.Clear();
        }

        private bool Contains1(System.Collections.Generic.KeyValuePair<string, object?> item)
        {
            return this.ContainsKey(item.Key);
        }

        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Contains(System.Collections.Generic.KeyValuePair<string, object?> item)
        {
            return Contains1(item);
        }

        private void CopyTo(System.Collections.Generic.KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            Dictionary<string, object?> dict = this.GetNameValueDictionary();
            ICollection<KeyValuePair<string, object?>> coll = dict;
            coll.CopyTo(array, arrayIndex);
        }

        private bool Remove1(System.Collections.Generic.KeyValuePair<string, object?> item)
        {
            return this.Remove(item.Key);
        }

        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>.Remove(System.Collections.Generic.KeyValuePair<string, object?> item)
        {
            return Remove1(item);
        }

        /// <summary>Adds a variable to the collection.</summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
        /// <remarks>
        /// Use this method to add a variable to the collection.  The variable's type is the type of <paramref name="value"/>.
        /// If the value is an expression (<see cref="IExpression"/>), the variable's type is the expression's result type, and an
        /// expression that reads the variable evaluates that expression.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// A variable with the given name is already defined -or- the type of <paramref name="value"/> is not accessible to the context's expressions.
        /// </exception>
        public void Add(string name, object? value)
        {
            Utility.AssertNotNull(value, "value");
            // AssertNotNull above throws for null.
            this.DefineVariableInternal(name, value!.GetType(), value);
            this[name] = value;
        }

        /// <summary>Determines if the collection contains a variable.</summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>True if the collection has a variable with the given name; False otherwise</returns>
        /// <remarks>Use this method to determine if the collection contains a variable</remarks>
        public bool ContainsKey(string name)
        {
            return _variables.ContainsKey(name);
        }

        /// <summary>Removes a variable from the collection.</summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>True if the variable was found and removed; False otherwise</returns>
        /// <remarks>Use this method to remove a variable from the collection</remarks>
        public bool Remove(string name)
        {
            return _variables.Remove(name);
        }

        /// <summary>Gets the value of a variable in the collection.</summary>
        /// <param name="key">The name of the variable</param>
        /// <param name="value">The location to store the value of the variable</param>
        /// <returns>True if the collection contains a variable with the given name; False otherwise</returns>
        /// <remarks>Use this method to get the value of a variable in the collection</remarks>
        public bool TryGetValue(string key, out object? value)
        {
            IVariable? v = this.GetVariable(key, false);
            value = v?.ValueAsObject;
            return v != null;
        }

        /// <summary>Returns an enumerator over the names and values of all variables.</summary>
        /// <returns>An enumerator over a snapshot of the variables taken when this method is called.</returns>
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator()
        {
            Dictionary<string, object?> dict = this.GetNameValueDictionary();
            return dict.GetEnumerator();
        }

        private System.Collections.IEnumerator GetEnumerator1()
        {
            return this.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }

        /// <summary>Gets the number of variables defined in the collection.</summary>
        /// <value>The number of variables in the collection</value>
        /// <remarks>Use this property to get a count of the number of variables in the collection</remarks>
        public int Count => _variables.Count;

        /// <summary>Gets a value indicating whether the collection is read-only.</summary>
        /// <value>Always <see langword="false"/>.</value>
        public bool IsReadOnly => false;

        /// <summary>Gets or sets the value of a variable.</summary>
        /// <param name="name">The name of the variable</param>
        /// <value>The value of the variable</value>
        /// <remarks>
        /// Use this property to get or set the value of a variable.  If a variable with the given name does not exist, a new variable will be defined
        /// (see <see cref="Add(string, object)"/>).  Otherwise, the value of the existing variable will be overwritten.
        /// <para>
        /// The new value of an existing variable is not checked against the variable's type: the variable keeps its type, and an
        /// expression that reads it fails when it is evaluated, for example with an <see cref="InvalidCastException"/>.  Setting an
        /// existing variable that holds a plain value to <see langword="null"/> stores the default value of its type.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Getting a variable that is not defined.</exception>
        /// <exception cref="ArgumentNullException">Defining a new variable with a <see langword="null"/> value.</exception>
        public object? this[string name]
        {
            get
            {
                // With throwOnNotFound set, GetVariable throws instead of returning null.
                IVariable v = this.GetVariable(name, true)!;
                return v.ValueAsObject;
            }
            set
            {
                IVariable? v = null;

                if (_variables.TryGetValue(name, out v))
                {
                    v.ValueAsObject = value;
                }
                else
                {
                    this.Add(name, value);
                }
            }
        }

        /// <summary>Gets a collection with the names of all variables.</summary>
        /// <value>A collection with all the names</value>
        /// <remarks>Use this property to access all the variable names in the collection</remarks>
        public System.Collections.Generic.ICollection<string> Keys => _variables.Keys;

        /// <summary>Gets a collection with the values of all variables.</summary>
        /// <value>A collection with all the values, taken when the property is read</value>
        /// <remarks>Use this property to access all the variable values in the collection</remarks>
        public System.Collections.Generic.ICollection<object?> Values
        {
            get
            {
                Dictionary<string, object?> dict = this.GetNameValueDictionary();
                return dict.Values;
            }
        }

        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        #endregion "IDictionary Implementation"
    }
}