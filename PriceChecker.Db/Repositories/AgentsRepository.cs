using System.Linq.Expressions;
using Genius.PriceChecker.Db.Models;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.Dto.RequestMessages;
using Microsoft.EntityFrameworkCore;

namespace Genius.PriceChecker.Db.Repositories;

public interface IAgentsRepository : IRepository<Guid, AgentRef, AgentDto, CreateAgentRequest, UpdateAgentRequest>
{
    Task<bool> ExistsWithKeyAsync(string key, AgentRef? excludedAgentId = null, CancellationToken cancellationToken = default);
}

internal sealed class AgentsRepository
    : BaseRepository<Agent, Guid, AgentRef, AgentDto, CreateAgentRequest, UpdateAgentRequest>, IAgentsRepository
{
    public AgentsRepository(IDateTime dateTime, IDatabaseContext databaseContext)
        : base(dateTime, databaseContext)
    {
    }

    protected override Expression<Func<Agent, AgentDto>> ProjectToGetDto { get; }
        = a => new AgentDto(a.Id, a.Key, a.Url, a.PricePattern, a.Handler, a.DecimalDelimiter.ToString(),
            a.DateCreated, a.LastModified);

    protected override Agent MapCreateDto(CreateAgentRequest dto)
        => Agent.CreateWithNoLinking(dto.Key, dto.Url, dto.PricePattern, dto.Handler, ToDelimiterChar(dto.DecimalDelimiter));

    protected override Agent MapUpdateDto(UpdateAgentRequest dto, Agent existingEntity)
        => existingEntity with
        {
            Key = dto.Key,
            Url = dto.Url,
            PricePattern = dto.PricePattern,
            Handler = dto.Handler,
            DecimalDelimiter = ToDelimiterChar(dto.DecimalDelimiter),
        };

    public async Task<bool> ExistsWithKeyAsync(string key, AgentRef? excludedAgentId = null, CancellationToken cancellationToken = default)
    {
        var agents = GetContext().Set<Agent>().AsNoTracking().Where(x => x.Key == key);
        if (excludedAgentId is not null)
        {
            agents = agents.Where(NotWithId(excludedAgentId));
        }

        return await agents.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    private static char ToDelimiterChar(string decimalDelimiter)
        => string.IsNullOrEmpty(decimalDelimiter) ? '.' : decimalDelimiter[0];

    private static Expression<Func<Agent, bool>> NotWithId(AgentRef id)
    {
        var param = Expression.Parameter(typeof(Agent), "a");
        var property = Expression.Property(param, nameof(Agent.Id));
        var constant = Expression.Constant(id, typeof(AgentRef));
        return Expression.Lambda<Func<Agent, bool>>(Expression.NotEqual(property, constant), param);
    }
}
