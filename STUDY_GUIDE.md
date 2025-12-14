# Guia de Estudo e Referência Técnica: StreamForge (Edição Completa)

Este documento é um **manual técnico exaustivo** do projeto StreamForge. Ele detalha cada decisão arquitetural, padrão de projeto, tecnologia e linha de código estratégica. Projetado para ser a fonte definitiva de conhecimento para desenvolvedores que desejam dominar arquitetura de microsserviços em .NET.

---

## 1. Fundamentos e Tecnologias (O "O Que é O Que")

Antes de mergulhar no código, vamos definir as peças do nosso tabuleiro.

### 1.1 O Ecossistema AWS (Simulado pelo LocalStack)
O StreamForge é "Cloud Native". Usamos o **LocalStack** para simular a AWS na sua máquina local via Docker.

*   **S3 (Simple Storage Service):** Armazenamento de objetos (arquivos de mídia).
    *   *No StreamForge:* Bucket `streamforge-videos-local`.
    *   *Padrão:* Pre-signed URLs para upload seguro sem sobrecarregar a API.
*   **SQS (Simple Queue Service):** Fila de mensagens.
    *   *No StreamForge:* Fila `streamforge-ingestion-queue`. Atua como buffer assíncrono.
    *   *DLQ (Dead Letter Queue):* `streamforge-ingestion-queue-dlq`. Mensagens que falham 3x vão para cá.
*   **SNS (Simple Notification Service):** Pub/Sub.
    *   *No StreamForge:* Tópico `streamforge-video-events`. O Worker publica aqui quando termina.
*   **DynamoDB:** Banco NoSQL chave-valor.
    *   *Vantagem:* Schema-less, alta performance de leitura por chave.

### 1.2 Componentes .NET
*   **Worker Service (`IHostedService`):** Aplicação console que roda em background (Daemon).
*   **MediatR:** Implementação do padrão Mediator. Desacopla quem pede (Controller) de quem faz (Handler).
*   **FluentValidation:** Biblioteca para regras de validação fluentes e separadas da lógica de negócio.

---

## 2. Anatomia da Arquitetura (Clean Architecture)

A dependência flui de fora para dentro. O núcleo (Domain) não conhece ninguém.

### 🟢 Camada 1: Domain (`src/StreamForge.Domain`) - O Centro
*   **Entities (`Video.cs`, `User.cs`):**
    *   **Rich Domain Model:** As entidades protegem suas invariantes.
    *   Exemplo: `Video.MarkAsProcessing()` lança erro se o vídeo já estiver concluído.
    *   *Setters Privados:* Forçam o uso de métodos de negócio para alterar estado.
*   **Enums (`ProcessingStatus`):** Pending (0), Processing (1), Completed (2), Failed (3).
*   **Interfaces (`IVideoRepository`):** Definem contratos. O Domínio define *o que* precisa, a Infra define *como* fazer.

### 🟡 Camada 2: Application (`src/StreamForge.Application`) - A Orquestração
*   **Features (CQRS):** Cada caso de uso é uma pasta (`Features/Videos/Commands/...`).
    *   `InitiateUploadCommand`: DTO de entrada.
    *   `InitiateUploadHandler`: Lógica de orquestração.
*   **Behaviors (`ValidationBehavior`):** Middleware do MediatR. Intercepta requisições e valida automaticamente.
*   **Interfaces de Serviço:** `IStorageService`, `IMessagePublisher`, `IAuthServices`.

### 🔴 Camada 3: Infrastructure (`src/StreamForge.Infrastructure`) - O Músculo
*   **Persistência (DynamoDB):**
    *   **Mapster:** Mapeia `Video` (Domain) <-> `VideoDocument` (Infra). Isso isola os atributos `[DynamoDBTable]` da entidade pura.
*   **Services Concretos:**
    *   `S3StorageService`: Gera URLs assinadas.
    *   `RedisLockService`: Implementa lock distribuído (`SET NX`) para evitar concorrência.
    *   `FfmpegMediaAnalyzer`: Wrapper que chama o processo `ffprobe` no SO.

### 🔵 Camada 4: API (`src/StreamForge.API`) - A Vitrine
*   **Controllers:** Minimalistas. Apenas recebem HTTP e chamam MediatR.
*   **Global Exception Handler:** Middleware que converte Exceptions em JSON `ProblemDetails`.
*   **Health Checks:** Monitora conectividade com S3 e DynamoDB.

