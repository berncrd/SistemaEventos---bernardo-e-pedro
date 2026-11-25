/* ============================================================================
   SISTEMA DE EVENTOS (Encontro) — Script de população do banco de dados
   ----------------------------------------------------------------------------
   Execute DEPOIS de criar o banco com as migrações:
       dotnet ef database update        (ou  Update-Database  no Visual Studio)

   Como executar:
     • SSMS / Azure Data Studio: abra este arquivo, confira o nome do banco no
       comando USE abaixo e execute (F5).
     • Terminal: sqlcmd -S localhost -E -f 65001 -i Database/populacao.sql

   O script pode ser executado mais de uma vez: perfis e usuários só são
   criados se ainda não existirem, e os dados de eventos só são inseridos
   quando a tabela de categorias estiver vazia.

   Contas criadas
     Administrador ........ admin@eventos.com   / Admin@123
     Usuários ............. ana@email.com       / Usuario@123
                            joao@email.com      / Usuario@123
                            maria@email.com     / Usuario@123
                            carlos@email.com    / Usuario@123

   As datas dos eventos são calculadas a partir da data atual, então o
   catálogo sempre terá eventos futuros quando o script for executado.

   Imagens: fotos reais do Unsplash (licença Unsplash, uso gratuito),
   carregadas pela internet. Créditos no README.md.
   ============================================================================ */

USE dbSistemaEventos;   -- ajuste se o "Initial Catalog" da sua conexão for outro
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Agora datetime2 = SYSDATETIME();
DECLARE @Hoje  datetime2 = CAST(CAST(@Agora AS date) AS datetime2);

/* ----------------------------------------------------------------------------
   1) PERFIS (AspNetRoles)
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'ADMIN')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (CONVERT(nvarchar(450), NEWID()), N'Admin', N'ADMIN', CONVERT(nvarchar(36), NEWID()));

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'USUARIO')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (CONVERT(nvarchar(450), NEWID()), N'Usuario', N'USUARIO', CONVERT(nvarchar(36), NEWID()));

DECLARE @PerfilAdmin   nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE NormalizedName = N'ADMIN');
DECLARE @PerfilUsuario nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE NormalizedName = N'USUARIO');

/* ----------------------------------------------------------------------------
   2) USUÁRIOS (AspNetUsers + AspNetUserRoles)
      PasswordHash no formato do ASP.NET Core Identity (PBKDF2-HMACSHA512, 100.000 iterações)
        Admin@123   -> hash do administrador
        Usuario@123 -> hash dos usuários comuns
   ---------------------------------------------------------------------------- */
DECLARE @HashAdmin   nvarchar(max) = N'AQAAAAIAAYagAAAAEPCbk7zusXZqAxUsXG+askfLRJccCd53OGZLSO7LgAsUk1AHqKYF8e04sBEv+CKy6Q==';
DECLARE @HashUsuario nvarchar(max) = N'AQAAAAIAAYagAAAAEEXrEOtmxOrZ32qVL8+/oGCm5CH2B6eOLe9Gvb8RlHtYYOVt0WkG+l84RkBOR7W1wA==';

DECLARE @Usuarios TABLE (
    Nome      nvarchar(100),
    Email     nvarchar(256),
    Telefone  nvarchar(20),
    PerfilId  nvarchar(450),
    Hash      nvarchar(max),
    Cadastro  datetime2
);

INSERT INTO @Usuarios (Nome, Email, Telefone, PerfilId, Hash, Cadastro) VALUES
    (N'Administrador do Sistema', N'admin@eventos.com', N'(11) 3000-0000',  @PerfilAdmin,   @HashAdmin,   DATEADD(DAY, -60, @Agora)),
    (N'Ana Souza',                N'ana@email.com',     N'(11) 98888-1111', @PerfilUsuario, @HashUsuario, DATEADD(DAY, -30, @Agora)),
    (N'João Lima',                N'joao@email.com',    N'(11) 97777-2222', @PerfilUsuario, @HashUsuario, DATEADD(DAY, -25, @Agora)),
    (N'Maria Costa',              N'maria@email.com',   N'(21) 96666-3333', @PerfilUsuario, @HashUsuario, DATEADD(DAY, -22, @Agora)),
    (N'Carlos Pereira',           N'carlos@email.com',  NULL,               @PerfilUsuario, @HashUsuario, DATEADD(DAY, -10, @Agora));

INSERT INTO AspNetUsers
    (Id, NomeCompleto, DataCadastro, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
     LockoutEnd, LockoutEnabled, AccessFailedCount)
SELECT
    CONVERT(nvarchar(450), NEWID()), u.Nome, u.Cadastro, u.Email, UPPER(u.Email), u.Email, UPPER(u.Email), 1,
    u.Hash, REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''), CONVERT(nvarchar(36), NEWID()), u.Telefone, 0, 0,
    NULL, 1, 0
