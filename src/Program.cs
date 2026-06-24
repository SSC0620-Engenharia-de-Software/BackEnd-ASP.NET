using src.Models;
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

builder.Services.AddSingleton<Conexao>();

var app = builder.Build();

// IMPORTANTE: UseCors ANTES dos endpoints
app.UseCors("AllowFrontend");

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapGet("/Estatisticas/MesMaisAntigo", async (Conexao db) =>
{
    try
    {
        // Como a tabela EMPRESA_GERAL tem as datas de abertura, 
        // ela deve ter a data mais antiga registrada
        var dataAntiga = await db.PegarRegistroMaisAntigo("EMPRESA_GERAL", "data_abertura");

        if (dataAntiga == null)
            return Results.NotFound("EMPRESA_GERAL está vazia.");

        return Results.Ok(new 
        { 
            Dia = dataAntiga.Value.Day,
            Mes = dataAntiga.Value.Month, 
            Ano = dataAntiga.Value.Year,
            DataFormatada = dataAntiga.Value.ToString("DD/MM/yyyy")
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao buscar data: {ex.Message}");
    }
});

app.MapGet("/Relatorios/EvolucaoEmpresas", async (string? tipoEmpresa, Conexao db) =>
{
    try
    {
        var dataAntiga = await db.PegarRegistroMaisAntigo("EMPRESA_GERAL", "data_abertura");

        if (dataAntiga == null)
            return Results.NotFound("EMPRESA_GERAL está vazia.");
        
        // Passamos o tipoEmpresa direto. Se não vier na URL, ele será null e fará a soma geral.
        var resultado = await db.GerarEvolucaoNumeroEmpresas(dataAntiga.Value, tipoEmpresa);
        
        return Results.Ok(resultado);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar a evolução das empresas: {ex.Message}");
    }
});

app.MapGet("/Relatorios/LeitosPorMes", async (string? tipoEmpresa, Conexao db) =>
{
    try
    {
        // Como a tabela EMPRESA_GERAL tem as datas de abertura, 
        // ela deve ter a data mais antiga registrada
        var dataAntiga = await db.PegarRegistroMaisAntigo("EMPRESA_GERAL", "data_abertura");

        if (dataAntiga == null)
            return Results.NotFound("EMPRESA_GERAL está vazia.");

        var res = await db.GerarEvolucaoLeitos(dataAntiga.Value, tipoEmpresa);

        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar a evolução dos leitos: {ex.Message}");
    }
});

app.MapGet("/Relatorios/DiariaMedia", async (string? tipoEmpresa, Conexao db) =>
{
    try
    {
        // Como a tabela EMPRESA_GERAL tem as datas de abertura, 
        // ela deve ter a data mais antiga registrada
        var dataAntiga = await db.PegarRegistroMaisAntigo("EMPRESA_GERAL", "data_abertura");

        if (dataAntiga == null)
            return Results.NotFound("EMPRESA_GERAL está vazia.");

        var res = await db.GerarEvolucaoDiariaMedia(dataAntiga.Value, tipoEmpresa);

        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar a evolução das diárias médias: {ex.Message}");
    }
});

app.MapGet("/DadosEmpresa", async (int id, Conexao db) =>
{
    try
    {
        // Chama a função do banco passando o objeto que chegou do frontend
        EmpresaCompletaDTO res = await db.PegarEmpresaCompletaPorId(id);
        
        // Retorna o status HTTP 201 (Created) ou 200 (Ok)
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        // Se der erro no banco, retorna um erro 500 para o frontend entender
        return Results.Problem($"Erro ao fazer o login: {ex.Message}");
    }
});

app.MapPost("/Login", async (LoginRequestDTO login, Conexao db) =>
{
    try
    {
        // Chama a função do banco passando o objeto que chegou do frontend
        ResultadoLoginDTO res = await db.ValidarLogin(login);
        
        // Retorna o status HTTP 200 (Ok)
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        // Se der erro no banco, retorna um erro 500 para o frontend entender
        return Results.Problem($"Erro ao fazer o login: {ex.Message}");
    }
});

app.MapPost("/AtualizarEmpresa", async (EmpresaCompletaDTO novosDados, Conexao db) =>
{
    try
    {
        // Chama a função do banco passando o objeto que chegou do frontend
        await db.AtualizarEmpresaCompleta(novosDados);
        
        // Retorna o status HTTP 200 (Ok)
        return Results.Ok();
    }
    catch (Exception ex)
    {
        // Se der erro no banco, retorna um erro 500 para o frontend entender
        return Results.Problem($"Erro ao atualizar os dados da empresa: {ex.Message}");
    }
});

app.MapPost("/CadastrarEmpresa", async (EmpresaCompletaDTO novosDados, Conexao db) =>
{
    try
    {
        if (novosDados.DadosGerais == null || string.IsNullOrEmpty(novosDados.DadosGerais.RazaoSocial))
        {
            return Results.BadRequest(new { mensagem = "Os dados gerais básicos da empresa são obrigatórios." });
        }

        int novoId = await db.InserirEmpresaCompleta(novosDados);
        
        // Atribui o ID gerado de volta ao DTO para retornar o objeto completo atualizado
        novosDados.DadosGerais.IdEmpresa = novoId;

        // Atualize apenas o link inicial
        return Results.Created($"/DadosEmpresa?id={novoId}", new { 
            id = novoId, 
            mensagem = "Empresa e tabelas satélites registadas com sucesso!",
            dados = novosDados
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro crítico ao efetuar o cadastro: {ex.Message}");
    }
});

app.MapControllers();
app.Run();