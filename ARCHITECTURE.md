# StreamForge - Plataforma Distribuída de Processamento de Mídia

## 1. Visão Geral do Projeto
O **StreamForge** é uma solução backend de alta performance projetada para demonstrar o processamento assíncrono e distribuído de arquivos grandes em ambiente Cloud. O sistema resolve o problema de latência em uploads e processamentos pesados, desacoplando a recepção do arquivo de sua manipulação.

O projeto simula um ambiente **AWS Enterprise** completo utilizando **LocalStack**, focando em resiliência, observabilidade e escalabilidade horizontal.

---

## 2. Arquitetura de Software

### 2.1 Estilo Arquitetural
Adotaremos a **Clean Architecture** (Arquitetura Limpa), garantindo que o núcleo do domínio seja independente de frameworks, UI ou banco de dados.

### 2.2 Estrutura da Solução (Projetos)
A solução será dividida em camadas concêntricas:

*   **`src/StreamForge.Domain`**: O núcleo. Contém Entidades, Value Objects, Interfaces de Repositório e Exceções de Domínio. **Zero dependências externas.**
*   **`src/StreamForge.Application`**: Camada de Orquestração. Implementa CQRS (via MediatR), DTOs, Validadores (FluentValidation) e Interfaces de Serviços de Infraestrutura (ex: `IFileStorage`, `IMessagePublisher`).
*   **`src/StreamForge.Infrastructure`**: Implementação técnica. Aqui residem os clientes AWS (SDKs), acesso a dados (DynamoDB), configurações do Redis e implementações concretas das interfaces da Application.
*   **`src/StreamForge.API`**: Entrypoint HTTP. Responsável apenas por receber requisições, mapear para Commands/Queries e retornar respostas.
*   **`src/StreamForge.Worker`**: Microsserviço (Background Service) autônomo responsável por consumir filas e executar o processamento pesado.

---

## 3. Stack Tecnológico

### 3.1 Core & Frameworks
*   **.NET 8**: Plataforma base.
*   **ASP.NET Core Web API**: Para endpoints RESTful.
*   **Worker Service**: Para processamento em background.
*   **MediatR**: Implementação do padrão Mediator para CQRS.
*   **FluentValidation**: Regras de validação robustas.
*   **Polly**: Resiliência (Retries, Circuit Breaker) nas chamadas HTTP e AWS.

### 3.2 Cloud & Infraestrutura (Simulado via LocalStack)
*   **AWS S3**: Armazenamento de objetos (arquivos de mídia).
*   **AWS SQS (Simple Queue Service)**: Desacoplamento via filas de mensagens.
*   **AWS SNS (Simple Notification Service)**: Fan-out de eventos de conclusão.
*   **AWS DynamoDB**: Banco de dados NoSQL para armazenar metadados e estado do processamento (alta performance de escrita/leitura).

### 3.3 Suporte & Observabilidade
*   **Redis**:
    *   **Caching**: Cache-aside para status de processamento.
    *   **Distributed Lock**: Prevenção de processamento duplicado (Race Conditions).
*   **OpenTelemetry**: Instrumentação padrão da indústria para rastreamento.
*   **Jaeger**: Visualização de Traces Distribuídos (Acompanhar a request da API até o Worker).
*   **Docker & Docker Compose**: Orquestração de todo o ambiente local.

---

## 4. Fluxo de Dados (Workflow)

Este fluxo "Event-Driven" é o coração do sistema:

1.  **Solicitação de Upload (API)**:
    *   Cliente solicita intenção de upload via endpoint `POST /api/videos/init`.
    *   API gera uma **AWS S3 Pre-Signed URL** (segura e temporária).
    *   API registra o "início do processo" no DynamoDB com status `PENDING`.

2.  **Upload Direto (Client -> S3)**:
    *   Cliente faz PUT do binário direto para o S3 (bypass na API para evitar gargalo).

