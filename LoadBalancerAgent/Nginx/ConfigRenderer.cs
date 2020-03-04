using System;
using HandlebarsDotNet;
using LoadBalancer;

namespace LoadBalancerAgent.Nginx
{
    public class ConfigRenderer
    {
        private Func<object, string> _templateFunc;

        public ConfigRenderer()
        {
            const string template = @"
events {
  worker_connections  4096;
}

http {
{{#rules}}
    upstream {{name}} {
        {{#targets}}
        server {{target}};
        {{/targets}}
    }

    server {
        listen {{listen_port}};
        server_name {{host}};

        location / {
            proxy_pass {{protocol}}://{{name}};
        }
    }

{{/rules}}
}
";

            _templateFunc = Handlebars.Compile(template);
        }

        public string Render(object routingRules)
        {
            return _templateFunc(routingRules);
        }
    }
}