using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltis.Domain.Entities;

namespace Voltis.Infrastructure.Persistence.Configurations;

public class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        // Mapeamento espelha a tabela que já existe no banco, criada à mão.
        // Colunas em snake_case exigem nome explícito (o padrão do EF é PascalCase).
        builder.ToTable("conta");

        builder.HasKey(c => c.ContaId);

        builder.Property(c => c.ContaId)
            .HasColumnName("conta_id")
            .UseIdentityByDefaultColumn();

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(c => c.Nome)
            .HasColumnName("nome")
            .HasMaxLength(Conta.NomeTamanhoMaximo)
            .IsRequired();

        // A coluna no banco é `timestamp without time zone`. Sem declarar o tipo,
        // o EF assumiria `timestamptz` (padrão do Npgsql para DateTime) e o
        // modelo divergiria do banco. Pior: o Npgsql recusa gravar um DateTime
        // com Kind=Utc numa coluna sem fuso, e o erro só apareceria em runtime.
        //
        // ValueGeneratedOnAdd: quem preenche é o DEFAULT do banco. O EF omite a
        // coluna no INSERT e lê o valor de volta.
        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .HasConstraintName("fk_conta_usuario")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
