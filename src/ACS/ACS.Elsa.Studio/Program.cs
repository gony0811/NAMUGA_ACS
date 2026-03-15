using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.WebHost.UseUrls("http://0.0.0.0:5200");

builder.Services
    .AddElsa(elsa => elsa
        .UseIdentity(identity =>
        {
            identity.TokenOptions = options =>
                options.SigningKey = "ACS-Elsa-Studio-JWT-Signing-Key-Min32Chars!!";
            identity.UseAdminUserProvider();
        })
        .UseDefaultAuthentication()
        .UseWorkflowManagement(management =>
            management.UseEntityFrameworkCore(ef =>
                ef.UsePostgreSql("Host=localhost;Port=5432;Database=acsdb_elsa;Username=postgres;Password=P@4083w0rd")))
        .UseWorkflowRuntime(runtime =>
            runtime.UseEntityFrameworkCore(ef =>
                ef.UsePostgreSql("Host=localhost;Port=5432;Database=acsdb_elsa;Username=postgres;Password=P@4083w0rd")))
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseCSharp()
        .UseHttp(http => http.ConfigureHttpOptions = options =>
        {
            options.BaseUrl = new Uri("http://localhost:5200");
            options.BasePath = "/api/workflows";
        })
        .UseWorkflowsApi()
        .AddActivitiesFrom<Program>()
        .AddActivitiesFrom<ACS.Elsa.Activities.ReflectionActivityBase>()
        .AddActivitiesFrom<ACS.Activity.Activities.SearchSuitableRechargeStation>()
        .AddWorkflowsFrom<Program>()
    );

builder.Services.AddCors(cors =>
    cors.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

builder.Services.AddRazorPages(options =>
    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseRouting();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapFallbackToPage("/_Host");
app.Run();
