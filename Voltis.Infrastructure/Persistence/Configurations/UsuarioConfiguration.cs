using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltis.Domain.Entities;

namespace Voltis.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        // Índice único: o banco garante que não existem dois emails iguais.
        // Essa é a defesa REAL contra email duplicado. Checar no código não
        // basta — duas requisições simultâneas passariam pela checagem antes
        // de qualquer uma salvar. O banco é o único que resolve isso de fato.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.SenhaHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.CriadoEm)
            .IsRequired();
    }
}