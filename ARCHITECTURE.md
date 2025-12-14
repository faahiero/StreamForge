# StreamForge - Plataforma Distribuída de Processamento de Mídia

## 1. Visão Geral
O **StreamForge** é uma solução backend de alta performance para processamento assíncrono de vídeos. O sistema resolve o problema de latência em uploads desacoplando a ingestão do processamento pesado, utilizando uma arquitetura orientada a eventos.

---

## 2. Status do Projeto (Versão Final 1.0)

| Componente | Status | Detalhes Técnicos |
| :--- | :--- | :--- |
| **Infraestrutura Local** | ✅ Concluído | Docker Compose com LocalStack (S3, SQS, SNS, DynamoDB), Redis e Seq. |
| **Arquitetura** | ✅ Concluído | Clean Architecture, DDD (Rich Domain Models), CQRS (MediatR). |
| **API REST** | ✅ Concluído | Upload via Pre-signed URL, Autenticação JWT, Swagger com Bearer Auth. |
| **Worker Service** | ✅ Concluído | Processamento real com FFmpeg (Docker), Lock Distribuído (Redis), Idempotência. |
| **Mensageria** | ✅ Concluído | Fluxo completo: S3 Event -> SQS -> Worker -> SNS Topic. |
| **Resiliência** | ✅ Concluído | Auto-provisionamento de filas, Dead Letter Queue (DLQ), Health Checks (/health). |
| **Qualidade de Código** | ✅ Concluído | Pipeline de Validação (FluentValidation), Global Exception Handler, Domain Exceptions. |
| **Observabilidade** | ✅ Concluído | Logs Estruturados e Tracing Distribuído (OpenTelemetry) centralizados no **Seq**. |
| **Testes** | ✅ Concluído | Pirâmide completa: Unitários, Integração (API) e E2E (Full Stack). |

---

## 3. Arquitetura de Software

O projeto segue estritamente a **Clean Architecture**:

*   **`StreamForge.Domain`**: O núcleo puro. Entidades (`Video`, `User`), Exceções de Domínio e Interfaces. Sem dependências externas.
*   **`src/StreamForge.Application`**: Casos de uso. Implementa CQRS (Commands/Queries), Validadores e Behaviors.
*   **`src/StreamForge.Infrastructure`**: Implementação técnica. AWS SDK, Repositories (com DTOs e Mapster), Services (Token, Hash, FFmpeg), Redis.
*   **`src/StreamForge.API`**: Entrada HTTP. Configuração de DI, Middlewares de Erro e Controllers.
*   **`src/StreamForge.Worker`**: Consumidor de fundo. Gerencia o ciclo de vida da mensagem SQS.

---

## 4. Stack Tecnológico

### 4.1 Core & Frameworks
*   **.NET 10 (Preview)**: Plataforma base.
*   **ASP.NET Core Web API**: Endpoints RESTful.
*   **Worker Service**: Processamento em background (`IHostedService`).
*   **MediatR**: Implementação do padrão Mediator.
*   **FluentValidation**: Regras de validação de input.
*   **Mapster**: Mapeamento de objetos de alta performance (Domain <-> Infra).

### 4.2 Cloud & Dados (Simulado via LocalStack)
*   **AWS S3**: Armazenamento de vídeos.
*   **AWS SQS**: Fila de ingestão e DLQ.
*   **AWS SNS**: Tópico de notificação de conclusão.
*   **AWS DynamoDB**: Banco de dados NoSQL (Tabelas `Video`, `User`).
*   **Redis**: Cache Distribuído e Lock.

### 4.3 Observabilidade & Ferramentas
*   **Seq**: Centralizador de Logs e Traces (substitui Jaeger/ELK).
*   **OpenTelemetry**: Instrumentação padrão.
*   **Docker Compose**: Orquestração do ambiente de desenvolvimento.
*   **FFmpeg**: Motor de análise de vídeo.

---

## 5. Fluxo de Dados (Workflow)

1.  **Autenticação**: Usuário obtém JWT (`/api/auth/login`).
2.  **Ingestão**: Usuário solicita upload (`/api/videos/init`). API salva metadados `Pending` no DynamoDB e retorna URL S3 assinada.
3.  **Upload**: Usuário envia arquivo binário direto para o S3.
4.  **Evento**: S3 detecta o arquivo e envia evento para a fila SQS `streamforge-ingestion-queue`.
5.  **Processamento (Worker)**:
    *   Lê mensagem SQS.
    *   Adquire **Lock Redis** (evita duplicidade).
    *   Baixa arquivo do S3.
    *   Extrai metadados reais (Duração, Codec) usando **FFmpeg**.
    *   Atualiza DynamoDB (`Completed`).
6.  **Conclusão**: Publica evento no tópico SNS `streamforge-video-events`.

---

## 6. Estrutura de Pastas

```text
/
├── src/
│   ├── StreamForge.Domain/        # Regras de Negócio, Entidades, Interfaces
│   ├── StreamForge.Application/   # Use Cases, CQRS, Validadores
│   ├── StreamForge.Infrastructure/# AWS, Redis, FFmpeg, Repositories, Mappers
│   ├── StreamForge.API/           # Controllers, Middlewares, DI Setup
│   └── StreamForge.Worker/        # SQS Consumer, VideoProcessor
├── tests/
│   ├── StreamForge.Tests/         # Testes Unitários e de Integração
│   └── StreamForge.E2E/           # Testes Ponta a Ponta (C#)
├── deploy/                        # Scripts de Inicialização (init-aws.sh)
├── docker-compose.yml             # Orquestração Local
├── ARCHITECTURE.md                # Esta documentação
└── StreamForge.sln                # Solution File
```

## 7. Backlog Técnico (Futuro)

1.  **Shared Kernel:** Extrair classes comuns (`DomainException`, `Behaviors`) para um pacote NuGet compartilhado.
2.  **API Gateway (YARP):** Implementar gateway para rotear tráfego entre múltiplos serviços.
3.  **CDN:** Integração com CloudFront para entrega de conteúdo.