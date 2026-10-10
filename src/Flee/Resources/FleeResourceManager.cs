using System.Resources;

namespace Flee.Resources
{
    internal class FleeResourceManager
    {

        private Dictionary<string, ResourceManager> _resourceManagers;

        private static FleeResourceManager _instance = new FleeResourceManager();
        private FleeResourceManager()
        {
            _resourceManagers = new Dictionary<string, ResourceManager>(StringComparer.OrdinalIgnoreCase);
        }

        private ResourceManager GetResourceManager(string resourceFile)
        {
            lock (this)
            {
                ResourceManager rm = null;
                if (!_resourceManagers.TryGetValue(resourceFile, out rm))
                {
                    Type t = typeof(FleeResourceManager);
                    rm = new ResourceManager(string.Format("{0}.{1}", t.Namespace, resourceFile), t.Assembly);
                    _resourceManagers.Add(resourceFile, rm);
                }
                return rm;
            }
        }

        private string GetResourceString(string resourceFile, string key)
        {
            ResourceManager rm = this.GetResourceManager(resourceFile);
            return rm.GetString(key);
        }

        public string GetCompileErrorString(string key)
        {
            return this.GetResourceString("CompileErrors", key);
        }

        public string GetElementNameString(string key)
        {
            return this.GetResourceString("ElementNames", key);
        }

        public string GetGeneralErrorString(string key)
        {
            return this.GetResourceString("GeneralErrors", key);
        }

        public static FleeResourceManager Instance
        {
            get { return _instance; }
        }
    }
}
