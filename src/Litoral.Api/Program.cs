using Litoral.Api.Errors;
using Litoral.Api.Monitoring;
using Litoral.Api.OpenApi;
using Litoral.Infrastructure;
using Litoral.Infrastructure.Persistence;
using Litoral.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionRecorder>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<RequestMetricsMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);

    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Litoral API v1");
        options.DocumentTitle = "Litoral Linhas Aéreas - API";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(LocalPhotoStorage.ResolveRoot(app.Environment, app.Configuration)),
    RequestPath = LocalPhotoStorage.RequestPath
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;