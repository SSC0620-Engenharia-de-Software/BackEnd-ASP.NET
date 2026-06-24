using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace src.Models;

public class PessoaVisitante
{
    [Key]
    public int IdPessoa { get; set; }

    [Required]
    [MaxLength(255)]
    public string Senha { get; set; }
}

public class PessoaAdmin
{
    [Key]
    public int IdPessoa { get; set; }

    [Required]
    [MaxLength(255)]
    public string Senha { get; set; }
}

public class PessoaFuncionario
{
    [Key]
    public int IdPessoa { get; set; }

    [Required]
    [MaxLength(255)]
    public string Senha { get; set; }
}

public class PessoaEmpresario
{
    [Key]
    public int IdPessoa { get; set; }

    [Required]
    [MaxLength(255)]
    public string Senha { get; set; }

    [Required]
    public int IdEmpresa;
}