namespace EconomyBackPortifolio.Enums
{
    /// <summary>
    /// Define os perfis de usuário disponíveis na plataforma Economy Portfolio.
    ///
    /// Cada perfil determina quais funcionalidades e telas estão disponíveis:
    ///   - PessoaFisica: controle financeiro pessoal, lançamentos, gráficos
    ///   - Empresa: controle financeiro empresarial, multi-categoria, relatórios
    ///   - Contador: gestão de múltiplos clientes (multi-tenant), ferramentas contábeis
    /// </summary>
    public enum ProfileType
    {
        /// <summary>Usuário pessoa física — controle financeiro pessoal.</summary>
        PessoaFisica = 0,

        /// <summary>Usuário empresa ou microempresa — controle financeiro empresarial.</summary>
        Empresa = 1,

        /// <summary>Contador — gerencia múltiplos clientes com espaços isolados.</summary>
        Contador = 2
    }
}
