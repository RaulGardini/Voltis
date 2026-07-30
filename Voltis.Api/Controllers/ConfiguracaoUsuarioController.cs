using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltis.Api.DTOs;
using Voltis.Domain.Entities;
using Voltis.Infrastructure.Persistence;

namespace Voltis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracaoUsuarioController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConfiguracaoUsuarioController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{usuarioId:guid}")]
    public async Task<IActionResult> ObterPorUsuario(Guid usuarioId)
    {
        var configuracao = await _db.ConfiguracoesUsuario
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (configuracao is null)
            return NotFound(new { mensagem = "Configuração não encontrada para este usuário." });

        return Ok(new ConfiguracaoUsuarioResponse
        {
            DiaFechamentoMes = configuracao.DiaFechamentoMes,
            Moeda = configuracao.Moeda
        });
    }

    [HttpPut("{usuarioId:guid}")]
    public async Task<IActionResult> Atualizar(Guid usuarioId, AtualizarConfiguracaoUsuarioRequest request)
    {
        var moedaNormalizada = request.Moeda.Trim().ToUpperInvariant();

        if (!ConfiguracaoUsuario.MoedasPermitidas.Contains(moedaNormalizada))
            return BadRequest(new
            {
                mensagem = $"Moeda inválida. Valores aceitos: {string.Join(", ", ConfiguracaoUsuario.MoedasPermitidas)}."
            });

        var configuracao = await _db.ConfiguracoesUsuario
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (configuracao is null)
            return NotFound(new { mensagem = "Configuração não encontrada para este usuário." });

        configuracao.Atualizar(request.DiaFechamentoMes, moedaNormalizada);
        await _db.SaveChangesAsync();

        return Ok(new ConfiguracaoUsuarioResponse
        {
            DiaFechamentoMes = configuracao.DiaFechamentoMes,
            Moeda = configuracao.Moeda
        });
    }
}
