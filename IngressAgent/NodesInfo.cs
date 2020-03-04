using System;
using System.Collections.Generic;
using System.Linq;
using k8s.Models;

namespace IngressAgent
{
    public class NodesInfo
    {
        public NodesInfo(V1NodeList v1NodeList)
        {
            v1NodeList.Items.ToList().ForEach(node =>
            {
                NodeAddresses = node.Status.Addresses.ToList().Select(address => address.Address);
            });
        }

        public IEnumerable<string> NodeAddresses { get; private set; }
    }
}