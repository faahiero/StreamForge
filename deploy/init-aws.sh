#!/bin/bash
echo "🚀 Inicializando recursos AWS no LocalStack..."

awslocal s3 mb s3://streamforge-videos-local
awslocal dynamodb create-table \
    --table-name Video \
    --attribute-definitions AttributeName=Id,AttributeType=S \
    --key-schema AttributeName=Id,KeyType=HASH \
    --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

echo "✅ Recursos criados!"

