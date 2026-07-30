using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voltis.Api.DTOs;
using Voltis.Domain.Entities;
using Voltis.Domain.Services;
using Voltis.Infrastructure.Persistence;

namespace Voltis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public AuthController(
        AppDbContext db,
        IPasswordHasher hasher,
        ITokenService tokenService)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar(RegistrarRequest request)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        // Checagem no código: dá uma mensagem amigável. Mas a garantia
        // real contra duplicata é o índice único do banco (passo 3a).
        var jaExiste = await _db.Usuarios
            .AnyAsync(u => u.Email == emailNormalizado);

        if (jaExiste)
            return Conflict(new { mensagem = "Email já cadastrado." });

        var senhaHash = _hasher.Hash(request.Senha);
        var usuario = new Usuario(request.Nome.Trim(), emailNormalizado, senhaHash);

        _db.Usuarios.Add(usuario);

        // A configuração nasce junto com o usuário (dia 1, moeda BRL).
        // Como as duas entidades entram no mesmo SaveChanges, o EF salva
        // tudo em uma única transação: ou grava os dois, ou nenhum.
        _db.ConfiguracoesUsuario.Add(new ConfiguracaoUsuario(usuario.Id));

        await _db.SaveChangesAsync();

        var token = _tokenService.GerarToken(usuario);

        return Ok(new AuthResponse
        {
            Token = token,
            Nome = usuario.Nome,
            Email = usuario.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado);

        // Ponto de segurança: a MESMA resposta para "email não existe" e
        // "senha errada". Se as mensagens fossem diferentes, um atacante
        // descobriria quais emails estão cadastrados.
        if (usuario is null || !_hasher.Verificar(request.Senha, usuario.SenhaHash))
            return Unauthorized(new { mensagem = "Credenciais inválidas." });

        var token = _tokenService.GerarToken(usuario);

        return Ok(new AuthResponse
        {
            Token = token,
            Nome = usuario.Nome,
            Email = usuario.Email
        });
    }
}