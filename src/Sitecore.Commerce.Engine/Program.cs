// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Sitecore.Commerce.Engine
{
    /// <summary>
    /// Defines the program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command line args.</param>
#pragma warning disable CA1801
        public static void Main(string[] args)
#pragma warning restore CA1801
        {
            try
            {
                CreateHost().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Creates the host 
        /// </summary>
        /// <returns>The <see cref="IHost"/></returns>
        public static IHost CreateHost()
        {
            return new HostBuilder()
                   .UseContentRoot(Directory.GetCurrentDirectory())
                   .ConfigureWebHostDefaults(webBuilder =>
                   {
                       webBuilder
                           .ConfigureKestrel(options =>
                           {
                               options.Limits.MinResponseDataRate = null;

                               var configuration = options.ApplicationServices.GetRequiredService<IConfiguration>();
                               bool useHttps = configuration.GetValue("AppSettings:UseHttpsInKestrel", false);
                               if (useHttps)
                               {
                                   int port = configuration.GetValue("AppSettings:SslPort", 5000);
                                   string pfxPath = configuration.GetSection("AppSettings:SslPfxPath").Value ?? string.Empty;
                                   string pfxPassword = configuration.GetSection("AppSettings:SslPfxPassword").Value ?? string.Empty;
                                   var hostingEnvironment = options.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

                                   if (File.Exists(Path.Combine(hostingEnvironment.ContentRootPath, pfxPath)))
                                   {
                                       options.Listen(IPAddress.Any, port, listenOptions => { listenOptions.UseHttps(pfxPath, pfxPassword); });
                                   }
                               }
                           })
                           .ConfigureAppConfiguration((context, builder) =>
                           {
                               builder
                                   .SetBasePath(context.HostingEnvironment.WebRootPath)
                                   .AddJsonFile("config.json", false, true)
                                   .AddJsonFile($"config.{context.HostingEnvironment.EnvironmentName}.json", true, true);

                               if (context.HostingEnvironment.IsDevelopment())
                               {
                                   builder.AddApplicationInsightsSettings(true);
                               }

                               // Call AddEnvironmentVariables method last to allow environment variables to override values from other providers.
                               builder.AddEnvironmentVariables("COMMERCEENGINE_");
                           })
                           .UseSerilog()
                           .UseStartup<Startup>();
                   })
                   .Build();
        }
    }
}
