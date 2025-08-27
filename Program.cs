using DocumentManager.API.Data;
using DocumentManager.API.Infrastructure.Repositories;
using DocumentManager.API.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Configuration
// ----------------------------
var configuration = builder.Configuration;

// ----------------------------
// EF Core + MySQL
// ----------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// ----------------------------
// Form limits (büyük dosyalar için)
// ----------------------------
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1073741824; // 1GB
});

// ----------------------------
// CORS (Vite default: http://localhost:5173)
// ----------------------------
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", p =>
    p.WithOrigins("http://localhost:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
));

// ----------------------------
// Add Controllers & Swagger
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------------
// Dependency Injection
// ----------------------------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IFileStorage, FileStorage>();

var app = builder.Build();

// ----------------------------
// Uploads klasörünü statik servis et
// ----------------------------
var uploadsRelative = configuration["StoredFilesPath"] ?? "Uploads";
var uploadsAbsolute = Path.Combine(app.Environment.ContentRootPath, uploadsRelative);
Directory.CreateDirectory(uploadsAbsolute);

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsAbsolute),
    RequestPath = "/uploads"
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.MapControllers();

app.Run();