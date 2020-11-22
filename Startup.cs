using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AspNetIdentity_Server.Data;
using AspNetIdentity_Server.Data.Models;
using AspNetIdentity_Server.Extensions;
using IdentityServer4.WsFederation.Configuration;
using IdentityServer4.WsFederation.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace AspNetIdentity_Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Config.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var corsOrigins = Configuration.GetSection("CorsOrigins");
            services.AddCors(options =>
            {
                options.AddPolicy(name: "ApplicationCorsPolicy",
                builder =>
                {
                    builder.WithOrigins(corsOrigins.GetValue<string>("CallCenterRootUri"));
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.SetPreflightMaxAge(new TimeSpan(0, 0, 600));
                });
            });

            services.AddRazorPages();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<User, IdentityRole>()
                .AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;

            // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
            options.EmitStaticAudienceClaim = true;
        })
            .AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            .AddInMemoryClients(Config.Clients)
            .AddAspNetIdentity<User>()
            .AddSigningCredential(InitializeRsaKey())
            .AddWsFederationPlugin(options => new WsFederationOptions
            {
                // WsFederationEndpoint = "https://localhost:5001/wsfed",
                Licensee = "DEMO",
                LicenseKey = "eyJTb2xkRm9yIjowLjAsIktleVByZXNldCI6NiwiU2F2ZUtleSI6ZmFsc2UsIkxlZ2FjeUtleSI6ZmFsc2UsIlJlbmV3YWxTZW50VGltZSI6IjAwMDEtMDEtMDFUMDA6MDA6MDAiLCJhdXRoIjoiREVNTyIsImV4cCI6IjIwMjAtMTItMTlUMDA6MDA6MDAiLCJpYXQiOiIyMDIwLTExLTE5VDEwOjE4OjI2Iiwib3JnIjoiREVNTyIsImF1ZCI6M30=.UhTAxychYJHyF39zhgwN8waQn3nraIW2Qm2Sw+wb5ISnqzO3KJbr+t/mp8w6NdgMlzj7SXZ36QqkP5/c9hdaIzS+/2UPl6ndSI7B4+up4FlhAntEDBumODY6Pgr5IF/BcFEvk09vm65cA84DRMsDcMeREDkkjYNGVbeJjLzpOzJ6QmQifVplYIblR38A9qYDeTdToh9dI8zT1n8McZWW8tYi9YES8/YJM4UBxBkuO8Ej6haFihPeVZkXmzAOkQRWRwy4+06VxC4tGzJeVWNK4vBLEpZxLTr81To2MnRhhNjzjqx/oaJfm7FmcqhVlcLobFTsoDy8pQmIK9tO5mF+aoic6TJBvahPIwt7BIsJHFMjqM4RNw/97T81WMhIAdev9rrhVTVLonyxgko24JCOUQIGnvaKF4eFZ4bKcsDNr1ymOjgdM5zFGwuLAAY+Okk+Ftl5TPa/x5aZSIg6SI6dZPD9dxFuC5e3kLCniSS6fuz2K+cKqdOBtq2lJyloWZt1yyoC9+suy45NrLOMVQeWwsXqysACFjPVU3OBoCFD00Vt3vaw6x8sv7bdRZdM8PRfRdB+A09bOhmq3PUA8pTTcDTuyRS5cncyGMlTkwnFybbziAWGDVQP0hug4HJdfRF6c2BmqP6/S1Js7ULPLSevyKQzs7BJmYcKlHmSN04QlrE="
            })
            .AddInMemoryRelyingParties(new List<RelyingParty>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer().UseIdentityServerWsFederationPlugin();
            // app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private SigningCredentials InitializeRsaKey()
        {
            try
            {
                RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider(2048);
                var rsaParametersPrivate = RSAExtensions.RSAParametersFromXmlFile("private-rsa-key.xml");
                rsaProvider.ImportParameters(rsaParametersPrivate);
                var securityKey = new RsaSecurityKey(rsaProvider);
                return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            }
            catch (Exception ex)
            {
                throw new Exception("Identity Server RSA Key initialization failed. " + ex.ToString());
            }
        }
    }
}
