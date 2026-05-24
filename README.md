# Repositório do Backend ASP.NET

Repositório responsável por:
- API backend em ASP.NET Core
- conexão com banco PostgreSQL via Npgsql
- execução em Docker
- endpoints REST para testes e evolução do sistema
- base para autenticação e CRUDs futuros

---

# Estrutura do Projeto

```text
project-root/
│
├── Dockerfile
├── docker-compose.yaml
├── .dockerignore
│
└── src/
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Docker.json
    │
    ├── Properties/
    │   └── launchSettings.json
    │
    ├── Services/
    │   └── PostgreTesteConexao.cs
    │
    ├── Models/
    │
    └── src.csproj
```

---

# Descrição dos Arquivos
## Dockerfile

Responsável por:

- restaurar dependências (.NET restore)
- compilar o projeto
- publicar a aplicação
- executar o backend dentro do container

Fluxo típico:

```bash
dotnet restore
dotnet publish -c Release -o out
dotnet out/src.dll
```

## docker-compose.yaml

Responsável por:

- criar o container do backend
- expor a porta da API
- definir variáveis de ambiente
- configurar execução em ambiente de desenvolvimento

## .dockerignore

Responsável por excluir arquivos desnecessários no build da imagem Docker.

## src/

Pasta com os arquivos do projeto ASP.net.

## src/Models/

Modelos das tabelas na base de dados.

## src/Services/

Códigos de interação com a base de dados.

## src/appsettings.Docker.json

Configurações do projeto ASP.net quando for criado no docker.

```Json
"Postgres": "Host=host.docker.internal;Port=5433;Username=postgres;Password=1234;Database=DB-OLIMPIA"
```

Ele usa o localhost do computador para acessar a base de dados, ao invés, do localhost do docker.

## src/appsettings.json

Configurações do projeto ASP.net quando for criado na máquina local.

## src/Program.cs

Código principal do projeto ASP.net. Ele quem define os links _http_ com os códigos.

---

# Iniciar o código
## Em máquina Local

No prompt de comando, dentro da pasta _src_:

```Bash
dotnet run
```

## No docker

No prompt de comando, dentro da pasta raíz:

```Bash
docker compose up --build
```