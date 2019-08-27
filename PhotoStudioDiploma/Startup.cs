using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PhotoStudioDiploma.Startup))]
namespace PhotoStudioDiploma
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