### ⚫ Camada 5: Worker (`src/StreamForge.Worker`) - O Bastidor
*   **Worker.cs:** Loop infinito de leitura de fila.
*   **Auto-Provisionamento:** `QueueInitializer` cria a fila e DLQ se não existirem (ótimo para dev).
*   **VideoProcessor:** Serviço que baixa o vídeo, roda o FFmpeg e atualiza o banco.

---

## 3. Schema de Dados (DynamoDB)

### Tabela: `Video`
*   **PK (Partition Key):** `Id` (String/Guid).
*   **Atributos:**
    *   `FileName` (String)
    *   `Status` (String - convertido de Enum)
    *   `Duration` (Number - Ticks)
    *   `S3Key` (String)

### Tabela: `User`
*   **PK (Partition Key):** `Email` (String).
*   **Atributos:**
    *   `PasswordHash` (String - Base64 SHA256)
    *   `Id` (String/Guid)

---

## 4. Segurança e Autenticação

### Fluxo JWT (JSON Web Token)
1.  **Registro (`/register`):** Recebe senha -> Hash (SHA256) -> Salva no DynamoDB.
2.  **Login (`/login`):** Recebe senha -> Hash -> Compara com banco -> Gera JWT assinado (HMACSHA256).
3.  **Proteção:** O endpoint `/init` tem o atributo `[Authorize]`. O middleware do ASP.NET valida a assinatura do token antes de permitir o acesso.

---

## 5. Fluxos Detalhados e "Porquês"

### 5.1 O Problema da Concorrência (Race Condition)
**Cenário:** O SQS garante "pelo menos uma entrega". Dois Workers podem receber a mesma mensagem.
**Solução (Redis Lock):**
1.  Worker A tenta: `SET video:123 "tokenA" NX EX 300` (NX = Se não existe).
2.  Redis retorna `OK` (True). Worker A processa.
3.  Worker B tenta a mesma chave.
4.  Redis retorna `Nil` (False). Worker B aborta e não duplica o trabalho.

### 5.2 Build Docker Otimizado (Multi-Stage)
O `Dockerfile` usa múltiplos estágios para gerar uma imagem minúscula.
1.  **Estágio `build`:** Usa imagem SDK (grande, com compiladores). Compila o código.
2.  **Estágio `publish`:** Gera os binários otimizados.
3.  **Estágio `final`:** Usa imagem Runtime (leve).
    *   Copia apenas os binários do estágio anterior.
    *   Instala dependências de runtime (FFmpeg).
    *   Resultado: Imagem final pequena e segura (sem código fonte).

### 5.3 FFmpeg no Docker
O FFmpeg não vem instalado nas imagens .NET.
*   **Estratégia:** Baixamos o "Static Build" do FFmpeg via `wget` no Dockerfile.
*   **Por que estático?** Funciona em qualquer distribuição Linux (Debian, Alpine, etc) sem depender de bibliotecas de sistema (`glibc` versions) que mudam entre versões do OS.

---

## 6. Guia de Troubleshooting

### "Fila não existe" (QueueDoesNotExist)
*   **Causa:** LocalStack reiniciou e perdeu estado, ou o script de init não rodou.
*   **Cura:** O `QueueInitializer` no Worker recria automaticamente em Dev.

### "Connection Refused" nos testes
*   **Causa:** O teste (rodando no Host) tenta acessar `localhost:5200` mas o container não está mapeando a porta, ou o LocalStack não está acessível.
*   **Cura:** Verificar `docker-compose ps`. Garantir que `127.0.0.1` está mapeado.

### "FFprobe failed with code 1"
*   **Causa:** O arquivo baixado do S3 está corrompido ou vazio (0 bytes).
*   **Cura:** Verificar se o upload inicial funcionou. Verificar se a URL assinada estava correta (Region/Bucket).

---

## 7. Como estudar este projeto?

1.  **Comece pelo Domínio:** Leia `Video.cs`. Entenda as regras.
2.  **Siga o Handler:** Leia `InitiateUploadHandler.cs`. Veja como ele usa `IVideoRepository` e `IStorageService`.
3.  **Infra:** Veja como `VideoRepository.cs` usa `Mapster` para converter `Video` em `VideoDocument`.
4.  **Worker:** Estude `Worker.cs`. Veja o tratamento de `OperationCanceledException` para shutdown limpo.
5.  **Testes:** Rode `test_docker_e2e.sh` e veja os logs fluindo.

Este projeto é um mapa do tesouro para arquitetura de software. Explore cada pasta!