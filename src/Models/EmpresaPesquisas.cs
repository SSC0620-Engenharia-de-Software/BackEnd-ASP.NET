using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace src.Models;

public class EmpresaPesquisa
{
    [Key]
    public int IdPesquisaEmp { get; set; }

    public bool? AceitaPesquisa { get; set; }

    [MaxLength(20)]
    public string TelPesquisa { get; set; }

    [MaxLength(150)]
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

    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    public int? IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual EmpresaGeral Empresa { get; set; }
}

public class PesquisaHospedagem
{
    [Key]
    public int IdPesquisa { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TaxaOcup { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiariaMedia { get; set; }

    public int? QtdHospedes { get; set; }
    public int? QtdLeitos { get; set; }
    public int? QtdUhs { get; set; }

    [MaxLength(100)]
    public string TipoPesquisa { get; set; }

    public int? IdEmpresa { get; set; }

    [ForeignKey("IdEmpresa")]
    public virtual EmpresaGeral Empresa { get; set; }
}