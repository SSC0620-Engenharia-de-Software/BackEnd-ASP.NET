using src.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<TesteConexao>();

var app = builder.Build();

// IMPORTANTE: UseCors ANTES dos endpoints
app.UseCors("AllowFrontend");

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapGet("/Teste", async (TesteConexao db) =>
{
    return await db.PegarPessoas();
});

app.MapControllers();

app.Run();