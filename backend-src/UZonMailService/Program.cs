using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using UZonMailService.Config;
using UZonMailService.Utils.DotNETCore;
using UZonMailService.Models.SQL;
using UZonMailService.Utils.DotNETCore.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using UZonMailService.SignalRHubs;
using Uamazing.Utils.Extensions;
using UZonMailService.Services.HostedServices;
using Quartz;
using UZonMailService.Utils.ASPNETCore.Filters;
using Uamazing.Utils.Helpers;
using UZonMailService.Middlewares;
using Microsoft.AspNetCore.HttpLogging;
using UZonMailService.Cache;
using Uamazing.Utils.Web.Token;

var appOptions = new WebApplicationOptions
{
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory),
    WebRootPath = "wwwroot",
    Args = args
};
var builder = WebApplication.CreateBuilder(appOptions);

var services = builder.Services;

// ��־
services.AddLogging();
services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestProperties
        | HttpLoggingFields.RequestHeaders
        | HttpLoggingFields.ResponseHeaders;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});
// log4net: https://github.com/huorswords/Microsoft.Extensions.Logging.Log4Net.AspNetCore/blob/develop/samples/Net8.0/WebApi/log4net.config
builder.Logging.ClearProviders();
builder.Logging.AddLog4Net();

// ���� httpClient
services.AddHttpClient();

// Add services to the container.
services.AddControllers(option =>
{
    // ����ȫ���쳣����
    option.Filters.Add(new KnownExceptionFilter());
    option.Filters.Add(new TokenExpiredFilter());
})
.AddNewtonsoftJson();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();

// ���� swagger
services.AddSwaggerGen(new OpenApiInfo()
{
    Title = "UZonMail",
    Contact = new OpenApiContact()
    {
        Name = "galens",
        Url = new Uri("https://galens.uamazing.cn"),
        Email = "gmx_galens@163.com"
    }
}, "UZonMailService.xml");

// ��֤�� jwt ��ʵ��
// ���� signalR������Ҫ�� app ��ʹ�� MapHub
// �ο�: https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?view=aspnetcore-8.0&tabs=visual-studio
services.AddSignalR();
// Change to use Name as the user identifier for SignalR
// WARNING: This requires that the source of your JWT token 
// ensures that the Name claim is unique!
// If the Name claim isn't unique, users could receive messages 
// intended for a different user!
// ��ʹ���Զ���� IUserIdProvider��ʹ��Ĭ�ϵģ���֤ token �� claim �а��� ClaimTypes.Name,
//builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// ���� hyphen-case ·��
services.SetupSlugifyCaseRoute();
// ������
services.Configure<AppConfig>(builder.Configuration);
// ע�����ݿ�
services.AddSqlContext();
// ע�� liteDB
//services.AddLiteDB();
// �������ݻ���
services.AddCache();

// ���� HttpContextAccessor���Թ� service ��ȡ��ǰ������û���Ϣ
services.AddHttpContextAccessor();
// ����ע�����
services.AddServices();
// ���Ӻ�̨����
services.AddHostedService<SendingHostedService>();

// ��ʱ����
services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));
// if you are using persistent job store, you might want to alter some options
services.Configure<QuartzOptions>(options =>
{
    options.Scheduling.IgnoreDuplicates = true; // default: false
    options.Scheduling.OverWriteExistingData = true; // default: true
});
services.AddQuartz();
services.AddQuartzHostedService(
    q => q.WaitForJobsToComplete = false);

// ���� jwt ��֤
var tokenParams = new TokenParams();
builder.Configuration.GetSection("TokenParams").Bind(tokenParams);
services.AddJWTAuthentication(tokenParams.UniqueSecret);
// ���ýӿڼ�Ȩ����
services.AddAuthorizationBuilder()
    // ����
    .AddPolicy("RequireAdmin", policy => policy.RequireClaim(ClaimTypes.Role, "admin"));

// �رղ����Զ�����
services.Configure<ApiBehaviorOptions>(o =>
{
    o.SuppressModelStateInvalidFilter = true;
});

// ����
services.AddCors(options =>
{
    var configuration = builder.Configuration;
    // ��ȡ��������
    string[]? corsConfig = configuration.GetSection("Cors").Get<string[]>();

    // ��ȡ��ǰ�����ĵ�ַ
    var hostIPs = NetworkHelper.GetCurrentHostIPs();
    var port = configuration.GetSection("Http:Port").Get<int>();
    var hostUrls = hostIPs.Select(x => $"http://{x}:{port}").ToList();
    List<string> cors = [$"http://127.0.0.1:{port}", $"http://localhost:{port}", "http://localhost:9000"];
    cors.AddRange(hostUrls);

    if (corsConfig?.Length > 0) cors.AddRange(corsConfig);

    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins([.. cors])
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

// �޸��ļ��ϴ���С����
services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
});

// ���� Kestrel ������
// Ĭ�ϼ�����ַͨ�� Urls ���� 
builder.WebHost.ConfigureKestrel(options =>
{
    bool listenAnyIP = builder.Configuration.GetSection("Http:ListenAnyIP").Get<bool>();
    int port = builder.Configuration.GetSection("Http:Port").Get<int>();
    if (listenAnyIP)
        options.ListenAnyIP(port);
    else
        options.ListenLocalhost(port);

    options.Limits.MaxRequestBodySize = int.MaxValue;
});

var app = builder.Build();


app.UseDefaultFiles();
// ������վ�ĸ�Ŀ¼
app.UseStaticFiles();

// ���� public Ŀ¼Ϊ��̬�ļ�Ŀ¼
var publicPath = Path.Combine(builder.Environment.ContentRootPath, "public");
Directory.CreateDirectory(publicPath);
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(publicPath),
    RequestPath = "/public"
});

// ����
app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

// vue ��ҳ��Ӧ���м��
app.UseVueASP();

// http ·��
app.MapControllers();

// SignalR ����
app.MapHub<UzonMailHub>($"/hubs/{nameof(UzonMailHub).ToCamelCase()}");

// ��ʼ���ݿ�
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
app.Run();
