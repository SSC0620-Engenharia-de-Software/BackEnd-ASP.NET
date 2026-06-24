using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace src.Models;

public class TipoEmpresa
{
    [Key]
    public int IdTipo { get; set; }

    [MaxLength(100)]
    public string NomeTipo { get; set; }

    // Propriedade de navegação
    public virtual ICollection<EmpresaGeral> Empresas { get; set; }
}

public class EmpresaGeral
{
    [Key]
    public int IdEmpresa { get; set; }

    [MaxLength(100)]
    public string TipoEstabelecimento { get; set; }

    [MaxLength(255)]
    public string RazaoSocial { get; set; }

    [MaxLength(255)]
    public string NomeFantasia { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DataAbertura { get; set; }

    public string Proprietarios { get; set; } // Equivalente ao TEXT

    [MaxLength(20)]
    public string Cnae { get; set; }

    [MaxLength(50)]
    public string Cadastur { get; set; }

    [MaxLength(50)]
    public string NumCadastur { get; set; }

    [DataType(DataType.Date)]
    public DateTime? VencCadastur { get; set; }

    [MaxLength(255)]
    public string Endereco { get; set; }

    [MaxLength(100)]
    public string Bairro { get; set; }

    [MaxLength(100)]
    public string Localizacao { get; set; }

    [MaxLength(100)]
    public string Regiao { get; set; }

    [MaxLength(20)]
    public string TelComercial { get; set; }

    [MaxLength(150)]
    public string EmailComercial { get; set; }

    [MaxLength(255)]
    public string Site { get; set; }

    public string RedesSociais { get; set; } // Equivalente ao TEXT

    public int? FuncFixos { get; set; }
    
    public int? FuncTemporarios { get; set; }

    // Chave Estrangeira
    public int? IdTipo { get; set; }

    [ForeignKey("IdTipo")]
    public virtual TipoEmpresa TipoEmpresa { get; set; }

    // Propriedades de navegação para as tabelas dependentes
    // Automatiza joins
    public virtual ICollection<EmpresaPesquisa> PesquisasEmpresa { get; set; }
    public virtual ICollection<PesquisaHospedagem> PesquisasHospedagem { get; set; }
    public virtual ICollection<EmpresaServicos> Servicos { get; set; }
    public virtual ICollection<EmpresaAcessibilidade> Acessibilidades { get; set; }
    public virtual ICollection<EmpresaEstrutura> Estruturas { get; set; }
    public virtual ICollection<Pesquisa> PesquisasConsolidadas { get; set; }
}