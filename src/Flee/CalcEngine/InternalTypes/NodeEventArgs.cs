using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Provides the data for the <see cref="PublicTypes.CalculationEngine.NodeRecalculated"/> event.
    /// </summary>
    /// <remarks>
    /// Use the members of this class to get additional information about the recalculated node.  One instance is reused for
    /// every node of a recalculation, so read its values in the event handler.
    /// </remarks>
    public sealed class NodeEventArgs : EventArgs
    {

        private string _name;

        private object _result;

        internal NodeEventArgs()
        {
        }

        internal void SetData(string name, object result)
        {
            _name = name;
            _result = result;
        }

        /// <summary>
        /// Gets the name of the recalculated node.
        /// </summary>
        /// <value>The name of the node</value>
        /// <remarks>
        /// Use this property to get the name of the recalculated node.
        /// </remarks>
        public string Name => _name;

        /// <summary>
        /// Gets the recalculated result of the node.
        /// </summary>
        /// <value>The value of the result</value>
        /// <remarks>
        /// Use this property to get the recalculated result of the node.
        /// </remarks>
        public object Result => _result;
    }
}
