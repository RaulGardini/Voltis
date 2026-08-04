using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Voltis.Api.Erros;

/// <summary>
/// Última linha de defesa: captura qualquer exceção que escape de um
/// controller e responde em ProblemDetails (RFC 7807).
///
/// Sem isto, uma exceção não tratada devolve a página de erro do ASP.NET —
/// que em produção é opaca, mas em qualquer ambiente com o DeveloperExceptionPage
/// ligado entrega stack trace, nomes de arquivo e caminho do projeto ao cliente.
/// </summary>
public class TratadorGlobalDeExcecoes : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<TratadorGlobalDeExcecoes> _logger;

    public TratadorGlobalDeExcecoes(
        IProblemDetailsService problemDetailsService,
        ILogger<TratadorGlobalDeExcecoes> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception excecao,
        CancellationToken cancellationToken)
    {
        var (status, titulo, detalhe) = Traduzir(excecao);

        // O diagnóstico completo fica no log do servidor. O cliente recebe
        // só o que precisa para corrigir a própria requisição.
        _logger.LogError(
            excecao,
            "Falha ao processar {Metodo} {Caminho}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = detalhe
            }
        });
    }

    private static (int Status, string Titulo, string Detalhe) Traduzir(Exception excecao) =>
        excecao switch
        {
            // ArgumentNullException herda de ArgumentException, mas significa
            // "esqueci de passar um valor" — bug nosso, não erro do cliente.
            // Por isso vem ANTES do caso geral, senão cairia como 400.
            ArgumentNullException => Interno(),

            // Regras do domínio (ex: ConfiguracaoUsuario.Atualizar) sinalizam
            // entrada inválida com ArgumentException. Isso é 400, não 500, e a
            // mensagem da própria regra é segura para devolver.
            ArgumentException e => (
                StatusCodes.Status400BadRequest,
                "Requisição inválida.",
                e.Message),

            _ => Interno(),
        };

    /// Resposta genérica: nunca vaza mensagem interna, que pode revelar
    /// estrutura do sistema (tabelas, caminhos, versões de biblioteca).
    private static (int, string, string) Interno() => (
        StatusCodes.Status500InternalServerError,
        "Erro interno.",
        "Erro inesperado ao processar a requisição.");
}
