using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;

namespace GrapeLeaf.FrontEnd
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = "Cookies";
                options.Authority = Settings["AuthorityUrl"];
                options.RequireHttpsMetadata = false;
                options.ClientId = "grapeseed.dashboard";
                options.ClientSecret = "secret";
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                options.Scope.Add("basicinfo");
                options.Scope.Add("teacherAccount");
                options.Scope.Add("parent");
                options.Scope.Add("systemAccount");
                options.Scope.Add("offline_access");
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                options.Events = new OpenIdConnectEvents()
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Home/AccessDenied");

                        return Task.FromResult(0);
                    },

                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        if (!context.HttpContext.User.Identity.IsAuthenticated)
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Home/Index");
                        }

                        return Task.FromResult(0);
                    },

                    OnTokenValidated = context =>
                    {
                        return Task.FromResult(0);
                    },

                    OnRemoteFailure = context =>
                    {
                        context.HandleResponse();

                        if (context.Failure is OpenIdConnectProtocolException)
                        {
                            context.Response.Redirect("/Home/AccessDenied");
                        }
                        //else if (context.Failure.Message.Contains("Correlation failed."))
                        //{
                        //    context.Response.Redirect("/Home/Index");
                        //}
                        else if (context.Request.Path.HasValue && context.Request.Path.Value == "/signin-oidc"
                           && context.HttpContext.User.Identity.IsAuthenticated)
                        {
                            context.Response.Redirect("/Home/Index");
                        }
                        else
                        {
                            context.Response.Redirect("/Home/Error");
                        }

                        return Task.FromResult(0);
                    }
                };

            });

            services.AddResponseCompression();

            services.AddSingleton(_ => Configuration);

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.Use(async (context, next) =>
                {
                    context.Request.Scheme = "https";
                    HostString httpsHostString = new HostString(Settings["Host"]);
                    context.Request.Host = httpsHostString;
                    await next.Invoke();
                });

                app.UseExceptionHandler("/Home/Error");
            }

            app.UseResponseCompression();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public IConfiguration Configuration { get; }
        public IConfigurationSection Settings { get { return Configuration.GetSection("Settings"); } }
    }
}
