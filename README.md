# Encontro: sistema de eventos em ASP.NET Core MVC

Atividade 08: desenvolvimento de um sistema web com **ASP.NET Core MVC**, **Entity Framework Core**, **SQL Server** e **ASP.NET Core Identity**.

**Contexto escolhido:** Eventos (Categorias, Eventos e Inscrições). O usuário consulta os eventos e se inscreve; o administrador gerencia os cadastros e acompanha as inscrições.

---

## 1. Descrição do contexto

O **Encontro** é um portal de eventos da cidade: feiras, workshops, shows, palestras, corridas e festivais. Cada evento pertence a uma **categoria** e tem data de início e término, local, número de vagas, preço (ou gratuito) e imagem.

| Perfil | O que faz |
|---|---|
| **Visitante** | Consulta o catálogo (busca, filtro por categoria e "só com vagas") e os detalhes dos eventos. |
| **Usuário** (`Usuario`) | Cria a conta, entra e sai do sistema, **se inscreve** nos eventos, acompanha **as próprias inscrições** (com filtro por status), cancela antes do início e edita os dados da conta. |
| **Administrador** (`Admin`) | Painel com indicadores, **CRUD completo de Categorias e de Eventos** (com upload de imagem), consulta das **inscrições de todos os usuários** com busca e **atualização de status**, e consulta das contas cadastradas. |

Status de uma inscrição: **Pendente** → **Confirmada** → **Concluída** (participou), ou **Cancelada**.
Eventos gratuitos são confirmados na hora. Nos eventos pagos, a inscrição fica pendente até o administrador confirmar o pagamento.

---

## 2. Regras de negócio implementadas

Todas são verificadas **no controller, antes de gravar no banco**. As regras 1 a 3 também são conferidas de novo no POST, para não depender só da tela.

| # | Regra | Onde |
|---|---|---|
| 1 | **Inscrição sem duplicidade**: o usuário não pode ter duas inscrições ativas no mesmo evento. Também existe um índice único `EventoId + UsuarioId` no banco. | `Controllers/InscricoesController.cs` → `VerificarRegrasAsync` |
| 2 | **Vagas disponíveis**: inscrições ativas (pendente, confirmada ou concluída) não podem passar do número de vagas. | `InscricoesController.VerificarRegrasAsync` |
| 3 | **Bloqueio após o encerramento**: não aceita inscrição em evento que já começou ou que não está publicado. | `InscricoesController.VerificarRegrasAsync` |
| 4 | **Cancelamento antes do início**: o usuário só cancela a própria inscrição, e só antes do início do evento. | `InscricoesController.Cancelar` |
| 5 | **Preço preservado**: o valor do evento é gravado na inscrição no momento em que ela é feita. | `InscricoesController.Nova` (POST) |
| 6 | **Datas coerentes**: o término precisa ser depois do início (`IValidatableObject` no Model), e o início de um evento novo precisa ser uma data futura. | `Models/Evento.cs`, `Areas/Admin/Controllers/EventosController.Create` |
| 7 | **Vagas x inscritos**: ao editar, o número de vagas não pode ficar menor que o de inscrições ativas. | `Admin/EventosController.Edit` |
| 8 | **Exclusões protegidas**: categoria com eventos e evento com inscrições ativas não podem ser excluídos. | `Admin/CategoriasController`, `Admin/EventosController` |
| 9 | **Status do admin**: para reativar uma inscrição cancelada é preciso ter vaga e o evento não pode ter começado; "Concluída" só depois do início do evento. | `Admin/InscricoesController.AtualizarStatus` |
| 10 | **Categoria sem nome repetido** (verificação no controller + índice único). | `Admin/CategoriasController` |

**Validações e mensagens:** Data Annotations com mensagens em português em todos os Models e ViewModels, validação no cliente (jQuery Validate, ajustado para vírgula decimal) e no servidor, mensagens do Identity traduzidas (`Services/IdentityErrorDescriberPtBr.cs`) e alertas de resultado (sucesso, erro, aviso) via `TempData`.

---

## 3. Modelo de dados

```
tbCategorias 1 ──── N tbEventos 1 ──── N tbInscricoes N ──── 1 AspNetUsers
```

