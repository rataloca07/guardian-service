using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GuardianService.Services;

var builder = WebApplication.CreateBuilder(args);

// Asegúrate de agregar esta línea para cargar las variables de entorno
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();

// Firestore Service
builder.Services.AddScoped<FirestoreService>();


// Registrar IAuthService y su implementación
builder.Services.AddScoped<IAuthService, AuthService>();

// Configuración JWT Authentication
/*builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });*/
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")))
    };
});

// Configuración de CORS para permitir cualquier origen en desarrollo (o especificar orígenes)
builder.Services.AddCors(options =>
{
    /*options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()  // Permitir cualquier origen
              .AllowAnyHeader()  // Permitir cualquier encabezado
              .AllowAnyMethod();  // Permitir cualquier método HTTP
    });*/
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://localhost", "https://guardian-service.onrender.com")  // Permitir estos dominios
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
/*builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();*/

var app = builder.Build();

//app.UseCors("AllowAllOrigins");
app.UseCors("AllowSpecificOrigins");

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

//app.UseHttpsRedirection();

//app.UseAuthorization();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
