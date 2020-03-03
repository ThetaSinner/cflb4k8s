using System;
using System.Collections.Generic;
using System.Linq;
using Cflb4K8S;
using Microsoft.VisualBasic.CompilerServices;

namespace LoadBalancer
{
    public class RoutingRules
    {
        private readonly Dictionary<string, Rule> _rules = new Dictionary<string, Rule>();
        
        public bool Initialised { get; private set; }

        public void AddRule(string host, Rule rule)
        {
            Initialised = true;
            _rules.Add(host, rule);
        }

        public void DropRule(string requestHost)
        {
            _rules.Remove(requestHost);
            if (_rules.Count == 0)
            {
                Initialised = false;
            }
        }

        public object GetRenderData()
        {
            return new
            {
                rules = _rules.Values.Select(rule => new
                {
                    name = rule.Name,
                    host = rule.Host,
                    targets = rule.Targets.Select(target => new
                    {
                        target
                    }),
                    listen_port = rule.Port,
                    protocol = rule.Protocol
                })
            };
        }
    }
}
