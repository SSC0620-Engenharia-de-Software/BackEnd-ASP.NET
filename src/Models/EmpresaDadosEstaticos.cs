using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace src.Models;

public class EmpresaServicos
{
    [Key]
    public int IdServicos { get; set; }

    public bool? IdiomaIngles { get; set; }
    public bool? IdiomaEspanhol { get; set; }

    [MaxLength(100)]
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

    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    public int? IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual EmpresaGeral Empresa { get; set; }
}

public class EmpresaAcessibilidade
{
    [Key]
    public int IdAcessibilidade { get; set; }

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

    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    public int? IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual EmpresaGeral Empresa { get; set; }
}

public class EmpresaEstrutura
{
    [Key]
    public int IdEstrutura { get; set; }

    public int? QtdUhs { get; set; }
    public int? QtdUhsPcd { get; set; }
    public int? TotalLeitos { get; set; }
    public int? MinLeitosUh { get; set; }
    public int? MaxLeitosUh { get; set; }
    public bool? Func24h { get; set; }

    [MaxLength(100)]
    public string HorarioCheckinCheckout { get; set; }

    public string SistemaReservas { get; set; }
    public string FormasPagamento { get; set; }
    public bool? Estacionamento { get; set; }
    public bool? Manobrista { get; set; }
    public bool? Mensageiro { get; set; }
    public bool? AreaFumantes { get; set; }
    public bool? PetFriendly { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    public int? IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual EmpresaGeral Empresa { get; set; }
}

public class Pesquisa
{
    [Key]
    public int IdConsolidada { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TaxaOcupacaoReal { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? ReceitaReal { get; set; }

    public int? QtdTuristasReal { get; set; }

    public int? IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual EmpresaGeral Empresa { get; set; }
}