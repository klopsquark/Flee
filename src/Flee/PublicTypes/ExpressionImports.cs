#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Holds all the types whose static members can be used in an expression.
    /// </summary>
    /// <remarks>
    /// Use this class to allow the static functions, methods, and properties of a type
    /// to be used in an expression.  By default, no types are imported.
    /// </remarks>
    /// <example>
    /// This example shows how to use this class to let an expression use all the static members of the Math class:
    /// <code lang="C#">
    /// // Define the context of our expression
    /// ExpressionContext context = new ExpressionContext();
    /// // Import all members of the Math type into the default namespace
    /// context.Imports.AddType(typeof(Math));
    /// </code>
    /// </example>
    public sealed class ExpressionImports
    {

        private static Dictionary<string, Type> _builtinTypeMap = CreateBuiltinTypeMap();
        private NamespaceImport _rootImport;
        private TypeImport? _ownerImport;

        // Set by SetContext right after construction (ExpressionContext constructor and CloneInternal).
        private ExpressionContext? _context;
        internal ExpressionImports()
        {
            _rootImport = new NamespaceImport("true");
        }

        private static Dictionary<string, Type> CreateBuiltinTypeMap()
        {
            Dictionary<string, Type> map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            map.Add("boolean", typeof(bool));
            map.Add("byte", typeof(byte));
            map.Add("sbyte", typeof(sbyte));
            map.Add("short", typeof(short));
            map.Add("ushort", typeof(UInt16));
            map.Add("int", typeof(Int32));
            map.Add("uint", typeof(UInt32));
            map.Add("long", typeof(long));
            map.Add("ulong", typeof(ulong));
            map.Add("single", typeof(float));
            map.Add("double", typeof(double));
            map.Add("decimal", typeof(decimal));
            map.Add("char", typeof(char));
            map.Add("object", typeof(object));
            map.Add("string", typeof(string));

            return map;
        }

        #region "Methods - Non public"
        internal void SetContext(ExpressionContext context)
        {
            _context = context;
            _rootImport.SetContext(context);
        }

        internal ExpressionImports Clone()
        {
            ExpressionImports copy = new ExpressionImports();

            copy._rootImport = (NamespaceImport)_rootImport.Clone();
            copy._ownerImport = _ownerImport;

            return copy;
        }

        internal void ImportOwner(Type ownerType)
        {
            _ownerImport = new TypeImport(ownerType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, false);
            _ownerImport.SetContext(_context!);
        }

        internal bool HasNamespace(string ns)
        {
            NamespaceImport? import = _rootImport.FindImport(ns) as NamespaceImport;
            return (import != null);
        }

        internal NamespaceImport GetImport(string ns)
        {
            if (ns.Length == 0)
            {
                return _rootImport;
            }

            NamespaceImport? import = _rootImport.FindImport(ns) as NamespaceImport;

            if (import == null)
            {
                import = new NamespaceImport(ns);
                _rootImport.Add(import);
            }

            return import;
        }

        internal MemberInfo[] FindOwnerMembers(string memberName, System.Reflection.MemberTypes memberType)
        {
            // The expression calls ImportOwner before it compiles, and only the compiler calls this.
            return _ownerImport!.FindMembers(memberName, memberType);
        }

        internal Type? FindType(string[] typeNameParts)
        {
            string[] namespaces = new string[typeNameParts.Length - 1];
            string typeName = typeNameParts[typeNameParts.Length - 1];

            System.Array.Copy(typeNameParts, namespaces, namespaces.Length);
            ImportBase? currentImport = _rootImport;

            foreach (string ns in namespaces)
            {
                currentImport = currentImport.FindImport(ns);
                if (currentImport == null)
                {
                    break;
                }
            }

            return currentImport?.FindType(typeName);
        }

        static internal Type? GetBuiltinType(string name)
        {
            Type? t = null;

            if (_builtinTypeMap.TryGetValue(name, out t))
            {
                return t;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region "Methods - Public"
        /// <summary>Imports all public and static members of a type into a specific namespace and makes them available to an expression.</summary>
        /// <param name="t">The type to import</param>
        /// <param name="ns">The namespace to import the type into -or- the empty string to import into the default namespace</param>
        /// <remarks>
        /// Use this method to import a type into an expression.  All static and public methods, fields, and properties of the type will be
        /// directly accessible in the expression.  If the namespace parameter is the empty string, the type's members will be able to be referenced
        /// without any qualification.  Otherwise, they will be imported into the specified namespace and will need to be qualified with it before being accessed.  The
        /// imported type is deemed accessible if it is either public or in the same module as the expression owner.
        /// </remarks>
        /// <example>
        /// If you import the Math type into the default namespace, you can reference its members in the expression: <c>cos(1.0)</c>.
        /// If you import same type into the "Math" namespace, you will need to qualify its members like so: <c>math.cos(1.0)</c>.
        /// </example>
        /// <exception cref="ArgumentException">The imported type is not accessible</exception>
        /// <exception cref="ArgumentNullException"><paramref name="t"/> or <paramref name="ns"/> is <see langword="null"/>.</exception>
        public void AddType(Type t, string ns)
        {
            Utility.AssertNotNull(t, "t");
            Utility.AssertNotNull(ns, "namespace");

            _context!.AssertTypeIsAccessible(t);

            NamespaceImport import = this.GetImport(ns);
            import.Add(new TypeImport(t, BindingFlags.Public | BindingFlags.Static, false));
        }

        /// <summary>Imports all public and static members of a type into the default namespace and makes them available to an expression.</summary>
        /// <param name="t">The type to import</param>
        /// <remarks>
        /// Use this method to import a type into an expression.  All static and public methods, fields, and properties of the type will be
        /// directly accessible in the expression.
        /// <para>See <see cref="AddType(Type, string)"/> for more details.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The imported type is not accessible</exception>
        /// <exception cref="ArgumentNullException"><paramref name="t"/> is <see langword="null"/>.</exception>
        public void AddType(Type t)
        {
            this.AddType(t, string.Empty);
        }

        /// <summary>Imports a public and static method of a type into a given namespace and makes it available to an expression.</summary>
        /// <param name="methodName">The name of the method to import</param>
        /// <param name="t">The type on which to lookup the method</param>
        /// <param name="ns">The namespace to import the method into -or- the empty string to import into the default namespace</param>
        /// <remarks>
        /// Use this method to import a single static, public method into an expression.  The method with the given name will be looked
        /// up on the type argument and added into the specified namespace.  The name is matched ignoring case.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The type the method belongs to is not accessible -or- the type has no public and static method with the given name
        /// </exception>
        /// <exception cref="AmbiguousMatchException">The type has more than one public and static method with the given name (overloads)</exception>
        /// <exception cref="ArgumentNullException"><paramref name="methodName"/>, <paramref name="t"/> or <paramref name="ns"/> is <see langword="null"/>.</exception>
        public void AddMethod(string methodName, Type t, string ns)
        {
            Utility.AssertNotNull(methodName, "methodName");
            Utility.AssertNotNull(t, "t");
            Utility.AssertNotNull(ns, "namespace");

            MethodInfo? mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

            if (mi == null)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.CouldNotFindPublicStaticMethodOnType, methodName, t.Name);
                throw new ArgumentException(msg);
            }

            this.AddMethod(mi, ns);
        }

        /// <summary>Imports a public and static method of a type into a given namespace and makes it available to an expression.</summary>
        /// <param name="mi">The method to import</param>
        /// <param name="ns">The namespace to import the method into -or- the empty string to import into the default namespace</param>
        /// <remarks>
        /// Use this method to import a single static, public method into an expression.  This overload is used when you already have a specific
        /// MethodInfo available.
        /// </remarks>
        /// <exception cref="ArgumentException">The type the method belongs to is not accessible -or- the method is not public and static</exception>
        /// <exception cref="ArgumentNullException"><paramref name="mi"/> or <paramref name="ns"/> is <see langword="null"/>.</exception>
        public void AddMethod(MethodInfo mi, string ns)
        {
            Utility.AssertNotNull(mi, "mi");
            Utility.AssertNotNull(ns, "namespace");

            // ReflectedType is null only for a module-level (global) method, which C# cannot declare.
            _context!.AssertTypeIsAccessible(mi.ReflectedType!);

            if (!mi.IsStatic || !mi.IsPublic)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.OnlyPublicStaticMethodsCanBeImported);
                throw new ArgumentException(msg);
            }

            NamespaceImport import = this.GetImport(ns);
            import.Add(new MethodImport(mi));
        }

        /// <summary>
        /// Imports the builtin types into an expression
        /// </summary>
        /// <remarks>
        /// Call this method to import the builtin types (int, string, double) into an expression.  After this method is called, you can use members of the
        /// builtin types in an expression ie: <c>int.maxvalue * 2</c>
        /// <para>
        /// Each type is imported into a namespace named after its keyword: boolean, byte, sbyte, short, ushort, int, uint, long, ulong,
        /// single, double, decimal, char, object and string.
        /// </para>
        /// </remarks>
        public void ImportBuiltinTypes()
        {
            foreach (KeyValuePair<string, Type> pair in _builtinTypeMap)
            {
                this.AddType(pair.Value, pair.Key);
            }
        }
        #endregion

        #region "Properties - Public"
        /// <summary>
        /// Gets the root import of the context.
        /// </summary>
        /// <value>The root import.</value>
        /// <remarks>
        /// Use this property to access the root import of an expression.  Imports added to it are in the default namespace.
        /// </remarks>
        public NamespaceImport RootImport => _rootImport;

        #endregion
    }
}
