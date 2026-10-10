using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal class BatchLoadInfo
    {
        public string Name;
        public string ExpressionText;

        public ExpressionContext Context;
        public BatchLoadInfo(string name, string text, ExpressionContext context)
        {
            this.Name = name;
            this.ExpressionText = text;
            this.Context = context;
        }
    }
}