3.  **Evento de Ingestão (S3 -> SQS)**:
    *   O bucket S3 está configurado para disparar um evento `s3:ObjectCreated:*` automaticamente para uma fila SQS.

4.  **Processamento (Worker)**:
    *   O Worker escuta a fila SQS.
    *   **Idempotência & Locking**: O Worker tenta adquirir um Lock no Redis para o `FileKey`. Se falhar, ignora a mensagem.
    *   **Download**: Baixa o arquivo do S3 (stream).
    *   **Processamento**: Simula extração de metadados (ex: duração, codec) ou transcodificação.
    *   **Persistência**: Atualiza o registro no DynamoDB com os metadados e muda status para `COMPLETED`.

5.  **Notificação (Worker -> SNS)**:
    *   Worker publica evento `VideoProcessedEvent` no SNS.
    *   Assinantes (Email Service, Push Notification, outros microsserviços) recebem o aviso.

---

## 5. Padrões de Projeto (Design Patterns)

*   **CQRS (Command Query Responsibility Segregation)**: Separação física e lógica entre operações de Leitura (Queries) e Escrita (Commands).
*   **Repository Pattern**: Abstração da camada de dados (mesmo sendo NoSQL/DynamoDB).
*   **Strategy Pattern**: Para suportar diferentes estratégias de processamento baseadas no MIME type do arquivo (VideoStrategy, ImageStrategy).
*   **Distributed Locking**: Garantia de consistência em ambiente concorrente.
*   **Correlation ID**: Um ID único que trafega nos headers das mensagens para rastrear o log do início ao fim via OpenTelemetry.

---

## 6. Estrutura de Pastas Proposta

```text
/
├── src/
│   ├── StreamForge.Domain/        # Regras de Negócio Puras
│   ├── StreamForge.Application/   # Casos de Uso (CQRS)
│   ├── StreamForge.Infrastructure/# Implementação AWS/Redis
│   ├── StreamForge.API/           # REST API
│   └── StreamForge.Worker/        # Consumer SQS
├── pipelines/                     # Definições YAML do Azure Pipelines
├── deploy/                        # Manifestos Bicep/Terraform para Azure
├── tests/                         # Testes Unitários e de Integração
├── docker-compose.yml             # Definição da Infraestrutura Local
├── ARCHITECTURE.md                # Este arquivo
└── StreamForge.sln                # Solution File
```

## 7. DevOps e Ciclo de Vida (Azure DevOps)

Toda a automação será executada via **Azure Pipelines**, centralizando o desenvolvimento no ecossistema Microsoft.

### 7.1 Plataforma de Deploy: Azure Container Apps (ACA)
Utilizaremos o ACA para orquestrar nossos microsserviços. O ambiente de produção manterá a arquitetura de "Simulação Hospedada" (LocalStack rodando como container no Azure).
*   **Container App Environment**: Rede isolada.
*   **Apps**: API, Worker, LocalStack, Redis.

### 7.2 Pipelines (Azure Pipelines)

#### A. Build & Validation (CI)
Arquivo: `pipelines/azure-pipelines-ci.yml`
Gatilho: Push em branches de feature ou Pull Requests no Azure Repos.
1.  **DotNetBuild**: Restaura e compila a solution (.NET 8).
2.  **UnitTests**: Executa `dotnet test` e publica os resultados de cobertura de código no painel do Azure DevOps.
3.  **DockerLint**: Verifica boas práticas nos Dockerfiles.

#### B. Release & Deploy (CD)
Arquivo: `pipelines/azure-pipelines-cd.yml`
Gatilho: Merge na branch `main`.
1.  **Docker Build & Push**:
    *   Gera as imagens da API e do Worker.
    *   Envia para o **Azure Container Registry (ACR)**.
2.  **Infrastructure as Code (Bicep)**:
    *   Valida e aplica templates Bicep para criar/atualizar o Azure Container Apps.
3.  **Deploy Revision**:
    *   Atualiza os microsserviços para usar a nova tag da imagem gerada.
