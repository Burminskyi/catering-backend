using CateringSaaS.Modules.Knowledge.Services;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CateringSaaS.Modules.Knowledge.Endpoints;

public static class UploadKnowledgeDocumentEndpoint
{
    private const long MaxFileBytes = 20 * 1024 * 1024;

    public static RouteHandlerBuilder MapUploadKnowledgeDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/documents/upload", HandleAsync)
            .WithName("UploadKnowledgeDocument")
            .WithTags("Knowledge")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data");
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        IKnowledgeBaseService knowledgeBase,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (tenantContext.WorkspaceId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Workspace context is required." });
        }

        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "PDF or DOCX file is required." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "PDF or DOCX file is required." });
        }

        if (file.Length > MaxFileBytes)
        {
            return Results.BadRequest(new { message = "File must be 20 MB or smaller." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Only PDF and DOCX files are supported." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await knowledgeBase.IngestDocumentAsync(
                stream,
                file.FileName,
                tenantContext.WorkspaceId,
                currentUser.UserId == Guid.Empty ? null : currentUser.UserId,
                file.ContentType,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (NotSupportedException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
