using Dapper;
using Npgsql;
using System.Reflection;
using src.Models;

namespace src.Services;

public class Conexao
{
    private readonly IConfiguration _configuration;

    public Conexao(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<NpgsqlConnection> CriarConexao()
    {
        var connectionString = _configuration.GetConnectionString("Postgres");

        var connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync();
        return connection;
    }

    public async Task<List<T>> PegarTabela<T>(string nomeTabela)
    {
        // Aqui já estava correto
        await using var connection = await CriarConexao();
        
        var resultado = await connection.QueryAsync<T>($"SELECT * FROM {nomeTabela}");        
        return resultado.ToList();
    }

    private bool MudouAlgo<T>(T? atual, T novo)
    {
        if (novo == null)
            return false;

        if (atual == null)
            return true;

        PropertyInfo[] propriedades = typeof(T).GetProperties();

        foreach (PropertyInfo prop in propriedades)
        {
            if (prop.Name == "Data") continue;

            var valorAtual = prop.GetValue(atual);
            var valorNovo = prop.GetValue(novo);

            if (!Equals(valorAtual, valorNovo))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<List<TipoEmpresaDTO>> PegarTiposEmpresas()
    {
        return await PegarTabela<TipoEmpresaDTO>("TIPO_EMPRESA");
    }

    public async Task<DateTime?> PegarRegistroMaisAntigo(string nomeTabela, string nomeColunaData = "data")
    {
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var query = $"SELECT MIN({nomeColunaData}) FROM {nomeTabela}";

        await using var command = new NpgsqlCommand(query, conn);
        var resultado = await command.ExecuteScalarAsync();

        if (resultado != DBNull.Value && resultado != null)
        {
            return Convert.ToDateTime(resultado);
        }

        return null;
    }

    public async Task<List<RelatorioNumeroEmpresasDTO>> GerarEvolucaoNumeroEmpresas(DateTime dataInicio, string nomeTipo = null)
    {
        var relatorio = new List<RelatorioNumeroEmpresasDTO>();
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var filtroTipo = string.IsNullOrEmpty(nomeTipo) ? "" : " AND e.nometipo = @nomeTipo ";

        var query = $@"
            WITH Meses AS (
                SELECT GENERATE_SERIES(@dataInicio::DATE, CURRENT_DATE, '1 month')::DATE AS mes_analise
            )
            SELECT 
                m.mes_analise,
                COUNT(e.idempresa) AS qtd_empresas
            FROM Meses m
            LEFT JOIN (
                SELECT eg.idempresa, eg.data_abertura, te.nometipo
                FROM EMPRESA_GERAL eg
                LEFT JOIN TIPO_EMPRESA te ON eg.idtipo = te.idtipo
                WHERE eg.data_abertura IS NOT NULL
            ) e ON e.data_abertura <= (m.mes_analise + interval '1 month' - interval '1 day')::DATE
            {filtroTipo}
            GROUP BY m.mes_analise
            ORDER BY m.mes_analise;";

        await using var command = new NpgsqlCommand(query, conn);
        
        command.Parameters.AddWithValue("dataInicio", dataInicio);

        if (!string.IsNullOrEmpty(nomeTipo))
        {
            command.Parameters.AddWithValue("nomeTipo", nomeTipo);
        }

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            relatorio.Add(new RelatorioNumeroEmpresasDTO
            {
                MesAnalise = reader.GetDateTime(0),
                QuantidadeEmpresas = Convert.ToInt32(reader.GetValue(1))
            });
        }

        return relatorio;
    }

    public async Task<List<RelatorioLeitosDTO>> GerarEvolucaoLeitos(DateTime dataInicio, string nomeTipo = null)
    {
        var relatorio = new List<RelatorioLeitosDTO>();
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var filtroTipo = string.IsNullOrEmpty(nomeTipo) ? "" : " AND e.nometipo = @nomeTipo ";

        var query = $@"
            WITH Meses AS (
                SELECT GENERATE_SERIES(@dataInicio::DATE, CURRENT_DATE, '1 month')::DATE AS mes_analise
            ),
            Empresas AS (
                SELECT eg.idempresa, te.nometipo 
                FROM EMPRESA_GERAL eg
                LEFT JOIN TIPO_EMPRESA te ON eg.idtipo = te.idtipo
            )
            SELECT 
                m.mes_analise,
                COALESCE(SUM(historico.total_leitos), 0) AS total_leitos_cidade
            FROM Meses m
            CROSS JOIN Empresas e
            LEFT JOIN LATERAL (
                SELECT total_leitos 
                FROM EMPRESA_ESTRUTURA est 
                WHERE est.idempresa = e.idempresa 
                {filtroTipo}
                AND est.data <= (m.mes_analise + interval '1 month' - interval '1 day')::DATE
                ORDER BY est.data DESC 
                LIMIT 1
            ) historico ON TRUE
            GROUP BY m.mes_analise
            ORDER BY m.mes_analise;";

        await using var command = new NpgsqlCommand(query, conn);
        
        command.Parameters.AddWithValue("dataInicio", dataInicio);

        if (!string.IsNullOrEmpty(nomeTipo))
        {
            command.Parameters.AddWithValue("nomeTipo", nomeTipo);
        }

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            relatorio.Add(new RelatorioLeitosDTO
            {
                MesAnalise = reader.GetDateTime(0),
                TotalLeitos = Convert.ToInt32(reader.GetValue(1)) 
            });
        }

        return relatorio;
    }

    public async Task<List<RelatorioDiariaMediaDTO>> GerarEvolucaoDiariaMedia(DateTime dataInicio, string? nomeTipo = null)
    {
        var relatorio = new List<RelatorioDiariaMediaDTO>();
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var filtroTipo = string.IsNullOrEmpty(nomeTipo) ? "" : " AND e.nometipo = @nomeTipo ";

        var query = $@"
            WITH Meses AS (
                SELECT GENERATE_SERIES(@dataInicio::DATE, CURRENT_DATE, '1 month')::DATE AS mes_analise
            ),
            Empresas AS (
                SELECT eg.idempresa, te.nometipo 
                FROM EMPRESA_GERAL eg
                LEFT JOIN TIPO_EMPRESA te ON eg.idtipo = te.idtipo
            )
            SELECT 
                m.mes_analise,
                COALESCE(ROUND(AVG(NULLIF(historico.diariamedia, 0)), 2), 0) AS media_diaria_cidade
            FROM Meses m
            CROSS JOIN Empresas e
            LEFT JOIN LATERAL (
                SELECT diariamedia 
                FROM PESQUISA_HOSPEDAGEM ph 
                WHERE ph.idempresa = e.idempresa 
                {filtroTipo}
                AND ph.data <= (m.mes_analise + interval '1 month' - interval '1 day')::DATE
                ORDER BY ph.data DESC 
                LIMIT 1
            ) historico ON TRUE
            GROUP BY m.mes_analise
            ORDER BY m.mes_analise;";

        await using var command = new NpgsqlCommand(query, conn);
        
        command.Parameters.AddWithValue("dataInicio", dataInicio);

        if (!string.IsNullOrEmpty(nomeTipo))
        {
            command.Parameters.AddWithValue("nomeTipo", nomeTipo);
        }

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            relatorio.Add(new RelatorioDiariaMediaDTO
            {
                MesAnalise = reader.GetDateTime(0),
                ValorDiaria = Convert.ToDecimal(reader.GetValue(1))
            });
        }

        return relatorio;
    }

