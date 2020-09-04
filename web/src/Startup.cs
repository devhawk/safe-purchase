using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo.SmartContract.Manifest;

namespace SafePuchaseWeb
{


    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            const string NEO_EXPRESS_PATH = @"C:\Users\harry\Source\neo\seattle\samples\safe-purchase\default.neo-express";
            const string CONTRACT_MANIFEST_PATH = @"C:\Users\harry\Source\neo\seattle\samples\safe-purchase\contract\bin\Debug\netstandard2.1\safe-purchase.manifest.json";

            var neoExpress = NeoExpress.Load(NEO_EXPRESS_PATH);
            var contractManifest = ContractManifest.Parse(File.ReadAllText(CONTRACT_MANIFEST_PATH));

            services.AddSingleton<NeoExpress>(neoExpress);
            services.AddSingleton<ContractManifest>(contractManifest);
            services.AddHttpClient();
            services.AddControllersWithViews();
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
