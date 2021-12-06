using ApiBase.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ApiBase.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNet.SignalR;
using ApiBase.Helpers;

namespace ApiBase
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddAutoMapper(typeof(MapperProfiles).Assembly);
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiBase", Version = "v1" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            if (Env.IsDevelopment())
            {
                services.AddDbContext<ApiDbContext>(
                            //option => option.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
                            options => /*option.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))*/
                            {
                                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                                string connStr;

                                // Depending on if in development or production, use either Heroku-provided
                                // connection string, or development connection string from env var.
                                if (env == "Development")
                                {
                                    // Use connection string from file.
                                    connStr = Configuration.GetConnectionString("DefaultConnection");
                                }
                                else
                                {
                                    // Use connection string provided at runtime by Heroku.
                                    var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

                                    // Parse connection URL to connection string for Npgsql
                                    connUrl = connUrl.Replace("postgres://", string.Empty);
                                    var pgUserPass = connUrl.Split("@")[0];
                                    var pgHostPortDb = connUrl.Split("@")[1];
                                    var pgHostPort = pgHostPortDb.Split("/")[0];
                                    var pgDb = pgHostPortDb.Split("/")[1];
                                    var pgUser = pgUserPass.Split(":")[0];
                                    var pgPass = pgUserPass.Split(":")[1];
                                    var pgHost = pgHostPort.Split(":")[0];
                                    var pgPort = pgHostPort.Split(":")[1];

                                    connStr = $"Server={pgHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb};TrustServerCertificate=True";
                                }

                                // Whether the connection string came from the local development configuration file
                                // or from the environment variable from Heroku, use it to set up your DbContext.
                                options.UseNpgsql(connStr);
                            }
                            );
                            
                    }
            else
            {
                services.AddDbContext<ApiDbContext>(
                            //option => option.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
                            options => /*option.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))*/
                            {
                                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                                string connStr;

                                // Depending on if in development or production, use either Heroku-provided
                                // connection string, or development connection string from env var.
                                if (env == "Development")
                                {
                                    // Use connection string from file.
                                    connStr = Configuration.GetConnectionString("DefaultConnection");
                                }
                                else
                                {
                                    // Use connection string provided at runtime by Heroku.
                                    var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

                                    // Parse connection URL to connection string for Npgsql
                                    connUrl = connUrl.Replace("postgres://", string.Empty);
                                    var pgUserPass = connUrl.Split("@")[0];
                                    var pgHostPortDb = connUrl.Split("@")[1];
                                    var pgHostPort = pgHostPortDb.Split("/")[0];
                                    var pgDb = pgHostPortDb.Split("/")[1];
                                    var pgUser = pgUserPass.Split(":")[0];
                                    var pgPass = pgUserPass.Split(":")[1];
                                    var pgHost = pgHostPort.Split(":")[0];
                                    var pgPort = pgHostPort.Split(":")[1];

                                    connStr = $"Server={pgHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb};sslmode=Require;TrustServerCertificate=True";
                                }

                                // Whether the connection string came from the local development configuration file
                                // or from the environment variable from Heroku, use it to set up your DbContext.
                                options.UseNpgsql(connStr);
                            }
                            );
            }

            services.AddIdentity<AppUser, IdentityRole>(
                    option =>
                    {
                        option.Password.RequireDigit = false;
                        option.Password.RequiredLength = 6;
                        option.Password.RequireNonAlphanumeric = false;
                        option.Password.RequireUppercase = false;
                        option.Password.RequireLowercase = false;
                    }
                ).AddEntityFrameworkStores<ApiDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Configuration["Jwt:Site"],
                    ValidIssuer = Configuration["Jwt:Site"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:SigningKey"]))
                };
            });            ;

            services.AddSignalR();

            services.AddCors();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiBase v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseCors(x => x
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithOrigins("http://localhost:4200"));

            app.UseAuthorization();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<EventHub>("/events", opt =>
                {
                    
                });
                endpoints.MapFallbackToController("Index", "Fallback");
            });

            

        }
    }
}
