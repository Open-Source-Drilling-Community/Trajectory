using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NORCE.Drilling.Trajectory.WebApp.Client
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            // This needs to match with what is defined in "charts/<helm-chart-name>/templates/values.yaml ingress.Path
            app.UsePathBase("/Trajectory/webapp");

            if (!String.IsNullOrEmpty(Configuration["TrajectoryHostURL"]))
			NORCE.Drilling.Trajectory.WebApp.Client.Configuration.TrajectoryHostURL = Configuration["TrajectoryHostURL"];
            NORCE.Drilling.Trajectory.WebApp.Client.Configuration.WellBoreHostURL = Configuration["WellBoreHostURL"];
            NORCE.Drilling.Trajectory.WebApp.Client.Configuration.WellHostURL = Configuration["WellHostURL"];
            NORCE.Drilling.Trajectory.WebApp.Client.Configuration.FieldHostURL = Configuration["FieldHostURL"];
            NORCE.Drilling.Trajectory.WebApp.Client.Configuration.ClusterHostURL = Configuration["ClusterHostURL"];
            NORCE.Drilling.Trajectory.WebApp.Client.Configuration.SurveyInstrumentHostURL = Configuration["SurveyInstrumentHostURL"];
            NORCE.Drilling.Trajectory.WebApp.Client.Configuration.SurveyProgramHostURL = Configuration["SurveyProgramHostURL"];


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

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
