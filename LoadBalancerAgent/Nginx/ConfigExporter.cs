using System;
using System.Diagnostics;
using LoadBalancer;
using Microsoft.Extensions.Configuration;

namespace LoadBalancerAgent.Nginx
{
    public class ConfigExporter
    {
        private readonly ConfigRenderer _configRenderer;
        
        private readonly string _nginxConfigLocation;
        private readonly string _reloadCommand;

        public ConfigExporter(ConfigRenderer configRenderer, IConfiguration configuration)
        {
            _configRenderer = configRenderer;
            
            _nginxConfigLocation = configuration.GetValue<string>("nginxConfigLocation");
            if (_nginxConfigLocation == null)
            {
                throw new InvalidOperationException("Must configure setting [nginxConfigLocation]");
            }

            _reloadCommand = configuration.GetValue<string>("reloadCommand");
            if (_reloadCommand == null)
            {
                throw new InvalidOperationException("Must configure setting [reloadCommand]");
            }
        }
        
        public void Export(RoutingRules routingRules)
        {
            var content = _configRenderer.Render(routingRules.GetRenderData());
            
            System.IO.File.WriteAllText (_nginxConfigLocation, content);

            // Reload the nginx service to use the new configuration.
            _reloadCommand.Bash();
        }
    }
}
