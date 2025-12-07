#!/bin/bash
echo "🚀 Inicializando recursos AWS no LocalStack..."

# 1. Criar Bucket S3
awslocal s3 mb s3://streamforge-videos-local

# 2. Criar Tabela DynamoDB
awslocal dynamodb create-table \
    --table-name Video \
    --attribute-definitions AttributeName=Id,AttributeType=S \
    --key-schema AttributeName=Id,KeyType=HASH \
    --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

awslocal dynamodb create-table \
    --table-name User \
    --attribute-definitions AttributeName=Email,AttributeType=S \
    --key-schema AttributeName=Email,KeyType=HASH \
    --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

# 3. Criar Fila SQS + DLQ
awslocal sqs create-queue --queue-name streamforge-ingestion-queue-dlq
awslocal sqs create-queue --queue-name streamforge-ingestion-queue \
    --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:us-east-1:000000000000:streamforge-ingestion-queue-dlq\",\"maxReceiveCount\":3}"}'

# 4. Criar Tópico SNS (Novo)
awslocal sns create-topic --name streamforge-video-events

# 5. Configurar Notificação S3 -> SQS
awslocal s3api put-bucket-notification-configuration \
    --bucket streamforge-videos-local \
    --notification-configuration '{
        "QueueConfigurations": [
            {
                "QueueArn": "arn:aws:sqs:us-east-1:000000000000:streamforge-ingestion-queue",
                "Events": ["s3:ObjectCreated:*"]
            }
        ]
    }'

echo "✅ Recursos criados!"

