using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltis.Domain.Entities;

namespace Voltis.Infrastructure.Persistence.Configurations;

public class ConfiguracaoUsuarioConfiguration : IEntityTypeConfiguration<ConfiguracaoUsuario>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoUsuario> builder)
    {
        // A tabela foi criada com nomes em snake_case, então cada coluna
        // precisa do mapeamento explícito (o padrão do EF seria PascalCase).
        builder.ToTable("configuracao_usuario");

        builder.HasKey(c => c.ConfiguracaoId);

        builder.Property(c => c.ConfiguracaoId)
            .HasColumnName("configuracao_id")
            .UseIdentityByDefaultColumn();

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(c => c.DiaFechamentoMes)
            .HasColumnName("dia_fechamento_mes")
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.Property(c => c.Moeda)
            .HasColumnName("moeda")
            .HasMaxLength(3)
            .HasDefaultValue("BRL")
            .IsRequired();

        // Um-para-um com Usuario: gera o índice único em usuario_id e o
        // ON DELETE CASCADE, igual ao DDL da tabela.
        builder.HasOne<Usuario>()
            .WithOne()
            .HasForeignKey<ConfiguracaoUsuario>(c => c.UsuarioId)
            .HasConstraintName("fk_configuracao_usuario")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
