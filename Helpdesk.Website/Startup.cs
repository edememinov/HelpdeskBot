using Microsoft.Owin;
using Owin;
using System.Collections.Generic;

namespace Helpdesk.Website
{
    public partial class Startup
    {
        public object AppName { get; private set; }

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

    }
}