| Tabela | Principais campos |
|---|---|
| `tbCategorias` | `Id` (PK), `Nome` (único), `Descricao` |
| `tbEventos` | `Id` (PK), `Titulo`, `Descricao`, `DataInicio`, `DataFim`, `Local`, `Vagas`, `Preco`, `ImagemUrl`, `Ativo`, `CategoriaId` (FK) |
| `tbInscricoes` | `Id` (PK), `EventoId` (FK), `UsuarioId` (FK → AspNetUsers), `DataInscricao`, `Status`, `Valor`, `Observacao`, `DataAtualizacao` |
| `AspNetUsers` | Tabela do Identity + `NomeCompleto` e `DataCadastro` (`Models/ApplicationUser.cs`) |

Comportamento das exclusões: Categoria → Evento: *Restrict*. Evento → Inscrição: *Cascade*, mas o controller bloqueia quando há inscrições ativas. Usuário → Inscrição: *Restrict*.

---

## 4. Tecnologias

- .NET 10 / ASP.NET Core MVC (Controllers, Views Razor e Areas)
- Entity Framework Core 10 + SQL Server (Code First com migrações)
- ASP.NET Core Identity com perfis (`[Authorize]` e `[Authorize(Roles = "Admin")]`)
- Bootstrap 5.3 + Bootstrap Icons (formulários, tabelas, cards e botões)

## 5. Estrutura do projeto

```
SistemaEventos.sln
Database/
  populacao.sql                 ← script SQL de população
SistemaEventos/
  Areas/Admin/                  ← área administrativa (somente perfil Admin)
    Controllers/                  Painel, Categorias, Eventos, Inscricoes, Usuarios
    Views/
  Controllers/                  ← Home, Eventos (catálogo), Inscricoes (usuário), Conta (Identity)
  Data/
    ApplicationDbContext.cs     ← DbSets, tabelas e relacionamentos
    DbInitializer.cs            ← aplica migrações, cria perfis e o admin padrão
  Migrations/                   ← migração CriacaoInicial
  Models/                       ← Categoria, Evento, Inscricao, ApplicationUser, StatusInscricao
  ViewModels/                   ← modelos das telas (catálogo, formulários, painel...)
  Services/                     ← upload de imagens, mensagens do Identity em PT-BR
  Views/                        ← telas públicas e do usuário
  wwwroot/                      ← CSS, JS e imagens dos eventos
```

---

## 6. Como configurar e executar

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (Express, Developer ou LocalDB) e, se quiser, SSMS ou Azure Data Studio
- Ferramenta do EF Core: `dotnet tool install --global dotnet-ef`
- Internet ao abrir o site (Bootstrap, ícones e fonte são carregados por CDN)

### 6.1 Conexão com o banco

Edite `SistemaEventos/appsettings.json`, chave `ConexaoSqlServer`:

```json
"ConnectionStrings": {
  "ConexaoSqlServer": "Data Source=localhost;Initial Catalog=dbSistemaEventos;Integrated Security=True;TrustServerCertificate=True"
}
```

| Seu SQL Server | `Data Source=` |
|---|---|
| Instância padrão (nome do computador) | `localhost` ou `NOME-DO-PC` |
| SQL Server Express | `localhost\\SQLEXPRESS` |
| LocalDB (Visual Studio) | `(localdb)\\MSSQLLocalDB` |
| Docker / login SQL | `localhost,1433;User ID=sa;Password=SuaSenha;` (no lugar de `Integrated Security=True`) |

> No JSON a barra invertida é escrita em dobro, como nos exemplos acima.

### 6.2 Criar o banco com as migrações

**VS Code / terminal** (dentro da pasta `SistemaEventos`, onde está o `.csproj`):

```bash
cd SistemaEventos
dotnet restore
dotnet ef database update
```

**Visual Studio**: *Ferramentas → Gerenciador de Pacotes do NuGet → Console do Gerenciador de Pacotes*:

```
Update-Database
```

> A aplicação também aplica migrações pendentes automaticamente ao iniciar e cria os perfis **Admin** e **Usuario** e a conta de administrador padrão (`Data/DbInitializer.cs`).

### 6.3 Popular o banco com os dados de demonstração

