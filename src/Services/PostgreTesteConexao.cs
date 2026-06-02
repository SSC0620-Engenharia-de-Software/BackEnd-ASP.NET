using Npgsql;
using src.Models;

namespace src.Services;

public class TesteConexao
{
    private readonly IConfiguration _configuration;

    public TesteConexao(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<List<Empresa>> PegarEmpresas()
    {
        var empresas = new List<Empresa>();

        var connectionString =
            _configuration.GetConnectionString("Postgres");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        var command = new NpgsqlCommand(
            "SELECT * FROM empresa_teste",
            connection
        );

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            empresas.Add(new Empresa
            {
                Nome = reader.GetString(0),
                Senha = reader.GetString(1),
                Tipo = reader.GetString(2)
            });
        }

        return empresas;
    }

    public async Task<List<Reservas>> PegarReservas()
    {
        var reservas = new List<Reservas>();

        var connectionString =
            _configuration.GetConnectionString("Postgres");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        var command = new NpgsqlCommand(
            "SELECT * FROM reservas_teste",
            connection
        );

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            reservas.Add(new Reservas
            {
                Empresa = reader.GetString(0),
                Data = DateOnly.FromDateTime(reader.GetDateTime(1)),
                NroReservas = reader.GetInt32(2)
            });
        }

        return reservas;
    }
}