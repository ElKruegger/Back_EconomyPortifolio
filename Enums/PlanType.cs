namespace EconomyBackPortifolio.Enums
{
    /// <summary>
    /// Define os planos de assinatura disponíveis na plataforma Economy Portfolio.
    ///
    /// Plano Basic:
    ///   - Lançamentos de receitas e despesas
    ///   - Categorias padrão do sistema
    ///   - Gráficos mensais
    ///   - Relatórios básicos por e-mail
    ///
    /// Plano Pro:
    ///   - Todas as funcionalidades do Basic
    ///   - Categorias personalizadas ilimitadas
    ///   - Gestão de carteiras e ativos (investimentos)
    ///   - Relatórios avançados e projeções
    ///   - Relatórios automáticos via WhatsApp
    ///   - Upload de planilhas (Excel/CSV)
    ///   - Assistente de IA financeiro
    ///   - Para Contadores: gestão de clientes sem limite
    /// </summary>
    public enum PlanType
    {
        /// <summary>Plano gratuito com funcionalidades essenciais.</summary>
        Basic = 0,

        /// <summary>Plano pago com acesso completo à plataforma.</summary>
        Pro = 1
    }
}
