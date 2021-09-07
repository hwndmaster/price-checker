using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.Infrastructure.Persistence;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Genius.PriceChecker.Core.Tests.Repositories
{
    public class AgentRepositoryTests
    {
        private readonly AgentRepository _sut;
        private readonly Fixture _fixture = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly Mock<IJsonPersister> _persisterMock = new();

        private readonly List<Agent> _agents = new();

        public AgentRepositoryTests()
        {
            _agents = _fixture.CreateMany<Agent>().ToList();

            _persisterMock.Setup(x => x.LoadCollection<Agent>(It.IsAny<string>()))
                .Returns(_agents.ToArray());

            _sut = new AgentRepository(_eventBusMock.Object, _persisterMock.Object,
                Mock.Of<ILogger<AgentRepository>>());

            _sut.GetAll(); // To trigger the initializer
        }

        [Fact]
        public void FindById__Returns_appropriate_agent()
        {
            // Arrange
            var agentToFind = _agents[1];

            // Act
            var result = _sut.FindById(agentToFind.Id);

            // Verify
            Assert.Equal(agentToFind, result);
        }

        [Fact]
        public void Delete__Removes_appripriate_agent()
        {
            // Arrange
            var agentToDelete = _agents[1];

            // Act
            _sut.Delete(agentToDelete.Id);

            // Verify
            Assert.Null(_sut.FindById(agentToDelete.Id));
        }

        [Fact]
        public void Delete__When_no_agent_found__Breaks_operation()
        {
            // Arrange
            var agentCount = _sut.GetAll().Count();

            // Act
            _sut.Delete(Guid.NewGuid());

            // Verify
            Assert.Equal(agentCount, _sut.GetAll().Count());
        }

        [Fact]
        public void Store__Replaces_all_existing_agents_and_updates_cache_and_fires_event()
        {
            // Arrange
            var newAgents = _fixture.CreateMany<Agent>().ToArray();
            var previousAgents = _sut.GetAll().ToArray();

            // Act
            _sut.Overwrite(newAgents);

            // Verify
            Assert.False(_sut.GetAll().Except(newAgents).Any());
            _persisterMock.Verify(x => x.Store(It.IsAny<string>(),
                It.Is((List<Agent> p) => p.SequenceEqual(newAgents))));
            _eventBusMock.Verify(x => x.Publish(It.Is<EntitiesAddedEvent>(e => e.Entities.Count == newAgents.Length)), Times.Once);
            _eventBusMock.Verify(x => x.Publish(It.Is<EntitiesDeletedEvent>(e => e.Entities.Count == previousAgents.Length)), Times.Once);
        }
    }
}
