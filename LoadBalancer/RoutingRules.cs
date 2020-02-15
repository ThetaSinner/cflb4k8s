using System;
using System.Collections.Generic;

namespace LoadBalancer
{
    public class RoutingRules
    {
        private readonly Dictionary<string, string> _rules = new Dictionary<string, string>();
        
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

        public string GetTarget(string host)
        {
            if (!Initialised)
            {
                throw new InvalidOperationException("Cannot use routing rules until it is initialised.");
            }
            
            if (_rules.TryGetValue(host, out var target))
            {
                return target;
            }
            
            throw new InvalidOperationException("Cannot get host because it is not mapped.");
        }
    }
}
