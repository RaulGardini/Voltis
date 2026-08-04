using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltis.Api.DTOs;
using Voltis.Api.Extensions;
using Voltis.Domain.Entities;
using Voltis.Infrastructure.Persistence;

namespace Voltis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Redundante com a FallbackPolicy de Program.cs, e de propósito: deixa
// explícito no arquivo que estas rotas exigem token, sem precisar ir ler
// a configuração global para descobrir.
[Authorize]
public class ConfiguracaoUsuarioController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConfiguracaoUsuarioController(AppDbContext db)
    {
        _db = db;
    }

    // Sem {usuarioId} na rota: o usuário é sempre o dono do token. Enquanto o
    // id vinha da URL, qualquer um autenticado lia e editava a configuração
    // de qualquer outro só trocando o Guid.
    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        var usuarioId = User.ObterUsuarioId();

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

    [HttpPut]
    public async Task<IActionResult> Atualizar(AtualizarConfiguracaoUsuarioRequest request)
    {
        var usuarioId = User.ObterUsuarioId();

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
