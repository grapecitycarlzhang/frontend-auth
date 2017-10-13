using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace GrapeLeaf.FrontEnd.Filters
{
    public class FrontEndConfigAttribute : TypeFilterAttribute
    {
        public FrontEndConfigAttribute() : base(typeof(FrontEndConfigFilterImpl))
        {
        }

        private class FrontEndConfigFilterImpl : IAsyncActionFilter
        {
            private readonly IConfiguration _configuration;
            private readonly ILogger _logger;
            public FrontEndConfigFilterImpl(IConfiguration configuration, ILoggerFactory loggerFactory)
            {
                _configuration = configuration;
                _logger = loggerFactory.CreateLogger<FrontEndConfigFilterImpl>();
            }
            public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
            {
                // do something before the action executes
                await OnActionExecuting(context);
                await next();
                // do something after the action executes
            }
            public async Task OnActionExecuting(ActionExecutingContext context)
            {
                try
                {
                    var controller = context.Controller as Controller;
                    var viewData = controller.ViewData;
                    var settings = _configuration.GetSection("Settings");
                    viewData.Add("apiPrefix", settings["ApiPrefix"]);
                    viewData.Add("authorityUrl", settings["AuthorityUrl"]);
                    viewData.Add("loginUser", AccountModel.FromClaims(controller.User.Claims, await controller.HttpContext.GetTokenAsync("access_token")));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex.Message, nameof(FrontEndConfigFilterImpl.OnActionExecuting));
                    _logger.LogDebug(ex.StackTrace, nameof(FrontEndConfigFilterImpl.OnActionExecuting));
                }

            }
            public void OnActionExecuted(ActionExecutedContext context)
            {
            }

            public class AccountModel
            {
                public string id { get; set; }
                public string name { get; set; }
                public string role { get; set; }
                public string accessToken { get; set; }

                public static AccountModel FromClaims(IEnumerable<Claim> claims, string accessToken)
                {
                    var model = new AccountModel()
                    {
                        accessToken = accessToken
                    };
                    foreach (var claim in claims)
                    {
                        if (claim.Type == "access token")
                        {
                            // model.accessToken = claim.Value;
                        }

                        if (claim.Type == "sub")
                        {
                            model.id = claim.Value;
                        }

                        if (claim.Type == "role")
                        {
                            model.role = claim.Value;
                        }

                        if (claim.Type == "nickname")
                        {
                            model.name = claim.Value;
                        }
                    }

                    return model;
                }
            }
        }
    }
}