    public async Task<Dictionary<int, List<PesquisaHospedagemColunaDTO>>> PegarHistoricoDeUmaColuna(string nomeColuna)
    {
        var colunasPermitidas = new HashSet<string> 
        { 
            "taxaocup", "diariamedia", "qtdhospedes", "qtdleitos", "qtduhs" 
        };

        if (!colunasPermitidas.Contains(nomeColuna.ToLower()))
        {
            throw new ArgumentException("Nome de coluna inválido ou não autorizado.");
        }

        var historicoAgrupado = new Dictionary<int, List<PesquisaHospedagemColunaDTO>>();
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var query = $@"
            SELECT idempresa, data, {nomeColuna} AS valor_dinamico 
            FROM PESQUISA_HOSPEDAGEM 
            ORDER BY idempresa ASC, data ASC;";

        await using var command = new NpgsqlCommand(query, conn);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var idEmpresa = Convert.ToInt32(reader["idempresa"]);
            
            var dto = new PesquisaHospedagemColunaDTO
            {
                Data = Convert.ToDateTime(reader["data"]),
                Valor = reader["valor_dinamico"] != DBNull.Value ? Convert.ToDecimal(reader["valor_dinamico"]) : null
            };

            if (!historicoAgrupado.ContainsKey(idEmpresa))
            {
                historicoAgrupado[idEmpresa] = new List<PesquisaHospedagemColunaDTO>();
            }
            
            historicoAgrupado[idEmpresa].Add(dto);
        }