FROM @Usuarios u
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers x WHERE x.NormalizedEmail = UPPER(u.Email));

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT x.Id, u.PerfilId
FROM @Usuarios u
JOIN AspNetUsers x ON x.NormalizedEmail = UPPER(u.Email)
WHERE NOT EXISTS (SELECT 1 FROM AspNetUserRoles r WHERE r.UserId = x.Id AND r.RoleId = u.PerfilId);

/* ----------------------------------------------------------------------------
   3) CATEGORIAS, EVENTOS E INSCRIÇÕES (somente se o banco ainda não tiver dados)
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM tbCategorias)
BEGIN
    /* ---- Categorias ---- */
    INSERT INTO tbCategorias (Nome, Descricao) VALUES
        (N'Tecnologia',  N'Feiras, hackathons e encontros sobre inovação, programação e transformação digital.'),
        (N'Música',      N'Shows, apresentações ao ar livre e noites temáticas.'),
        (N'Workshops',   N'Oficinas práticas para aprender uma nova habilidade em poucas horas.'),
        (N'Negócios',    N'Palestras e encontros sobre empreendedorismo, carreira e gestão.'),
        (N'Gastronomia', N'Festivais, degustações e oficinas culinárias.'),
        (N'Esportes',    N'Corridas, torneios e atividades físicas ao ar livre.');

    DECLARE @CatTecnologia  int = (SELECT Id FROM tbCategorias WHERE Nome = N'Tecnologia');
    DECLARE @CatMusica      int = (SELECT Id FROM tbCategorias WHERE Nome = N'Música');
    DECLARE @CatWorkshops   int = (SELECT Id FROM tbCategorias WHERE Nome = N'Workshops');
    DECLARE @CatNegocios    int = (SELECT Id FROM tbCategorias WHERE Nome = N'Negócios');
    DECLARE @CatGastronomia int = (SELECT Id FROM tbCategorias WHERE Nome = N'Gastronomia');
    DECLARE @CatEsportes    int = (SELECT Id FROM tbCategorias WHERE Nome = N'Esportes');

    /* ---- Eventos ----
       DATEADD(MINUTE, h*60+m, DATEADD(DAY, d, @Hoje)) = hoje + d dias, às h:m */
    INSERT INTO tbEventos (Titulo, Descricao, DataInicio, DataFim, Local, Vagas, Preco, ImagemUrl, Ativo, CategoriaId) VALUES
    (N'Feira de Tecnologia',
     N'Um dia inteiro com estandes de startups, demonstrações de robótica e realidade virtual e palestras rápidas sobre inteligência artificial, segurança da informação e carreira em TI.

Entrada gratuita mediante inscrição. Traga seu currículo: empresas parceiras estarão recrutando estagiários.',
     DATEADD(MINUTE, 9*60,  DATEADD(DAY, 5, @Hoje)), DATEADD(MINUTE, 18*60, DATEADD(DAY, 5, @Hoje)),
     N'Centro de Convenções', 150, 0.00, N'https://images.unsplash.com/photo-1605810230434-7631ac76ec81?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatTecnologia),

    (N'Workshop de Fotografia',
     N'Aprenda a dominar o modo manual da câmera: abertura, velocidade e ISO na prática. A turma faz exercícios de retrato e de fotografia de rua, com análise das fotos ao final.

Traga sua câmera ou celular. Turma reduzida para garantir atenção individual.',
     DATEADD(MINUTE, 14*60, DATEADD(DAY, 12, @Hoje)), DATEADD(MINUTE, 18*60, DATEADD(DAY, 12, @Hoje)),
     N'Espaço Cultural', 20, 89.90, N'https://images.unsplash.com/photo-1543954616-be267def7835?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatWorkshops),

    (N'Música no Parque',
     N'Tarde de música ao vivo com bandas locais de MPB, rock e choro. Leve sua canga e aproveite o pôr do sol com food trucks e área kids.

Evento gratuito com lugares limitados na área próxima ao palco.',
     DATEADD(MINUTE, 16*60, DATEADD(DAY, 19, @Hoje)), DATEADD(MINUTE, 21*60, DATEADD(DAY, 19, @Hoje)),
     N'Parque Municipal', 80, 0.00, N'https://images.unsplash.com/photo-1630140776983-807d6d1bb9d6?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatMusica),

    (N'Palestra: Empreendedorismo Digital',
     N'Como validar uma ideia, conquistar os primeiros clientes e organizar as finanças de um negócio digital. Palestra com fundadores de empresas da região, seguida de rodada de perguntas e networking.',
     DATEADD(MINUTE, 19*60, DATEADD(DAY, 9, @Hoje)), DATEADD(MINUTE, 21*60+30, DATEADD(DAY, 9, @Hoje)),
     N'Auditório Central', 120, 30.00, N'https://images.unsplash.com/photo-1492538368677-f6e0afe31dcc?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatNegocios),

    (N'Oficina de Café Especial',
     N'Conheça os métodos de preparo coado, prensa francesa e aeropress, aprenda a moer e dosar o café e participe de uma degustação guiada por um barista.

Vagas bem limitadas: a oficina acontece no balcão do café-escola.',
     DATEADD(MINUTE, 10*60, DATEADD(DAY, 15, @Hoje)), DATEADD(MINUTE, 12*60, DATEADD(DAY, 15, @Hoje)),
     N'Café Escola', 3, 60.00, N'https://images.unsplash.com/photo-1504469089401-14f795f6ddee?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatGastronomia),

    (N'Corrida Solidária 5K',
     N'Percurso de 5 km pela orla com largada às 7h. Parte do valor da inscrição é doada para instituições de acolhimento de animais.

Kit com camiseta, número de peito e medalha de participação.',
     DATEADD(MINUTE, 7*60, DATEADD(DAY, 26, @Hoje)), DATEADD(MINUTE, 10*60, DATEADD(DAY, 26, @Hoje)),
     N'Orla da Cidade', 300, 45.00, N'https://images.unsplash.com/photo-1596460456678-760115935178?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatEsportes),

    (N'Hackathon Cidades Inteligentes',
     N'Maratona de 34 horas para criar soluções de tecnologia para mobilidade, energia e serviços públicos. Equipes de até 5 pessoas, mentorias durante todo o evento e premiação para os três melhores projetos.',
     DATEADD(MINUTE, 8*60, DATEADD(DAY, 33, @Hoje)), DATEADD(MINUTE, 18*60, DATEADD(DAY, 34, @Hoje)),
     N'Campus Tech', 60, 0.00, N'https://images.unsplash.com/photo-1641951820920-c90394aef512?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatTecnologia),

    (N'Festival Gastronômico de Inverno',
     N'Edição de inverno do festival com mais de 20 restaurantes da cidade, pratos especiais a preços populares e apresentações musicais. Evento já realizado.',
     DATEADD(MINUTE, 11*60, DATEADD(DAY, -12, @Hoje)), DATEADD(MINUTE, 22*60, DATEADD(DAY, -12, @Hoje)),
     N'Praça Central', 200, 0.00, N'https://images.unsplash.com/photo-1506159094651-959dec66fac6?w=1200&h=675&fit=crop&auto=format&q=80', 1, @CatGastronomia),

    (N'Noite de Jazz',
     N'Quarteto convidado apresenta clássicos do jazz e da bossa nova. Evento em preparação: ainda não publicado no catálogo.',
     DATEADD(MINUTE, 20*60, DATEADD(DAY, 40, @Hoje)), DATEADD(MINUTE, 23*60+30, DATEADD(DAY, 40, @Hoje)),
     N'Teatro Municipal', 150, 50.00, N'https://images.unsplash.com/photo-1499314722718-0827eb4deb71?w=1200&h=675&fit=crop&auto=format&q=80', 0, @CatMusica);

    DECLARE @EvFeira     int = (SELECT Id FROM tbEventos WHERE Titulo = N'Feira de Tecnologia');
    DECLARE @EvFotografia int = (SELECT Id FROM tbEventos WHERE Titulo = N'Workshop de Fotografia');
    DECLARE @EvMusica    int = (SELECT Id FROM tbEventos WHERE Titulo = N'Música no Parque');
    DECLARE @EvPalestra  int = (SELECT Id FROM tbEventos WHERE Titulo = N'Palestra: Empreendedorismo Digital');
    DECLARE @EvCafe      int = (SELECT Id FROM tbEventos WHERE Titulo = N'Oficina de Café Especial');
    DECLARE @EvCorrida   int = (SELECT Id FROM tbEventos WHERE Titulo = N'Corrida Solidária 5K');
    DECLARE @EvFestival  int = (SELECT Id FROM tbEventos WHERE Titulo = N'Festival Gastronômico de Inverno');

    DECLARE @Ana    nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = N'ANA@EMAIL.COM');
    DECLARE @Joao   nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = N'JOAO@EMAIL.COM');
    DECLARE @Maria  nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = N'MARIA@EMAIL.COM');
    DECLARE @Carlos nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = N'CARLOS@EMAIL.COM');

    /* ---- Inscrições ----
       Status gravado como texto: Pendente | Confirmada | Cancelada | Concluida
       Valor = preço do evento no momento da inscrição */
    INSERT INTO tbInscricoes (EventoId, UsuarioId, DataInscricao, Status, Valor, Observacao, DataAtualizacao) VALUES
        -- Feira de Tecnologia (gratuita: confirmada na hora)
        (@EvFeira,      @Ana,    DATEADD(DAY, -3, @Agora),  N'Confirmada', 0.00,  NULL, NULL),
        (@EvFeira,      @Joao,   DATEADD(DAY, -2, @Agora),  N'Confirmada', 0.00,  N'Gostaria de participar da trilha de IA.', NULL),
        (@EvFeira,      @Carlos, DATEADD(DAY, -1, @Agora),  N'Confirmada', 0.00,  NULL, NULL),
        -- Workshop de Fotografia (pago)
        (@EvFotografia, @Ana,    DATEADD(DAY, -1, @Agora),  N'Pendente',   89.90, N'Vou levar câmera própria.', NULL),
        (@EvFotografia, @Maria,  DATEADD(DAY, -4, @Agora),  N'Confirmada', 89.90, NULL, DATEADD(DAY, -3, @Agora)),
        -- Música no Parque
        (@EvMusica,     @Joao,   DATEADD(DAY, -2, @Agora),  N'Confirmada', 0.00,  NULL, NULL),
        (@EvMusica,     @Maria,  DATEADD(DAY, -5, @Agora),  N'Cancelada',  0.00,  NULL, DATEADD(DAY, -3, @Agora)),
        -- Palestra (paga, aguardando confirmação)
        (@EvPalestra,   @Maria,  DATEADD(DAY, -1, @Agora),  N'Pendente',   30.00, NULL, NULL),
        (@EvPalestra,   @Carlos, DATEADD(HOUR, -6, @Agora), N'Pendente',   30.00, N'Preciso de nota fiscal.', NULL),
        -- Oficina de Café: 3 vagas e 3 inscrições ativas = LOTADO
        (@EvCafe,       @Ana,    DATEADD(DAY, -6, @Agora),  N'Confirmada', 60.00, NULL, DATEADD(DAY, -5, @Agora)),
        (@EvCafe,       @Joao,   DATEADD(DAY, -5, @Agora),  N'Confirmada', 60.00, NULL, DATEADD(DAY, -4, @Agora)),
        (@EvCafe,       @Maria,  DATEADD(DAY, -5, @Agora),  N'Confirmada', 60.00, N'Intolerância à lactose.', DATEADD(DAY, -4, @Agora)),
        -- Corrida Solidária
        (@EvCorrida,    @Joao,   DATEADD(DAY, -7, @Agora),  N'Cancelada',  45.00, NULL, DATEADD(DAY, -2, @Agora)),
        (@EvCorrida,    @Carlos, DATEADD(DAY, -3, @Agora),  N'Confirmada', 45.00, N'Camiseta tamanho G.', DATEADD(DAY, -2, @Agora)),
        -- Festival Gastronômico (já realizado: participações concluídas)
        (@EvFestival,   @Ana,    DATEADD(DAY, -20, @Agora), N'Concluida',  0.00,  NULL, DATEADD(DAY, -12, @Agora)),
        (@EvFestival,   @Maria,  DATEADD(DAY, -18, @Agora), N'Concluida',  0.00,  NULL, DATEADD(DAY, -12, @Agora)),
        (@EvFestival,   @Carlos, DATEADD(DAY, -15, @Agora), N'Concluida',  0.00,  NULL, DATEADD(DAY, -12, @Agora));

    PRINT N'Categorias, eventos e inscrições de demonstração inseridos.';
END
ELSE
BEGIN
    PRINT N'A tabela tbCategorias já possui dados: categorias, eventos e inscrições não foram inseridos novamente.';
END

COMMIT TRANSACTION;

/* ----------------------------------------------------------------------------
   Conferência
   ---------------------------------------------------------------------------- */
SELECT 'Perfis' AS Tabela, COUNT(*) AS Registros FROM AspNetRoles
UNION ALL SELECT 'Usuários',   COUNT(*) FROM AspNetUsers
UNION ALL SELECT 'Categorias', COUNT(*) FROM tbCategorias
UNION ALL SELECT 'Eventos',    COUNT(*) FROM tbEventos
UNION ALL SELECT 'Inscrições', COUNT(*) FROM tbInscricoes;

SELECT e.Id, e.Titulo, c.Nome AS Categoria, e.DataInicio, e.Vagas,
       SUM(CASE WHEN i.Status <> 'Cancelada' THEN 1 ELSE 0 END) AS InscritosAtivos,
       e.Preco, e.Ativo
FROM tbEventos e
JOIN tbCategorias c ON c.Id = e.CategoriaId
LEFT JOIN tbInscricoes i ON i.EventoId = e.Id
GROUP BY e.Id, e.Titulo, c.Nome, e.DataInicio, e.Vagas, e.Preco, e.Ativo
ORDER BY e.DataInicio;
GO
