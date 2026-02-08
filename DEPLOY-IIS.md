# Deploy automático no IIS com GitHub Actions

## Resumo

- O **web.config** já é gerado automaticamente ao rodar `dotnet publish` (está na pasta `publish`). Não precisa criar manualmente.
- Para o GitHub Actions conseguir fazer deploy no **seu IIS local**, o workflow precisa rodar **na sua máquina** (não na nuvem do GitHub). Isso é feito com um **self-hosted runner**.

---

## Passo 1: Garantir que o IIS está pronto

1. No servidor/máquina onde está o IIS:
   - Instale o **ASP.NET Core Hosting Bundle** (não só o Runtime):  
     https://dotnet.microsoft.com/download/dotnet/8.0  
     → em "Hosting" baixe o "Hosting Bundle" e instale.
   - Crie um **Site** ou **Application** no IIS apontando para uma pasta (ex: `C:\inetpub\wwwroot\EconomyBackPortifolio`).
   - Crie um **Application Pool** para esse site (ex: `EconomyBackPortifolio`) e deixe **Sem código gerenciado** (porque é ASP.NET Core).

---

## Passo 2: Instalar o self-hosted runner do GitHub

1. No GitHub: repositório → **Settings** → **Actions** → **Runners** → **New self-hosted runner**.
2. Escolha **Windows** e siga os comandos que aparecem (baixar, configurar, instalar e rodar o runner **na mesma máquina onde está o IIS**).
3. Depois de configurado, o runner aparece como “Idle” quando estiver livre. Os workflows com `runs-on: self-hosted` vão rodar nessa máquina.

---

## Passo 3: Definir a pasta de deploy (opcional)

No workflow está assim:

- Se existir a variável de ambiente **`IIS_DEPLOY_PATH`** na máquina do runner, ela será usada como pasta de deploy.
- Se não existir, o script usa: `C:\inetpub\wwwroot\EconomyBackPortifolio`.

Para definir em um runner do Windows (como serviço):

1. Abra **Serviços** (services.msc).
2. Encontre o serviço do **GitHub Actions Runner**.
3. Propriedades → **Log On** → variáveis de ambiente (ou defina no sistema em **Variáveis de ambiente** do usuário/sistema que roda o serviço):
   - `IIS_DEPLOY_PATH` = pasta física do site no IIS (ex: `C:\inetpub\wwwroot\EconomyBackPortifolio`).
   - `IIS_APP_POOL` = nome do Application Pool (ex: `EconomyBackPortifolio`) se quiser reinício automático do pool após o deploy.

---

## Passo 4: Rodar o deploy

- Dê **push** na branch **main** (ou na branch que você configurou no workflow).
- Em **Actions** no GitHub, o workflow “Build and Deploy to IIS” será disparado e rodará no seu runner.
- O job faz: checkout → `dotnet publish -c Release -o publish` → cópia dos arquivos da pasta `publish` (incluindo o **web.config**) para a pasta do IIS → opcionalmente reinicia o App Pool.

---

## web.config

Não é necessário criar `web.config` na raiz do projeto. O `dotnet publish` gera um `web.config` na pasta `publish` adequado para ASP.NET Core no IIS (AspNetCoreModuleV2, inprocess). Esse arquivo é o que será copiado para a pasta do site no deploy.

Se precisar personalizar (por exemplo, variáveis de ambiente ou caminho de logs), você pode adicionar um `web.config` na raiz do projeto e configurá-lo para ser copiado na publicação (Content / CopyToPublishDirectory).

---

## Resumo dos comandos que você usou

```bash
dotnet publish -c Release -o publish
```

Isso gera a pasta `publish` com a aplicação e o **web.config** prontos para o IIS. O workflow do GitHub Actions repete esse `publish` no runner e copia o resultado para a pasta do IIS.