        return historicoAgrupado;
    }

    public async Task<EmpresaCompletaDTO?> PegarEmpresaCompletaPorId(int idEmpresa)
    {
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var query = @"
            SELECT 
                -- EMPRESA_GERAL e TIPO_EMPRESA
                eg.idempresa, te.nometipo AS tipo_nome, eg.razao_social, eg.nome_fantasia, 
                eg.data_abertura, eg.proprietarios, eg.cnae, eg.cadastur, eg.num_cadastur, 
                eg.venc_cadastur, eg.endereco, eg.bairro, eg.localizacao, eg.regiao, 
                eg.tel_comercial, eg.email_comercial, eg.site, eg.redes_sociais, 
                eg.func_fixos, eg.func_temporarios,
                
                -- EMPRESA_PESQUISA
                ep.aceita_pesquisa, ep.tel_pesquisa, ep.email_pesquisa, ep.plano_emergencia,
                ep.mulheres_lideranca, ep.mulher_empreendedora, ep.camp_educ_ambiental,
                ep.uso_fontes_renovaveis, ep.selo_sustentabilidade, ep.camp_reducao_residuos,
                ep.praticas_gestao_sustentavel, ep.plano_recursos_hidricos, ep.plano_gestao_ambiental, 
                ep.data AS data_pesquisa,
                
                -- EMPRESA_SERVICOS
                es.idioma_ingles, es.idioma_espanhol, es.outro_idioma, es.equip_uhs, es.equip_recepcao,
                es.servicos_aeb, es.area_refeicoes, es.sanitario_aeb, es.alimentacao_diferenciada,
                es.area_eventos, es.equip_eventos, es.aberto_publico, es.equip_lazer, 
                es.data AS data_servicos,
                
                -- EMPRESA_ACESSIBILIDADE
                ea.facilidades_pcd, ea.tipos_deficiencia, ea.pessoal_capacitado, ea.rota_externa,
                ea.embarque_desembarque, ea.vaga_estacionamento, ea.area_circulacao, ea.escada,
                ea.rampa, ea.piso, ea.elevador, ea.alarme_emergencia, ea.locais_alarme,
                ea.comunicacao_pcd, ea.balcao_atendimento, ea.sanitario_adaptado,
                ea.telefone_acessivel, ea.sinalizacao_preferencial, ea.data AS data_acessibilidade,
                
                -- EMPRESA_ESTRUTURA (Apenas o registo mais recente)
                est.qtd_uhs, est.qtd_uhs_pcd, est.total_leitos, est.min_leitos_uh, est.max_leitos_uh,
                est.func_24h, est.horario_checkin_checkout, est.sistema_reservas, est.formas_pagamento,
                est.estacionamento, est.manobrista, est.mensageiro, est.area_fumantes, est.pet_friendly, 
                est.data AS data_estrutura

            FROM EMPRESA_GERAL eg
            LEFT JOIN TIPO_EMPRESA te ON eg.idtipo = te.idtipo
            LEFT JOIN EMPRESA_PESQUISA ep ON eg.idempresa = ep.idempresa
            LEFT JOIN EMPRESA_SERVICOS es ON eg.idempresa = es.idempresa
            LEFT JOIN EMPRESA_ACESSIBILIDADE ea ON eg.idempresa = ea.idempresa
            LEFT JOIN LATERAL (
                SELECT h.* FROM EMPRESA_ESTRUTURA h 
                WHERE h.idempresa = eg.idempresa 
                ORDER BY h.data DESC 
                LIMIT 1
            ) est ON TRUE
            WHERE eg.idempresa = @idEmpresa;";

        await using var command = new NpgsqlCommand(query, conn);
        command.Parameters.AddWithValue("idEmpresa", idEmpresa);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new EmpresaCompletaDTO
            {
                DadosGerais = new EmpresaGeralDTO
                {
                    IdEmpresa = Convert.ToInt32(reader["idempresa"]),
                    Tipo = reader["tipo_nome"].ToString(),
                    RazaoSocial = reader["razao_social"]?.ToString(),
                    NomeFantasia = reader["nome_fantasia"]?.ToString(),
                    DataAbertura = reader["data_abertura"] != DBNull.Value ? Convert.ToDateTime(reader["data_abertura"]) : null,
                    Proprietarios = reader["proprietarios"]?.ToString(),
                    Cnae = reader["cnae"]?.ToString(),
                    Cadastur = reader["cadastur"]?.ToString(),
                    NumCadastur = reader["num_cadastur"]?.ToString(),
                    VencCadastur = reader["venc_cadastur"] != DBNull.Value ? Convert.ToDateTime(reader["venc_cadastur"]) : null,
                    Endereco = reader["endereco"]?.ToString(),
                    Bairro = reader["bairro"]?.ToString(),
                    Localizacao = reader["localizacao"]?.ToString(),
                    Regiao = reader["regiao"]?.ToString(),
                    TelComercial = reader["tel_comercial"]?.ToString(),
                    EmailComercial = reader["email_comercial"]?.ToString(),
                    Site = reader["site"]?.ToString(),
                    RedesSociais = reader["redes_sociais"]?.ToString(),
                    FuncFixos = reader["func_fixos"] != DBNull.Value ? Convert.ToInt32(reader["func_fixos"]) : null,
                    FuncTemporarios = reader["func_temporarios"] != DBNull.Value ? Convert.ToInt32(reader["func_temporarios"]) : null
                },
                
                DadosPesquisa = reader["data_pesquisa"] == DBNull.Value ? null : new EmpresaPesquisaDTO
                {
                    AceitaPesquisa = reader["aceita_pesquisa"] as bool?,
                    TelPesquisa = reader["tel_pesquisa"]?.ToString(),
                    EmailPesquisa = reader["email_pesquisa"]?.ToString(),
                    PlanoEmergencia = reader["plano_emergencia"] as bool?,
                    MulheresLideranca = reader["mulheres_lideranca"] as bool?,
                    MulherEmpreendedora = reader["mulher_empreendedora"] as bool?,
                    CampEducAmbiental = reader["camp_educ_ambiental"] as bool?,
                    UsoFontesRenovaveis = reader["uso_fontes_renovaveis"] as bool?,
                    SeloSustentabilidade = reader["selo_sustentabilidade"] as bool?,
                    CampReducaoResiduos = reader["camp_reducao_residuos"] as bool?,
                    PraticasGestaoSustentavel = reader["praticas_gestao_sustentavel"] as bool?,
                    PlanoRecursosHidricos = reader["plano_recursos_hidricos"] as bool?,
                    PlanoGestaoAmbiental = reader["plano_gestao_ambiental"] as bool?,
                    Data = reader["data_pesquisa"] != DBNull.Value ? Convert.ToDateTime(reader["data_pesquisa"]) : null
                },

                Servicos = reader["data_servicos"] == DBNull.Value ? null : new EmpresaServicosDTO
                {
                    IdiomaIngles = reader["idioma_ingles"] as bool?,
                    IdiomaEspanhol = reader["idioma_espanhol"] as bool?,
                    OutroIdioma = reader["outro_idioma"]?.ToString(),
                    EquipUhs = reader["equip_uhs"]?.ToString(),
                    EquipRecepcao = reader["equip_recepcao"]?.ToString(),
                    ServicosAeb = reader["servicos_aeb"]?.ToString(),
                    AreaRefeicoes = reader["area_refeicoes"] as bool?,
                    SanitarioAeb = reader["sanitario_aeb"] as bool?,
                    AlimentacaoDiferenciada = reader["alimentacao_diferenciada"]?.ToString(),
                    AreaEventos = reader["area_eventos"] as bool?,
                    EquipEventos = reader["equip_eventos"]?.ToString(),
                    AbertoPublico = reader["aberto_publico"] as bool?,
                    EquipLazer = reader["equip_lazer"]?.ToString(),
                    Data = reader["data_servicos"] != DBNull.Value ? Convert.ToDateTime(reader["data_servicos"]) : null
                },

                Acessibilidade = reader["data_acessibilidade"] == DBNull.Value ? null : new EmpresaAcessibilidadeDTO
                {
                    FacilidadesPcd = reader["facilidades_pcd"] as bool?,
                    TiposDeficiencia = reader["tipos_deficiencia"]?.ToString(),
                    PessoalCapacitado = reader["pessoal_capacitado"] as bool?,
                    RotaExterna = reader["rota_externa"] as bool?,
                    EmbarqueDesembarque = reader["embarque_desembarque"] as bool?,
                    VagaEstacionamento = reader["vaga_estacionamento"] as bool?,
                    AreaCirculacao = reader["area_circulacao"] as bool?,
                    Escada = reader["escada"] as bool?,
                    Rampa = reader["rampa"] as bool?,
                    Piso = reader["piso"] as bool?,
                    Elevador = reader["elevador"] as bool?,
                    AlarmeEmergencia = reader["alarme_emergencia"] as bool?,
                    LocaisAlarme = reader["locais_alarme"]?.ToString(),
                    ComunicacaoPcd = reader["comunicacao_pcd"]?.ToString(),
                    BalcaoAtendimento = reader["balcao_atendimento"] as bool?,
                    SanitarioAdaptado = reader["sanitario_adaptado"] as bool?,
                    TelefoneAcessivel = reader["telefone_acessivel"] as bool?,
                    SinalizacaoPreferencial = reader["sinalizacao_preferencial"] as bool?,
                    Data = reader["data_acessibilidade"] != DBNull.Value ? Convert.ToDateTime(reader["data_acessibilidade"]) : null
                },

                EstruturaAtual = reader["data_estrutura"] == DBNull.Value ? null : new EmpresaEstruturaDTO
                {
                    QtdUhs = reader["qtd_uhs"] != DBNull.Value ? Convert.ToInt32(reader["qtd_uhs"]) : null,
                    QtdUhsPcd = reader["qtd_uhs_pcd"] != DBNull.Value ? Convert.ToInt32(reader["qtd_uhs_pcd"]) : null,
                    TotalLeitos = reader["total_leitos"] != DBNull.Value ? Convert.ToInt32(reader["total_leitos"]) : null,
                    MinLeitosUh = reader["min_leitos_uh"] != DBNull.Value ? Convert.ToInt32(reader["min_leitos_uh"]) : null,
                    MaxLeitosUh = reader["max_leitos_uh"] != DBNull.Value ? Convert.ToInt32(reader["max_leitos_uh"]) : null,
                    Func24h = reader["func_24h"] as bool?,
                    HorarioCheckinCheckout = reader["horario_checkin_checkout"]?.ToString(),
                    SistemaReservas = reader["sistema_reservas"]?.ToString(),
                    FormasPagamento = reader["formas_pagamento"]?.ToString(),
                    Estacionamento = reader["estacionamento"] as bool?,
                    Manobrista = reader["manobrista"] as bool?,
                    Mensageiro = reader["mensageiro"] as bool?,
                    AreaFumantes = reader["area_fumantes"] as bool?,
                    PetFriendly = reader["pet_friendly"] as bool?,
                    Data = reader["data_estrutura"] != DBNull.Value ? Convert.ToDateTime(reader["data_estrutura"]) : null
                }
            };
        }

        return null;
    }

    public async Task<ResultadoLoginDTO> ValidarLogin(LoginRequestDTO loginDados)
    {
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var query = @"
            SELECT 'Visitante' AS Categoria, NULL::INTEGER AS IdEmpresa 
            FROM PESSOA_VISITANTE WHERE idpessoa::text = @id::text AND senha::text = @senha::text
            UNION ALL
            SELECT 'Admin' AS Categoria, NULL::INTEGER AS IdEmpresa 
            FROM PESSOA_ADMIN WHERE idpessoa::text = @id::text AND senha::text = @senha::text
            UNION ALL
            SELECT 'Funcionario' AS Categoria, NULL::INTEGER AS IdEmpresa 
            FROM PESSOA_FUNCIONARIO WHERE idpessoa::text = @id::text AND senha::text = @senha::text
            UNION ALL
            SELECT 'Empresario' AS Categoria, idempresa AS IdEmpresa 
            FROM PESSOA_EMPRESARIO WHERE idpessoa::text = @id::text AND senha::text = @senha::text
            LIMIT 1";

        await using var command = new NpgsqlCommand(query, conn);

        command.Parameters.AddWithValue("id", loginDados.Id);
        command.Parameters.AddWithValue("senha", loginDados.Senha);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ResultadoLoginDTO
            {
                Categoria = reader.GetString(0),
                IdEmpresa = reader.IsDBNull(1) ? null : reader.GetInt32(1)
            };
        }

        return new ResultadoLoginDTO 
        { 
            Categoria = "Invalido", 
            IdEmpresa = null 
        };
    }

    public async Task<int> InserirPesquisaHospedagem(PesquisaHospedagemDTO dto)
    {
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();

        var query = @"
            INSERT INTO PESQUISA_HOSPEDAGEM (
                idempresa, data, taxaocup, diariamedia, qtdhospedes, qtdleitos, qtduhs
            ) VALUES (
                @idempresa, @data, @taxaocup, @diariamedia, @qtdhospedes, @qtdleitos, @qtduhs
            ) RETURNING idpesquisa;"; 

        try
        {
            await using var command = new NpgsqlCommand(query, conn);
            
            command.Parameters.AddWithValue("idempresa", dto.IdEmpresa);
            command.Parameters.AddWithValue("data", dto.Data.Date);
            
            command.Parameters.AddWithValue("taxaocup", dto.TaxaOcupacao ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("diariamedia", dto.DiariaMedia ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("qtdhospedes", dto.QtdHospedes ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("qtdleitos", dto.QtdLeitos ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("qtduhs", dto.QtdUhs ?? (object)DBNull.Value);

            var idGerado = Convert.ToInt32(await command.ExecuteScalarAsync());
            return idGerado;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new Exception("Já existe um fechamento de pesquisa para esta empresa nesta mesma data. Utilize a atualização.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> AtualizarColunaPesquisaHospedagem(int idEmpresa, string nomeColuna, PesquisaHospedagemColunaDTO novoValor)
    {
        var colunasPermitidas = new HashSet<string> 
        { 
            "taxaocup", "diariamedia", "qtdhospedes", "qtdleitos", "qtduhs" 
        };

        if (!colunasPermitidas.Contains(nomeColuna.ToLower()))
        {
            throw new ArgumentException("Nome de coluna inválido ou não autorizado.");
        }

        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var query = $@"
                UPDATE PESQUISA_HOSPEDAGEM 
                SET {nomeColuna} = @novoValor
                WHERE idempresa = @idempresa AND data = @data;";

            await using var command = new NpgsqlCommand(query, conn, transaction);
            
            command.Parameters.AddWithValue("idempresa", idEmpresa);
            command.Parameters.AddWithValue("data", novoValor.Data.Date);
            command.Parameters.AddWithValue("novoValor", novoValor.Valor ?? (object)DBNull.Value);

            var linhasAfetadas = await command.ExecuteNonQueryAsync();
            
            if (linhasAfetadas == 1)
            {
                await transaction.CommitAsync();
                return true;
            }
            else if (linhasAfetadas > 1)
            {
                await transaction.RollbackAsync();
                throw new Exception("ALERTA CRÍTICO: Anomalia de dados. Mais de um registro modificado. ROLLBACK acionado.");
            }
            
            await transaction.RollbackAsync();
            return false; 
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> InserirEmpresaCompleta(EmpresaCompletaDTO novaEmpresa)
    {
        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            void AddParam(NpgsqlCommand cmd, string nome, object? valor) 
                => cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);

            var sqlGeral = @"
                INSERT INTO EMPRESA_GERAL (
                    idtipo, razao_social, nome_fantasia, data_abertura, proprietarios, cnae, 
                    cadastur, num_cadastur, venc_cadastur, endereco, bairro, localizacao, 
                    regiao, tel_comercial, email_comercial, site, redes_sociais, func_fixos, func_temporarios
                ) VALUES (
                    (SELECT idtipo FROM TIPO_EMPRESA WHERE nometipo = @tipo LIMIT 1),
                    @razao, @fantasia, @abertura, @prop, @cnae, @cad, @num_cad, @venc_cad, 
                    @end, @bairro, @loc, @reg, @tel, @email, @site, @redes, @func_f, @func_t
                ) RETURNING idempresa;";

            int idEmpresaGerado;

            await using (var cmdGeral = new NpgsqlCommand(sqlGeral, conn, transaction))
            {
                var dg = novaEmpresa.DadosGerais;
                AddParam(cmdGeral, "tipo", dg.Tipo);
                AddParam(cmdGeral, "razao", dg.RazaoSocial);
                AddParam(cmdGeral, "fantasia", dg.NomeFantasia);
                AddParam(cmdGeral, "abertura", dg.DataAbertura);
                AddParam(cmdGeral, "prop", dg.Proprietarios);
                AddParam(cmdGeral, "cnae", dg.Cnae);
                AddParam(cmdGeral, "cad", dg.Cadastur);
                AddParam(cmdGeral, "num_cad", dg.NumCadastur);
                AddParam(cmdGeral, "venc_cad", dg.VencCadastur);
                AddParam(cmdGeral, "end", dg.Endereco);
                AddParam(cmdGeral, "bairro", dg.Bairro);
                AddParam(cmdGeral, "loc", dg.Localizacao);
                AddParam(cmdGeral, "reg", dg.Regiao);
                AddParam(cmdGeral, "tel", dg.TelComercial);
                AddParam(cmdGeral, "email", dg.EmailComercial);
                AddParam(cmdGeral, "site", dg.Site);
                AddParam(cmdGeral, "redes", dg.RedesSociais);
                AddParam(cmdGeral, "func_f", dg.FuncFixos);
                AddParam(cmdGeral, "func_t", dg.FuncTemporarios);

                idEmpresaGerado = Convert.ToInt32(await cmdGeral.ExecuteScalarAsync());
            }

            if (novaEmpresa.EstruturaAtual != null)
            {
                var sqlEst = @"
                    INSERT INTO EMPRESA_ESTRUTURA (
                        idempresa, data, qtd_uhs, qtd_uhs_pcd, total_leitos, min_leitos_uh, max_leitos_uh,
                        func_24h, horario_checkin_checkout, sistema_reservas, formas_pagamento,
                        estacionamento, manobrista, mensageiro, area_fumantes, pet_friendly
                    ) VALUES (
                        @id, CURRENT_DATE, @uhs, @uhs_pcd, @leitos, @min_l, @max_l, @f24, @checkin,
                        @reservas, @pgto, @estac, @manob, @mensag, @fumo, @pet
                    );";

                await using var cmdEst = new NpgsqlCommand(sqlEst, conn, transaction);
                var e = novaEmpresa.EstruturaAtual;
                AddParam(cmdEst, "id", idEmpresaGerado);
                AddParam(cmdEst, "uhs", e.QtdUhs);
                AddParam(cmdEst, "uhs_pcd", e.QtdUhsPcd);
                AddParam(cmdEst, "leitos", e.TotalLeitos);
                AddParam(cmdEst, "min_l", e.MinLeitosUh);
                AddParam(cmdEst, "max_l", e.MaxLeitosUh);
                AddParam(cmdEst, "f24", e.Func24h);
                AddParam(cmdEst, "checkin", e.HorarioCheckinCheckout);
                AddParam(cmdEst, "reservas", e.SistemaReservas);
                AddParam(cmdEst, "pgto", e.FormasPagamento);
                AddParam(cmdEst, "estac", e.Estacionamento);
                AddParam(cmdEst, "manob", e.Manobrista);
                AddParam(cmdEst, "mensag", e.Mensageiro);
                AddParam(cmdEst, "fumo", e.AreaFumantes);
                AddParam(cmdEst, "pet", e.PetFriendly);

                await cmdEst.ExecuteNonQueryAsync();
            }

            if (novaEmpresa.Servicos != null)
            {
                var sqlSrv = @"
                    INSERT INTO EMPRESA_SERVICOS (
                        idempresa, data, idioma_ingles, idioma_espanhol, outro_idioma, equip_uhs,
                        equip_recepcao, servicos_aeb, area_refeicoes, sanitario_aeb, alimentacao_diferenciada,
                        area_eventos, equip_eventos, aberto_publico, equip_lazer
                    ) VALUES (
                        @id, CURRENT_DATE, @ing, @esp, @outro, @e_uhs, @e_rec, @s_aeb, @a_ref, @san_aeb, 
                        @ali_dif, @a_evt, @e_evt, @pub, @e_laz
                    );";

                await using var cmdSrv = new NpgsqlCommand(sqlSrv, conn, transaction);
                var s = novaEmpresa.Servicos;
                AddParam(cmdSrv, "id", idEmpresaGerado);
                AddParam(cmdSrv, "ing", s.IdiomaIngles);
                AddParam(cmdSrv, "esp", s.IdiomaEspanhol);
                AddParam(cmdSrv, "outro", s.OutroIdioma);
                AddParam(cmdSrv, "e_uhs", s.EquipUhs);
                AddParam(cmdSrv, "e_rec", s.EquipRecepcao);
                AddParam(cmdSrv, "s_aeb", s.ServicosAeb);
                AddParam(cmdSrv, "a_ref", s.AreaRefeicoes);
                AddParam(cmdSrv, "san_aeb", s.SanitarioAeb);
                AddParam(cmdSrv, "ali_dif", s.AlimentacaoDiferenciada);
                AddParam(cmdSrv, "a_evt", s.AreaEventos);
                AddParam(cmdSrv, "e_evt", s.EquipEventos);
                AddParam(cmdSrv, "pub", s.AbertoPublico);
                AddParam(cmdSrv, "e_laz", s.EquipLazer);

                await cmdSrv.ExecuteNonQueryAsync();
            }

            if (novaEmpresa.Acessibilidade != null)
            {
                var sqlAcs = @"
                    INSERT INTO EMPRESA_ACESSIBILIDADE (
                        idempresa, data, facilidades_pcd, tipos_deficiencia, personnel_capacitado, rota_externa,
                        embarque_desembarque, vaga_estacionamento, area_circulacao, escada, rampa, piso,
                        elevador, alarme_emergencia, locais_alarme, comunicacao_pcd, balcao_atendimento,
                        sanitario_adaptado, telefone_acessivel, sinalizacao_preferencial
                    ) VALUES (
                        @id, CURRENT_DATE, @facil, @tipos, @pes, @rota, @emb, @vaga, @circ, @esc, @ramp, @piso,
                        @elev, @alarme, @locais, @comun, @balc, @sanit, @tel, @sinal
                    );";

                await using var cmdAcs = new NpgsqlCommand(sqlAcs, conn, transaction);
                var a = novaEmpresa.Acessibilidade;
                AddParam(cmdAcs, "id", idEmpresaGerado);
                AddParam(cmdAcs, "facil", a.FacilidadesPcd);
                AddParam(cmdAcs, "tipos", a.TiposDeficiencia);
                AddParam(cmdAcs, "pes", a.PessoalCapacitado);
                AddParam(cmdAcs, "rota", a.RotaExterna);
                AddParam(cmdAcs, "emb", a.EmbarqueDesembarque);
                AddParam(cmdAcs, "vaga", a.VagaEstacionamento);
                AddParam(cmdAcs, "circ", a.AreaCirculacao);
                AddParam(cmdAcs, "esc", a.Escada);
                AddParam(cmdAcs, "ramp", a.Rampa);
                AddParam(cmdAcs, "piso", a.Piso);
                AddParam(cmdAcs, "elev", a.Elevador);
                AddParam(cmdAcs, "alarme", a.AlarmeEmergencia);
                AddParam(cmdAcs, "locais", a.LocaisAlarme);
                AddParam(cmdAcs, "comun", a.ComunicacaoPcd);
                AddParam(cmdAcs, "balc", a.BalcaoAtendimento);
                AddParam(cmdAcs, "sanit", a.SanitarioAdaptado);
                AddParam(cmdAcs, "tel", a.TelefoneAcessivel);
                AddParam(cmdAcs, "sinal", a.SinalizacaoPreferencial);

                await cmdAcs.ExecuteNonQueryAsync();
            }

            if (novaEmpresa.DadosPesquisa != null)
            {
                var sqlPsq = @"
                    INSERT INTO EMPRESA_PESQUISA (
                        idempresa, data, aceita_pesquisa, tel_pesquisa, email_pesquisa, plano_emergencia,
                        mulheres_lideranca, mulher_empreendedora, camp_educ_ambiental, uso_fontes_renovaveis,
                        selo_sustentabilidade, camp_reducao_residuos, praticas_gestao_sustentavel,
                        plano_recursos_hidricos, plano_gestao_ambiental
                    ) VALUES (
                        @id, CURRENT_DATE, @aceita, @tel, @email, @pl_emerg, @mul_lid, @mul_emp, @camp_amb,
                        @fontes, @selo, @camp_res, @prat_gest, @pl_hidr, @pl_gest_amb
                    );";

                await using var cmdPsq = new NpgsqlCommand(sqlPsq, conn, transaction);
                var p = novaEmpresa.DadosPesquisa;
                AddParam(cmdPsq, "id", idEmpresaGerado);
                AddParam(cmdPsq, "aceita", p.AceitaPesquisa);
                AddParam(cmdPsq, "tel", p.TelPesquisa);
                AddParam(cmdPsq, "email", p.EmailPesquisa);
                AddParam(cmdPsq, "pl_emerg", p.PlanoEmergencia);
                AddParam(cmdPsq, "mul_lid", p.MulheresLideranca);
                AddParam(cmdPsq, "mul_emp", p.MulherEmpreendedora);
                AddParam(cmdPsq, "camp_amb", p.CampEducAmbiental);
                AddParam(cmdPsq, "fontes", p.UsoFontesRenovaveis);
                AddParam(cmdPsq, "selo", p.SeloSustentabilidade);
                AddParam(cmdPsq, "camp_res", p.CampReducaoResiduos);
                AddParam(cmdPsq, "prat_gest", p.PraticasGestaoSustentavel);
                AddParam(cmdPsq, "pl_hidr", p.PlanoRecursosHidricos);
                AddParam(cmdPsq, "pl_gest_amb", p.PlanoGestaoAmbiental);

                await cmdPsq.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return idEmpresaGerado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AtualizarEmpresaCompleta(EmpresaCompletaDTO novaEmpresa)
    {
        var estadoAtual = await PegarEmpresaCompletaPorId(novaEmpresa.DadosGerais.IdEmpresa);
        if (estadoAtual == null) 
            return false;

        // CORRIGIDO: Adicionado await using
        await using var conn = await CriarConexao();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            void AddParam(NpgsqlCommand cmd, string nome, object? valor) 
                => cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);

            if (MudouAlgo(estadoAtual.DadosGerais, novaEmpresa.DadosGerais))
            {
                var sqlGeral = @"
                    UPDATE EMPRESA_GERAL SET 
                        idtipo = (SELECT idtipo FROM TIPO_EMPRESA WHERE nometipo = @tipo LIMIT 1),
                        razao_social = @razao, nome_fantasia = @fantasia, data_abertura = @abertura, 
                        proprietarios = @prop, cnae = @cnae, cadastur = @cad, num_cadastur = @num_cad, 
                        venc_cadastur = @venc_cad, endereco = @end, bairro = @bairro, localizacao = @loc, 
                        regiao = @reg, tel_comercial = @tel, email_comercial = @email, site = @site, 
                        redes_sociais = @redes, func_fixos = @func_f, func_temporarios = @func_t
                    WHERE idempresa = @id;";

                await using (var cmdGeral = new NpgsqlCommand(sqlGeral, conn, transaction))
                {
                    var dg = novaEmpresa.DadosGerais;
                    AddParam(cmdGeral, "id", dg.IdEmpresa);
                    AddParam(cmdGeral, "tipo", dg.Tipo);
                    AddParam(cmdGeral, "razao", dg.RazaoSocial);
                    AddParam(cmdGeral, "fantasia", dg.NomeFantasia);
                    AddParam(cmdGeral, "abertura", dg.DataAbertura);
                    AddParam(cmdGeral, "prop", dg.Proprietarios);
                    AddParam(cmdGeral, "cnae", dg.Cnae);
                    AddParam(cmdGeral, "cad", dg.Cadastur);
                    AddParam(cmdGeral, "num_cad", dg.NumCadastur);
                    AddParam(cmdGeral, "venc_cad", dg.VencCadastur);
                    AddParam(cmdGeral, "end", dg.Endereco);
                    AddParam(cmdGeral, "bairro", dg.Bairro);
                    AddParam(cmdGeral, "loc", dg.Localizacao);
                    AddParam(cmdGeral, "reg", dg.Regiao);
                    AddParam(cmdGeral, "tel", dg.TelComercial);
                    AddParam(cmdGeral, "email", dg.EmailComercial);
                    AddParam(cmdGeral, "site", dg.Site);
                    AddParam(cmdGeral, "redes", dg.RedesSociais);
                    AddParam(cmdGeral, "func_f", dg.FuncFixos);
                    AddParam(cmdGeral, "func_t", dg.FuncTemporarios);
                    
                    await cmdGeral.ExecuteNonQueryAsync();
                }
            }

            if (MudouAlgo(estadoAtual.EstruturaAtual, novaEmpresa.EstruturaAtual))
            {
                var sqlEst = @"
                    INSERT INTO EMPRESA_ESTRUTURA (
                        idempresa, data, qtd_uhs, qtd_uhs_pcd, total_leitos, min_leitos_uh, max_leitos_uh,
                        func_24h, horario_checkin_checkout, sistema_reservas, formas_pagamento,
                        estacionamento, manobrista, mensageiro, area_fumantes, pet_friendly
                    ) VALUES (
                        @id, CURRENT_DATE, @uhs, @uhs_pcd, @leitos, @min_l, @max_l, @f24, @checkin,
                        @reservas, @pgto, @estac, @manob, @mensag, @fumo, @pet
                    );";

                await using var cmdEst = new NpgsqlCommand(sqlEst, conn, transaction);
                var e = novaEmpresa.EstruturaAtual;
                AddParam(cmdEst, "id", novaEmpresa.DadosGerais.IdEmpresa);
                AddParam(cmdEst, "uhs", e.QtdUhs);
                AddParam(cmdEst, "uhs_pcd", e.QtdUhsPcd);
                AddParam(cmdEst, "leitos", e.TotalLeitos);
                AddParam(cmdEst, "min_l", e.MinLeitosUh);
                AddParam(cmdEst, "max_l", e.MaxLeitosUh);
                AddParam(cmdEst, "f24", e.Func24h);
                AddParam(cmdEst, "checkin", e.HorarioCheckinCheckout);
                AddParam(cmdEst, "reservas", e.SistemaReservas);
                AddParam(cmdEst, "pgto", e.FormasPagamento);
                AddParam(cmdEst, "estac", e.Estacionamento);
                AddParam(cmdEst, "manob", e.Manobrista);
                AddParam(cmdEst, "mensag", e.Mensageiro);
                AddParam(cmdEst, "fumo", e.AreaFumantes);
                AddParam(cmdEst, "pet", e.PetFriendly);
                
                await cmdEst.ExecuteNonQueryAsync();
            }

            if (MudouAlgo(estadoAtual.Servicos, novaEmpresa.Servicos))
            {
                var sqlSrv = @"
                    INSERT INTO EMPRESA_SERVICOS (
                        idempresa, data, idioma_ingles, idioma_espanhol, outro_idioma, equip_uhs,
                        equip_recepcao, servicos_aeb, area_refeicoes, sanitario_aeb, alimentacao_diferenciada,
                        area_eventos, equip_eventos, aberto_publico, equip_lazer
                    ) VALUES (
                        @id, CURRENT_DATE, @ing, @esp, @outro, @e_uhs, @e_rec, @s_aeb, @a_ref, @san_aeb, 
                        @ali_dif, @a_evt, @e_evt, @pub, @e_laz
                    );";

                await using var cmdSrv = new NpgsqlCommand(sqlSrv, conn, transaction);
                var s = novaEmpresa.Servicos;
                AddParam(cmdSrv, "id", novaEmpresa.DadosGerais.IdEmpresa);
                AddParam(cmdSrv, "ing", s.IdiomaIngles);
                AddParam(cmdSrv, "esp", s.IdiomaEspanhol);
                AddParam(cmdSrv, "outro", s.OutroIdioma);
                AddParam(cmdSrv, "e_uhs", s.EquipUhs);
                AddParam(cmdSrv, "e_rec", s.EquipRecepcao);
                AddParam(cmdSrv, "s_aeb", s.ServicosAeb);
                AddParam(cmdSrv, "a_ref", s.AreaRefeicoes);
                AddParam(cmdSrv, "san_aeb", s.SanitarioAeb);
                AddParam(cmdSrv, "ali_dif", s.AlimentacaoDiferenciada);
                AddParam(cmdSrv, "a_evt", s.AreaEventos);
                AddParam(cmdSrv, "e_evt", s.EquipEventos);
                AddParam(cmdSrv, "pub", s.AbertoPublico);
                AddParam(cmdSrv, "e_laz", s.EquipLazer);

                await cmdSrv.ExecuteNonQueryAsync();
            }

            if (MudouAlgo(estadoAtual.Acessibilidade, novaEmpresa.Acessibilidade))
            {
                var sqlAcs = @"
                    INSERT INTO EMPRESA_ACESSIBILIDADE (
                        idempresa, data, facilidades_pcd, tipos_deficiencia, pessoal_capacitado, rota_externa,
                        embarque_desembarque, vaga_estacionamento, area_circulacao, escada, rampa, piso,
                        elevador, alarme_emergencia, locais_alarme, comunicacao_pcd, balcao_atendimento,
                        sanitario_adaptado, telefone_acessivel, sinalizacao_preferencial
                    ) VALUES (
                        @id, CURRENT_DATE, @facil, @tipos, @pes, @rota, @emb, @vaga, @circ, @esc, @ramp, @piso,
                        @elev, @alarme, @locais, @comun, @balc, @sanit, @tel, @sinal
                    );";

                await using var cmdAcs = new NpgsqlCommand(sqlAcs, conn, transaction);
                var a = novaEmpresa.Acessibilidade;
                AddParam(cmdAcs, "id", novaEmpresa.DadosGerais.IdEmpresa);
                AddParam(cmdAcs, "facil", a.FacilidadesPcd);
                AddParam(cmdAcs, "tipos", a.TiposDeficiencia);
                AddParam(cmdAcs, "pes", a.PessoalCapacitado);
                AddParam(cmdAcs, "rota", a.RotaExterna);
                AddParam(cmdAcs, "emb", a.EmbarqueDesembarque);
                AddParam(cmdAcs, "vaga", a.VagaEstacionamento);
                AddParam(cmdAcs, "circ", a.AreaCirculacao);
                AddParam(cmdAcs, "esc", a.Escada);
                AddParam(cmdAcs, "ramp", a.Rampa);
                AddParam(cmdAcs, "piso", a.Piso);
                AddParam(cmdAcs, "elev", a.Elevador);
                AddParam(cmdAcs, "alarme", a.AlarmeEmergencia);
                AddParam(cmdAcs, "locais", a.LocaisAlarme);
                AddParam(cmdAcs, "comun", a.ComunicacaoPcd);
                AddParam(cmdAcs, "balc", a.BalcaoAtendimento);
                AddParam(cmdAcs, "sanit", a.SanitarioAdaptado);
                AddParam(cmdAcs, "tel", a.TelefoneAcessivel);
                AddParam(cmdAcs, "sinal", a.SinalizacaoPreferencial);

                await cmdAcs.ExecuteNonQueryAsync();
            }

            if (MudouAlgo(estadoAtual.DadosPesquisa, novaEmpresa.DadosPesquisa))
            {
                var sqlPsq = @"
                    INSERT INTO EMPRESA_PESQUISA (
                        idempresa, data, aceita_pesquisa, tel_pesquisa, email_pesquisa, plano_emergencia,
                        mulheres_lideranca, mulher_empreendedora, camp_educ_ambiental, uso_fontes_renovaveis,
                        selo_sustentabilidade, camp_reducao_residuos, praticas_gestao_sustentavel,
                        plano_recursos_hidricos, plano_gestao_ambiental
                    ) VALUES (
                        @id, CURRENT_DATE, @aceita, @tel, @email, @pl_emerg, @mul_lid, @mul_emp, @camp_amb,
                        @fontes, @selo, @camp_res, @prat_gest, @pl_hidr, @pl_gest_amb
                    );";

                await using var cmdPsq = new NpgsqlCommand(sqlPsq, conn, transaction);
                var p = novaEmpresa.DadosPesquisa;
                AddParam(cmdPsq, "id", novaEmpresa.DadosGerais.IdEmpresa);
                AddParam(cmdPsq, "aceita", p.AceitaPesquisa);
                AddParam(cmdPsq, "tel", p.TelPesquisa);
                AddParam(cmdPsq, "email", p.EmailPesquisa);
                AddParam(cmdPsq, "pl_emerg", p.PlanoEmergencia);
                AddParam(cmdPsq, "mul_lid", p.MulheresLideranca);
                AddParam(cmdPsq, "mul_emp", p.MulherEmpreendedora);
                AddParam(cmdPsq, "camp_amb", p.CampEducAmbiental);
                AddParam(cmdPsq, "fontes", p.UsoFontesRenovaveis);
                AddParam(cmdPsq, "selo", p.SeloSustentabilidade);
                AddParam(cmdPsq, "camp_res", p.CampReducaoResiduos);
                AddParam(cmdPsq, "prat_gest", p.PraticasGestaoSustentavel);
                AddParam(cmdPsq, "pl_hidr", p.PlanoRecursosHidricos);
                AddParam(cmdPsq, "pl_gest_amb", p.PlanoGestaoAmbiental);

                await cmdPsq.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}