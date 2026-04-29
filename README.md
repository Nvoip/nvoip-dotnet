# nvoip-dotnet

SDK e exemplos oficiais da [Nvoip](https://www.nvoip.com.br/) para integrar a API v2 com OAuth, chamadas, OTP, WhatsApp, SMS e saldo em .NET.

## Requisitos

- .NET 8 SDK

## Configuração

```bash
cp .env.example .env
```

Ou exporte:

```bash
export NVOIP_NUMBERSIP="seu_numbersip"
export NVOIP_USER_TOKEN="seu_user_token"
export NVOIP_OAUTH_CLIENT_ID="seu_client_id"
export NVOIP_OAUTH_CLIENT_SECRET="seu_client_secret"
export NVOIP_CALLER="1049"
export NVOIP_TARGET_NUMBER="11999999999"
```

## Build

```bash
dotnet build
```

## Exemplos

```bash
dotnet run --project examples/Nvoip.Examples -- auth-token
dotnet run --project examples/Nvoip.Examples -- balance
dotnet run --project examples/Nvoip.Examples -- send-sms
dotnet run --project examples/Nvoip.Examples -- create-call
dotnet run --project examples/Nvoip.Examples -- send-otp
dotnet run --project examples/Nvoip.Examples -- check-otp
dotnet run --project examples/Nvoip.Examples -- wa-list
dotnet run --project examples/Nvoip.Examples -- wa-send
```

## SDK web

Para o fluxo de popup com telefone e código, use em conjunto o repositório `nvoip-web-sdk`. Este repo cobre o consumo server-side da API.

## Documentação oficial

- https://nvoip.docs.apiary.io/
- https://www.nvoip.com.br/api