Abra `Database/populacao.sql` no SSMS ou no Azure Data Studio e execute (F5), ou pelo terminal, na raiz do repositório:

```bash
sqlcmd -S localhost -E -f 65001 -i Database/populacao.sql
```

O script cria 5 contas, 6 categorias, 9 eventos e 17 inscrições, com datas calculadas a partir do dia em que é executado. Ele pode ser executado mais de uma vez sem duplicar dados.

### 6.4 Executar

```bash
dotnet run
```

Acesse **http://localhost:5080** (ou a porta mostrada no terminal). No Visual Studio, use **F5**.

---

## 7. Contas para teste

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@eventos.com` | `Admin@123` |
| Usuário | `ana@email.com` | `Usuario@123` |
| Usuário | `joao@email.com` | `Usuario@123` |
| Usuário | `maria@email.com` | `Usuario@123` |
| Usuário | `carlos@email.com` | `Usuario@123` |

Novas contas criadas pela tela **Criar conta** recebem automaticamente o perfil **Usuario**.

---

## 8. Roteiro sugerido para a demonstração

1. **Cadastro e login:** clique em *Criar conta*, cadastre um usuário novo (teste também uma senha fraca e um e-mail repetido para ver as validações) e entre no sistema.
2. **Operação principal:** em *Eventos*, busque "música" ou filtre por categoria, abra um evento e clique em **Inscrever-se** → **Confirmar inscrição**. A inscrição aparece em *Minhas inscrições*.
3. **Regras:**
   - Tente se inscrever de novo no mesmo evento → *"Você já está inscrito(a)"*.
   - Entre como `carlos@email.com` e tente a **Oficina de Café Especial** (3 vagas, lotada) → *"Não há mais vagas"*.
   - O **Festival Gastronômico de Inverno** já foi realizado e não aparece no catálogo. Pegue o Id dele na lista de eventos do admin (ou no resultado do script SQL) e acesse `/Inscricoes/Nova/{id}` → *"As inscrições para este evento estão encerradas"*.
   - Cancele uma inscrição e veja a vaga ser liberada.
4. **Administração:** entre como `admin@eventos.com` → *Painel*. Crie um evento com imagem, edite, veja os detalhes e tente excluir um evento com inscritos (bloqueado). Em *Inscrições*, filtre por **Pendente**, abra uma inscrição e altere o status para **Confirmada**.
5. **Permissões:** logado como usuário comum, digite `/Admin/Eventos` na barra de endereço → **Acesso negado**. Sem login, acesse `/Inscricoes` → redireciona para o **login**. Logado como admin, acesse `/Inscricoes/Nova/1` → **Acesso negado** (inscrição é exclusiva do perfil Usuário).

### Rotas protegidas

| Rota | Proteção |
|---|---|
| `/Inscricoes/*` | `[Authorize(Roles = "Usuario")]` |
| `/Conta/MinhaConta`, `/Conta/AlterarSenha` | `[Authorize]` |
| `/Admin/*` (Painel, Categorias, Eventos, Inscrições, Usuários) | `[Authorize(Roles = "Admin")]` |
| `/`, `/Eventos`, `/Eventos/Details/{id}` | Públicas |

---

## 9. Publicar no GitHub

```bash
git init
git add .
git commit -m "Sistema de eventos em ASP.NET Core MVC"
git branch -M main
git remote add origin https://github.com/SEU-USUARIO/sistema-eventos.git
git push -u origin main
```

## 10. Observações

- **Gerar o script SQL do esquema** (opcional): `dotnet ef migrations script -o Database/esquema.sql`.
- **Recriar a migração** (se preferir gerar pelo seu ambiente): apague a pasta `Migrations` e rode `dotnet ef migrations add CriacaoInicial`, depois `dotnet ef database update`.
- **Usando .NET 8**: troque `net10.0` por `net8.0` e os pacotes `10.0.0` por `8.0.11` no `.csproj`, e remova a linha `.ConfigureWarnings(...)` do `Program.cs` (esse aviso só existe no EF Core 9 ou superior).
- As imagens enviadas pelo cadastro ficam em `wwwroot/uploads/eventos` (pasta ignorada pelo Git). As imagens de demonstração ficam em `wwwroot/images/eventos`.
