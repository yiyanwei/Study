using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IdentityModel;
using System.Text;
using NLog.Web;
using SqlSugar;
using ZeroOne.Application;
using ZeroOne.Entity;
using ZeroOne.Extension.Global;
using Microsoft.AspNetCore.Routing;
using AutoMapper;
using ZeroOne.Extension.Model;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.PlatformAbstractions;
using Hangfire.MySql;
using Hangfire;
using ZeroOne.WebApi.Hubs;
using ZeroOne.Extension;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace ZeroOne.WebApi
{


    public class Startup
    {
        public IWebHostEnvironment Environment { get; set; }
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        private static string globalCorsName = "Global";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
            services.AddControllers(options =>
            {
                options.Filters.Add<ModelValidActionFilter>();
                ////优先使用客户端指定的数据格式，资源的表述
                //options.RespectBrowserAcceptHeader = true;
                ////添加xml数据格式的输出
                //options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
                options.Filters.Add(typeof(Global415Filter));
                options.Filters.Add(typeof(GlobalResultFilter));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

            services.AddSingleton<CountService>();
            services.AddSignalR();

            services.Configure<RedisCacheOptions>(Configuration.GetSection(nameof(RedisCacheOptions)));
            RedisCacheOptions redisCacheOptions = new RedisCacheOptions();
            Configuration.Bind(redisCacheOptions);
            services.AddSingleton<IDistributedCache>(serviceProvider => new RedisCache(redisCacheOptions));

            services.AddSession((s) => {
                s.IdleTimeout = TimeSpan.FromSeconds(2000);
            });

            //映射
            var config = new MapperConfiguration(e => e.AddProfile<ViewMappingProfile>());
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            //允许跨域访问
            services.AddCors(options =>
            {
                //options.AddPolicy(globalCorsName, set =>
                //{
                //    set.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                //});

                options.AddPolicy(globalCorsName, builder => builder.WithOrigins("http://localhost:8080", "http://localhost:8081").AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            });

            services.Configure<RouteOptions>(config =>
            {
                //config.ConstraintMap.Add("enum", typeof(EnumConstraint));
                //config.ConstraintMap.Add(nameof(GetUrlParamToObjConstraint), typeof(GetUrlParamToObjConstraint));
            });


            //添加服务到服务容器
            services.AddRepository();
            services.AddTransient<IProInfoService, ProInfoService>();
            services.AddTransient<IProCategoryService, ProCategoryService>();
            services.AddTransient<IUserInfoService, UserInfoService>();
            services.AddTransient<IFileInfoService, FileInfoService>();
            services.AddTransient<ISupplierService, SupplierService>();
            services.AddTransient<IDistrictService, DistrictService>();
            services.AddTransient<IMapLocationService, MapLocationService>();

            //数据库连接配置
            var conncfg = Configuration.GetSection(nameof(ConnectionConfig));
            services.Configure<ConnectionConfig>(conncfg);
            services.AddTransient<ISqlSugarClient>(t =>
            {
                var connConfig = t.GetRequiredService<IOptions<ConnectionConfig>>().Value;
                var extMethodList = new List<SqlFuncExternal>();
                extMethodList.Add(new SqlFuncExternal()
                {
                    MethodValue = (a, b, c) =>
                    {
                        return string.Empty;
                    },
                    UniqueMethodName = "IFNULL"
                });
                connConfig.ConfigureExternalServices.SqlFuncServices = extMethodList;
                return new SqlSugarClient(connConfig);
            });

            //地图定位配置
            services.Configure<MapLocationSettings>(Configuration.GetSection(nameof(MapLocationSettings)));


            #region 配置hangfire
            //运用MySql存储，对应web.config中的connectionStrings中的name
            //GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(conncfg["ConnectionString"], new MySqlStorageOptions() { TablesPrefix = "hg" }));
            //services.AddHangfire(cfg => cfg.UseStorage(new MySqlStorage(conncfg["ConnectionString"], new MySqlStorageOptions() { TablesPrefix = "hg" })));

            //services.AddHangfire(cfg =>
            //{
            //    var hangfireCfg = new HangfireConfig();
            //    Configuration.Bind(nameof(HangfireConfig), hangfireCfg);
            //    cfg.UseRedisStorage(hangfireCfg.ConnectionString, new RedisStorageOptions() { Db = hangfireCfg.Db });
            //});
            #endregion

            //配置Jwt信息
            services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
            JwtSettings jwtSettings = new JwtSettings();
            Configuration.Bind("JwtSettings", jwtSettings);
            //添加身份验证

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    //这里的key要进行加密
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    NameClaimType = JwtClaimTypes.Name,
                    RoleClaimType = JwtClaimTypes.Role,
                    //Token颁发机构
                    ValidIssuer = jwtSettings.Issuer,
                    //颁发给谁
                    ValidAudience = jwtSettings.Audience
                    /***********************************TokenValidationParameters的参数默认值***********************************/
                    // RequireSignedTokens = true,
                    // SaveSigninToken = false,
                    // ValidateActor = false,
                    // 将下面两个参数设置为false，可以不验证Issuer和Audience，但是不建议这样做。
                    // ValidateAudience = true,
                    // ValidateIssuer = true, 
                    // ValidateIssuerSigningKey = false,
                    // 是否要求Token的Claims中必须包含Expires
                    // RequireExpirationTime = true,
                    // 允许的服务器时间偏移量
                    // ClockSkew = TimeSpan.FromSeconds(300),
                    // 是否验证Token有效期，使用当前时间与Token的Claims中的NotBefore和Expires对比
                    // ValidateLifetime = true

                };
                o.Events = new JwtBearerOverrideEvents();
            });

            //文件上传配置
            var uploadSettingSection = Configuration.GetSection(nameof(UploadSettings));
            services.Configure<UploadSettings>(options =>
            {
                options.SourceImgRootPath = uploadSettingSection[nameof(UploadSettings.SourceImgRootPath)];
                options.ThumbnailImgRootPath = uploadSettingSection[nameof(UploadSettings.ThumbnailImgRootPath)];
            });

            //高德行政区域查询配置
            var districtSettingSection = Configuration.GetSection(nameof(DistrictSettings));
            services.Configure<DistrictSettings>(districtSettingSection);

            //add swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v3", new OpenApiInfo { Title = "ZeroOne", Version = "v1", Contact = new OpenApiContact { Name = "yiyanwei", Url = new Uri("http://www.baidu.com"), Email = "yiyanwei@live.com" } });
                //c.DocumentFilter<>
                //为 Swagger JSON and UI设置xml文档注释路径
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
                //获取entity的xml文件
                var entityXmlFile = $"{typeof(BaseEntity<>).Assembly.GetName().Name}.xml";
                var entityXmlPath = Path.Combine(AppContext.BaseDirectory, entityXmlFile);
                c.IncludeXmlComments(entityXmlPath);
            });

            //添加日志容器服务
            services.AddLogging(logger =>
            {
                logger.AddNLog($"nlog.config");
                //logger.AddNLog($"nlog.{Environment.EnvironmentName}.config");
            });
        }

        public void UseSetting()
        {
            string ss = string.Empty;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //string temp = $"AppDomain.BaseDirectory：{AppDomain.BaseDirectory}"; 

            Console.WriteLine(string.Format("EnvironmentName:{0}", env.EnvironmentName));
            Console.WriteLine(string.Format("WebHostDefaults.StartupAssemblyKey:{0}", WebHostDefaults.StartupAssemblyKey));
            Console.WriteLine(string.Format("AppContext.BaseDirectory:{0}", AppContext.BaseDirectory));
            Console.WriteLine(string.Format("AppDomain.CurrentDomain.BaseDirectory:{0}", AppDomain.CurrentDomain.BaseDirectory));
            Console.WriteLine(string.Format("IWebHostEnvironment.ContentRootPath:{0}", env.ContentRootPath));
            Console.WriteLine(string.Format("IWebHostEnvironment.WebRootPath:{0}", env.WebRootPath));

            //app.Use((req) =>
            //{
            //    Console.WriteLine("AAA");
            //    return req;
            //});

            //app.Use((req) =>
            //{
            //    Console.WriteLine("BBB");
            //    return req;
            //});

            //启用swagger中间件
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v3/swagger.json", "ApiHelp v3");
            });

            //app.Use((req) =>
            //{
            //    Console.WriteLine("CCC");

            //    RequestDelegate next = context =>
            //    {
            //        context.Response.StatusCode = 404;
            //        return Task.CompletedTask;
            //    };

            //    return next;
            //    //return Task.CompletedTask;
            //});

            app.UseCors(globalCorsName);
            //允许访问静态文件
            app.UseStaticFiles();
            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(
            //    Path.Combine(Directory.GetCurrentDirectory(), @"MyStaticFiles/Upload")),//物理地址
            //    RequestPath = new PathString()//请求地址，默认为url的根目录
            //});



            //app.UseHangfireDashboard();//配置后台仪表盘
            //app.UseHangfireServer();//开始使用Hangfire服务
            //RecurringJob.AddOrUpdate("test", () => Console.WriteLine("每1秒执行任务"), "*/1 * * * * *");


            app.UseHttpsRedirection();
            //启用身份验证中间件
            app.UseAuthentication();
            //启用路由中间件
            app.UseRouting();
            //如果没有匹配上的路由

            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<CountHub>("/count");
                endpoints.MapControllers();
            });
        }
    }
}
