# Back_EconomyPortifolio

Backend API para gerenciamento de portfólio de investimentos.

## Tecnologias

- .NET 10.0
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
3. Configure o seu SMTP para o envio de codigo 2FA.
4. Execute o projeto

## Banco de Dados

O projeto utiliza PostgreSQL. Certifique-se de ter o banco configurado antes de executar a aplicação.
O PostGreSQL atualmente é utilizado localmente, em futuras versões poderá vir a ser hospedado.


## IIS
O projeto esta sendo implementado dentro de um IIS como parte dos estudos, em possiveis casos de erro,
consulte o projeto pra trocar a abordagem do projeto.
