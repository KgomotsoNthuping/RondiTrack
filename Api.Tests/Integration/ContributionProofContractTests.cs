using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Api.DTO;
using Api.Services;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class ContributionProofContractTests
{
    private readonly PostgresApiFactory _factory;

    private static readonly byte[] PdfBytes =
        Encoding.ASCII.GetBytes(
            "%PDF-1.7\n1 0 obj\n<<>>\nendobj\n%%EOF");

    public ContributionProofContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Owner_can_upload_and_download_private_proof()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var ownerId = data.UserIds.Last();
        var contributionId = await FindContributionIdAsync(
            data.StokvelId,
            ownerId);

        using var owner =
            _factory.CreateAuthenticatedClient(ownerId);

        using var uploaded = await UploadAsync(
            owner,
            data.StokvelId,
            contributionId,
            PdfBytes);

        Assert.Equal(
            HttpStatusCode.OK,
            uploaded.StatusCode);

        var metadata = await uploaded.Content
            .ReadFromJsonAsync<ContributionProofResponse>();

        Assert.NotNull(metadata);
        Assert.Equal(
            "application/pdf",
            metadata.ContentType);
        Assert.Equal(
            PdfBytes.Length,
            metadata.SizeBytes);
        Assert.Equal(
            ownerId,
            metadata.UploadedByUserId);

        var url = ProofUrl(
            data.StokvelId,
            contributionId);

        using var downloaded =
            await owner.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.OK,
            downloaded.StatusCode);

        Assert.Equal(
            "application/pdf",
            downloaded.Content.Headers.ContentType?.MediaType);

        Assert.Equal(
            "attachment",
            downloaded.Content.Headers.ContentDisposition?.DispositionType);

        Assert.Contains(
            "no-store",
            downloaded.Headers.CacheControl?.ToString());

        Assert.Equal(
            "nosniff",
            downloaded.Headers.GetValues(
                "X-Content-Type-Options").Single());

        var actualBytes = await downloaded.Content
            .ReadAsByteArrayAsync();

        Assert.Equal(PdfBytes, actualBytes);

        var fetchedMetadata =
            await owner.GetFromJsonAsync<ContributionProofResponse>(
                url + "/metadata");

        Assert.NotNull(fetchedMetadata);
        Assert.Equal(metadata.Id, fetchedMetadata.Id);
    }

    [Fact]
    public async Task Only_owner_and_own_stokvel_treasurer_may_download()
    {
        var first = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 3);

        var second = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var ownerId = first.UserIds.Skip(1).First();
        var otherMemberId = first.UserIds.Last();

        var contributionId = await FindContributionIdAsync(
            first.StokvelId,
            ownerId);

        var ownTreasurerId = await AddTreasurerAsync(
            first.StokvelId);

        var otherTreasurerId = await AddTreasurerAsync(
            second.StokvelId);

        using var owner =
            _factory.CreateAuthenticatedClient(ownerId);

        using var ownTreasurer =
            _factory.CreateAuthenticatedClient(
                ownTreasurerId);

        using var ordinaryMember =
            _factory.CreateAuthenticatedClient(
                otherMemberId);

        using var otherTreasurer =
            _factory.CreateAuthenticatedClient(
                otherTreasurerId);

        using var upload = await UploadAsync(
            owner,
            first.StokvelId,
            contributionId,
            PdfBytes);

        Assert.Equal(
            HttpStatusCode.OK,
            upload.StatusCode);

        var url = ProofUrl(
            first.StokvelId,
            contributionId);

        using var ownResult = await ownTreasurer.GetAsync(
            url);

        using var ordinaryResult =
            await ordinaryMember.GetAsync(url);

        using var otherTreasurerResult =
            await otherTreasurer.GetAsync(url);

        using var missingResult =
            await owner.GetAsync(
                ProofUrl(
                    first.StokvelId,
                    Guid.NewGuid()));

        Assert.Equal(
            HttpStatusCode.OK,
            ownResult.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            ordinaryResult.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            otherTreasurerResult.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingResult.StatusCode);

        var ordinaryBody = await ordinaryResult.Content
            .ReadAsStringAsync();

        var missingBody = await missingResult.Content
            .ReadAsStringAsync();

        Assert.Contains(
            "Proof of payment was not found",
            ordinaryBody);

        Assert.Contains(
            "Proof of payment was not found",
            missingBody);

        using var ordinaryMetadata =
            await ordinaryMember.GetAsync(
                url + "/metadata");

        Assert.Equal(
            HttpStatusCode.NotFound,
            ordinaryMetadata.StatusCode);
    }

    [Fact]
    public async Task Renamed_text_file_is_rejected()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1);

        var ownerId = data.UserIds.First();

        var contributionId = await FindContributionIdAsync(
            data.StokvelId,
            ownerId);

        using var owner =
            _factory.CreateAuthenticatedClient(ownerId);

        var bytes = Encoding.UTF8.GetBytes(
            "This is not a PDF document.");

        using var response = await UploadAsync(
            owner,
            data.StokvelId,
            contributionId,
            bytes,
            "bank-statement.pdf");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body = await response.Content
            .ReadAsStringAsync();

        Assert.Contains(
            "\"file\"",
            body);

        using var metadata = await owner.GetAsync(
            ProofUrl(
                data.StokvelId,
                contributionId) + "/metadata");

        Assert.Equal(
            HttpStatusCode.NotFound,
            metadata.StatusCode);
    }

    [Fact]
    public async Task File_just_under_limit_succeeds_and_oversize_fails()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1);

        var ownerId = data.UserIds.First();

        var contributionId = await FindContributionIdAsync(
            data.StokvelId,
            ownerId);

        using var owner =
            _factory.CreateAuthenticatedClient(ownerId);

        var nearLimit = CreatePng(
            ProofFileValidator.MaxFileBytes - 1);

        using var accepted = await UploadAsync(
            owner,
            data.StokvelId,
            contributionId,
            nearLimit,
            "receipt.png");

        Assert.Equal(
            HttpStatusCode.OK,
            accepted.StatusCode);

        var oversized = CreatePng(
            ProofFileValidator.MaxFileBytes + 1);

        using var rejected = await UploadAsync(
            owner,
            data.StokvelId,
            contributionId,
            oversized,
            "receipt.png");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            rejected.StatusCode);

        var metadata =
            await owner.GetFromJsonAsync<ContributionProofResponse>(
                ProofUrl(
                    data.StokvelId,
                    contributionId) + "/metadata");

        Assert.NotNull(metadata);
        Assert.Equal(
            nearLimit.Length,
            metadata.SizeBytes);
    }

    [Fact]
    public async Task Closed_cycle_prevents_upload_replacement_and_delete()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var users = data.UserIds.ToArray();
        var ownerId = users[0];

        var firstContributionId =
            await FindContributionIdAsync(
                data.StokvelId,
                users[0]);

        var secondContributionId =
            await FindContributionIdAsync(
                data.StokvelId,
                users[1]);

        using var owner =
            _factory.CreateAuthenticatedClient(ownerId);

        using var secondOwner =
            _factory.CreateAuthenticatedClient(users[1]);

        using var firstUpload = await UploadAsync(
            owner,
            data.StokvelId,
            firstContributionId,
            PdfBytes);

        Assert.Equal(
            HttpStatusCode.OK,
            firstUpload.StatusCode);

        using (var scope =
            _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            await db.ContributionCycles
                .Where(cycle =>
                    cycle.Id == data.CycleId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        cycle => cycle.ClosedAtUtc,
                        DateTime.UtcNow));
        }

        using var replacement = await UploadAsync(
            owner,
            data.StokvelId,
            firstContributionId,
            PdfBytes);

        using var newUpload = await UploadAsync(
            secondOwner,
            data.StokvelId,
            secondContributionId,
            PdfBytes);

        using var deletion = await owner.DeleteAsync(
            ProofUrl(
                data.StokvelId,
                firstContributionId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            replacement.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            newUpload.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            deletion.StatusCode);

        using var original = await owner.GetAsync(
            ProofUrl(
                data.StokvelId,
                firstContributionId));

        Assert.Equal(
            HttpStatusCode.OK,
            original.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_upload_for_someone_else()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var users = data.UserIds.ToArray();

        var contributionId = await FindContributionIdAsync(
            data.StokvelId,
            users[0]);

        using var otherMember =
            _factory.CreateAuthenticatedClient(
                users[1]);

        using var response = await UploadAsync(
            otherMember,
            data.StokvelId,
            contributionId,
            PdfBytes);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Open_cycle_allows_replacement_and_removal()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1);

        var ownerId = data.UserIds.First();

        var contributionId = await FindContributionIdAsync(
            data.StokvelId,
            ownerId);

        using var owner =
            _factory.CreateAuthenticatedClient(ownerId);

        using var first = await UploadAsync(
            owner,
            data.StokvelId,
            contributionId,
            PdfBytes);

        var firstProof = await first.Content
            .ReadFromJsonAsync<ContributionProofResponse>();

        Assert.NotNull(firstProof);

        using var replaced = await UploadAsync(
            owner,
            data.StokvelId,
            contributionId,
            CreatePng(24),
            "image.png");

        Assert.Equal(
            HttpStatusCode.OK,
            replaced.StatusCode);

        var secondProof = await replaced.Content
            .ReadFromJsonAsync<ContributionProofResponse>();

        Assert.NotNull(secondProof);
        Assert.Equal(
            firstProof.Id,
            secondProof.Id);

        Assert.Equal(
            "image/png",
            secondProof.ContentType);

        using var deleted = await owner.DeleteAsync(
            ProofUrl(
                data.StokvelId,
                contributionId));

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleted.StatusCode);

        using var missing = await owner.GetAsync(
            ProofUrl(
                data.StokvelId,
                contributionId));

        Assert.Equal(
            HttpStatusCode.NotFound,
            missing.StatusCode);
    }

    [Fact]
    public async Task Proof_endpoints_require_authentication()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1);

        var contributionId = await FindContributionIdAsync(
            data.StokvelId,
            data.UserIds.First());

        using var anonymous = _factory.CreateClient();

        var url = ProofUrl(
            data.StokvelId,
            contributionId);

        using var download = await anonymous.GetAsync(url);

        using var metadata =
            await anonymous.GetAsync(url + "/metadata");

        using var upload = await UploadAsync(
            anonymous,
            data.StokvelId,
            contributionId,
            PdfBytes);

        using var delete = await anonymous.DeleteAsync(url);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            download.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            metadata.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            upload.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            delete.StatusCode);
    }

    private async Task<Guid> FindContributionIdAsync(
        Guid stokvelId,
        Guid userId)
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        return await db.Contributions
            .AsNoTracking()
            .Where(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.UserId == userId)
            .Select(contribution => contribution.Id)
            .SingleAsync();
    }

    private async Task<Guid> AddTreasurerAsync(
        Guid stokvelId)
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var unique = Guid.NewGuid().ToString("N");

        var user = new User(
            "Proof Test Treasurer",
            $"proof_{unique}@ronditrack.test",
            "0821234567");

        db.Users.Add(user);

        db.StokvelMembers.Add(
            new StokvelMember(
                stokvelId,
                user.Id,
                StokvelMemberRole.Treasurer));

        await db.SaveChangesAsync();

        return user.Id;
    }

    private static string ProofUrl(
        Guid stokvelId,
        Guid contributionId)
    {
        return
            $"/api/v1/stokvels/{stokvelId}" +
            $"/contributions/{contributionId}/proof";
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid stokvelId,
        Guid contributionId,
        byte[] bytes,
        string fileName = "proof.pdf")
    {
        using var form = new MultipartFormDataContent();

        using var content = new ByteArrayContent(bytes);

        content.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/octet-stream");

        form.Add(content, "file", fileName);

        return await client.PutAsync(
            ProofUrl(stokvelId, contributionId),
            form);
    }

    private static byte[] CreatePng(int length)
    {
        var bytes = new byte[length];

        byte[] signature =
        [
            137, 80, 78, 71,
            13, 10, 26, 10
        ];

        signature.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));

        return bytes;
    }
}
