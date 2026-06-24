using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace src.Models;

public class TipoEmpresaDTO
{
    public int IdTipo { get; set; }
    public string NomeTipo { get; set; }
}

public class RelatorioNumeroEmpresasDTO
{
    public DateTime MesAnalise { get; set; }
    public int QuantidadeEmpresas { get; set; }
}

public class RelatorioLeitosDTO
{
    public DateTime MesAnalise { get; set; }
    public int TotalLeitos { get; set; }
}

public class RelatorioDiariaMediaDTO
{
    public DateTime MesAnalise { get; set; }
    public decimal ValorDiaria { get; set; }
}

public class PesquisaHospedagemDTO
{
    public int IdEmpresa { get; set; }
    public DateTime Data { get; set; }
    public decimal? TaxaOcupacao { get; set; }
    public decimal? DiariaMedia { get; set; }
    public int? QtdHospedes { get; set; }
    public int? QtdLeitos { get; set; }
    public int? QtdUhs { get; set; }
}

public class EmpresaCompletaDTO
{
    public EmpresaGeralDTO DadosGerais { get; set; }
    public EmpresaPesquisaDTO DadosPesquisa { get; set; }
    public EmpresaEstruturaDTO EstruturaAtual { get; set; }
    public EmpresaServicosDTO Servicos { get; set; }
    public EmpresaAcessibilidadeDTO Acessibilidade { get; set; }
}

// 1. DTO de Dados Gerais
public class EmpresaGeralDTO
{
    public int IdEmpresa { get; set; }
    
    // Alterado para string conforme solicitado
    public string Tipo { get; set; } 
    
    public string TipoEstabelecimento { get; set; }
    public string RazaoSocial { get; set; }
    public string NomeFantasia { get; set; }
    public DateTime? DataAbertura { get; set; }
    public string Proprietarios { get; set; }
    public string Cnae { get; set; }
    public string Cadastur { get; set; }
    public string NumCadastur { get; set; }
    public DateTime? VencCadastur { get; set; }
    public string Endereco { get; set; }
    public string Bairro { get; set; }
    public string Localizacao { get; set; }
    public string Regiao { get; set; }
    public string TelComercial { get; set; }
    public string EmailComercial { get; set; }
    public string Site { get; set; }
    public string RedesSociais { get; set; }
    public int? FuncFixos { get; set; }
    public int? FuncTemporarios { get; set; }
}

// 2. DTO de Pesquisa e Sustentabilidade
public class EmpresaPesquisaDTO
{
    public bool? AceitaPesquisa { get; set; }
    public string TelPesquisa { get; set; }
    public string EmailPesquisa { get; set; }
    public bool? PlanoEmergencia { get; set; }
    public bool? MulheresLideranca { get; set; }
    public bool? MulherEmpreendedora { get; set; }
    public bool? CampEducAmbiental { get; set; }
    public bool? UsoFontesRenovaveis { get; set; }
    public bool? SeloSustentabilidade { get; set; }
    public bool? CampReducaoResiduos { get; set; }
    public bool? PraticasGestaoSustentavel { get; set; }
    public bool? PlanoRecursosHidricos { get; set; }
    public bool? PlanoGestaoAmbiental { get; set; }
    public DateTime? Data { get; set; }
}

// 3. DTO de Serviços
public class EmpresaServicosDTO
{
    public bool? IdiomaIngles { get; set; }
    public bool? IdiomaEspanhol { get; set; }
    public string OutroIdioma { get; set; }
    public string EquipUhs { get; set; }
    public string EquipRecepcao { get; set; }
    public string ServicosAeb { get; set; }
    public bool? AreaRefeicoes { get; set; }
    public bool? SanitarioAeb { get; set; }
    public string AlimentacaoDiferenciada { get; set; }
    public bool? AreaEventos { get; set; }
    public string EquipEventos { get; set; }
    public bool? AbertoPublico { get; set; }
    public string EquipLazer { get; set; }
    public DateTime? Data { get; set; }
}

// 4. DTO de Acessibilidade
public class EmpresaAcessibilidadeDTO
{
    public bool? FacilidadesPcd { get; set; }
    public string TiposDeficiencia { get; set; }
    public bool? PessoalCapacitado { get; set; }
    public bool? RotaExterna { get; set; }
    public bool? EmbarqueDesembarque { get; set; }
    public bool? VagaEstacionamento { get; set; }
    public bool? AreaCirculacao { get; set; }
    public bool? Escada { get; set; }
    public bool? Rampa { get; set; }
    public bool? Piso { get; set; }
    public bool? Elevador { get; set; }
    public bool? AlarmeEmergencia { get; set; }
    public string LocaisAlarme { get; set; }
    public string ComunicacaoPcd { get; set; }
    public bool? BalcaoAtendimento { get; set; }
    public bool? SanitarioAdaptado { get; set; }
    public bool? TelefoneAcessivel { get; set; }
    public bool? SinalizacaoPreferencial { get; set; }
    public DateTime? Data { get; set; }
}

// 5. DTO de Estrutura
public class EmpresaEstruturaDTO
{
    public int? QtdUhs { get; set; }
    public int? QtdUhsPcd { get; set; }
    public int? TotalLeitos { get; set; }
    public int? MinLeitosUh { get; set; }
    public int? MaxLeitosUh { get; set; }
    public bool? Func24h { get; set; }
    public string HorarioCheckinCheckout { get; set; }
    public string SistemaReservas { get; set; }
    public string FormasPagamento { get; set; }
    public bool? Estacionamento { get; set; }
    public bool? Manobrista { get; set; }
    public bool? Mensageiro { get; set; }
    public bool? AreaFumantes { get; set; }
    public bool? PetFriendly { get; set; }
    public DateTime? Data { get; set; }
}

public class LoginRequestDTO
{
    public string Id { get; set; }
    public string Senha { get; set; }
}

public class ResultadoLoginDTO
{
    public string Categoria { get; set; }
    
    public int? IdEmpresa { get; set; }
}