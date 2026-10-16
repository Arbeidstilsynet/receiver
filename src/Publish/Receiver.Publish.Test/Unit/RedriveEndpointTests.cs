using System.Net.Http.Json;
using Arbeidstilsynet.Receiver.DependencyInjection;
using Arbeidstilsynet.Receiver.Ports;
using Arbeidstilsynet.Receiver.Ports.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

namespace Arbeidstilsynet.Receiver.Test;

public class RedriveEndpointTests
{
    [Theory]
    [InlineData("/redrive-actions", null)]
    [InlineData("/redrive-actions", "?messageIds=1-0")]
    [InlineData("/redrive-actions", "?messageIds=1-0&messageIds=2-0")]
    [InlineData("/custom-redrive", "?messageIds=1-0&messageIds=2-0")]
    public async Task RedriveEndpoint_BindsOptionalStreamIdsAndReturnsResult(
        string pattern,
        string? query
    )
    {
        var redriver = Substitute.For<IMeldingerRedriver>();
        var expected = new RedriveResult
        {
            SuccessfulCount = 1,
            UnsuccessfulCount = 0,
            RedriveExceptions = [],
        };
        redriver.RedrivePendingMessages().Returns(expected);
        redriver
            .RedrivePendingMessages(Arg.Any<IReadOnlyCollection<RedisValue>>())
            .Returns(expected);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(redriver);
        builder.Services.AddEndpointsApiExplorer();
        await using var app = builder.Build();
        app.MapCommonRedriveEndpoints(pattern);
        var ct = TestContext.Current.CancellationToken;
        await app.StartAsync(ct);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        using var response = await client.PostAsync(
            $"{pattern}/redrive-pending-messages{query}",
            null,
            ct
        );

        response.EnsureSuccessStatusCode();
        (await response.Content.ReadFromJsonAsync<RedriveResult>(ct)).ShouldBeEquivalentTo(
            expected
        );
        if (query is null)
        {
            await redriver.Received(1).RedrivePendingMessages();
            await redriver
                .DidNotReceive()
                .RedrivePendingMessages(Arg.Any<IReadOnlyCollection<RedisValue>>());
        }
        else
        {
            RedisValue[] ids = query.Contains('&') ? ["1-0", "2-0"] : ["1-0"];
            await redriver
                .Received(1)
                .RedrivePendingMessages(
                    Arg.Is<IReadOnlyCollection<RedisValue>>(values => values.SequenceEqual(ids))
                );
            await redriver.DidNotReceive().RedrivePendingMessages();
        }

        var description = app
            .Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Single(api =>
                api.RelativePath == $"{pattern.TrimStart('/')}/redrive-pending-messages"
            );
        var parameter = description.ParameterDescriptions.ShouldHaveSingleItem();
        parameter.Name.ShouldBe("messageIds");
        parameter.Type.ShouldBe(typeof(string[]));
        parameter.IsRequired.ShouldBeFalse();
        parameter.Source.Id.ShouldBe("Query");
    }
}
