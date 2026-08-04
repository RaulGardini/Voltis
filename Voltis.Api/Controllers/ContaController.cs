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
[Authorize]
public class ContaController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContaController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Contas do usuário logado, da mais antiga para a mais nova — a ordem que
    /// o front usa para escolher qual conta abrir por padrão.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = User.ObterUsuarioId();

        var contas = await _db.Contas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            // ContaId como desempate: duas contas criadas no mesmo instante
            // teriam o mesmo criado_em, e a ordem ficaria indefinida.
            .OrderBy(c => c.CriadoEm)
            .ThenBy(c => c.ContaId)
            .Select(c => new ContaResponse
            {
                ContaId = c.ContaId,
                Nome = c.Nome,
                CriadoEm = c.CriadoEm
            })
            .ToListAsync();

        return Ok(contas);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(CriarContaRequest request)
    {
        var usuarioId = User.ObterUsuarioId();

        // Nome vazio/só espaços vira ArgumentException no domínio, que o
        // TratadorGlobalDeExcecoes converte em 400.
        var conta = new Conta(usuarioId, request.Nome);

        _db.Contas.Add(conta);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), Mapear(conta));
    }

    [HttpPut("{contaId:long}")]
    public async Task<IActionResult> Atualizar(long contaId, AtualizarContaRequest request)
    {
        var usuarioId = User.ObterUsuarioId();

        // O filtro por UsuarioId é a checagem de posse: conta de outro usuário
        // simplesmente não é encontrada. Responder 404 (e não 403) evita
        // confirmar que aquele id existe.
        var conta = await _db.Contas
            .FirstOrDefaultAsync(c => c.ContaId == contaId && c.UsuarioId == usuarioId);

        if (conta is null)
            return NotFound(new { mensagem = "Conta não encontrada." });

        conta.Renomear(request.Nome);
        await _db.SaveChangesAsync();

        return Ok(Mapear(conta));
    }

    private static ContaResponse Mapear(Conta conta) => new()
    {
        ContaId = conta.ContaId,
        Nome = conta.Nome,
        CriadoEm = conta.CriadoEm
    };
}
