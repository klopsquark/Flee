using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
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

        public string Name => _name;

        public object Result => _result;
    }
}
