# Back_EconomyPortifolio

Backend API para gerenciamento de portfólio de investimentos.

## Tecnologias

- .NET 8.0
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Web API

## Estrutura do Projeto

- **Models**: Entidades do banco de dados (Users, Assets, Wallets, Positions, Transactions)
- **Data**: DbContext e configurações do Entity Framework
- **Controllers**: Endpoints da API

## Configuração

1. Clone o repositório
2. Configure a connection string no `appsettings.Development.json` (use `appsettings.example.json` como referência)
3. Execute o projeto

## Banco de Dados

O projeto utiliza PostgreSQL. Certifique-se de ter o banco configurado antes de executar a aplicação.
