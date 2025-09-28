using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using WmsSystem.Data;
using WmsSystem.Models.Identity;

[assembly: OwinStartup(typeof(WmsSystem.Startup))]

namespace WmsSystem
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
        
        public void ConfigureAuth(IAppBuilder app)
        {
            app.CreatePerOwinContext(WmsDbContext.Create);
            app.CreatePerOwinContext<UserManager<ApplicationUser>>(CreateUserManager);
            app.CreatePerOwinContext<SignInManager<ApplicationUser, string>>(CreateSignInManager);
            
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                ExpireTimeSpan = TimeSpan.FromHours(8),
                SlidingExpiration = true
            });
        }
        
        private static UserManager<ApplicationUser> CreateUserManager(IdentityFactoryOptions<UserManager<ApplicationUser>> options, IOwinContext context)
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context.Get<WmsDbContext>()));
            
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
            };
            
            return manager;
        }
        
        private static SignInManager<ApplicationUser, string> CreateSignInManager(IdentityFactoryOptions<SignInManager<ApplicationUser, string>> options, IOwinContext context)
        {
            return new SignInManager<ApplicationUser, string>(context.GetUserManager<UserManager<ApplicationUser>>(), context.Authentication);
        }
    }
}
