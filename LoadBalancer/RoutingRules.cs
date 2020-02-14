using System.Collections.Generic;

namespace LoadBalancer
{
    public class RoutingRules
    {
        private Dictionary<string, string> _rules;
        
        public bool Initialised { get; private set; }

        public void AddRule(string host, string target)
        {
            Initialised = true;
            _rules.Add(host, target);
        }

        public void DropRule(string requestHost)
        {
            _rules.Remove(requestHost);
        }
    }
}
