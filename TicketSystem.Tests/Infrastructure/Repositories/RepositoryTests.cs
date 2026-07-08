using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Infrastructure.Repositories
{
    public class RepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Repository<Bus> _repository;

        public RepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new Repository<Bus>(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntity()
        {
            
            var bus = TestData.GetValidBus();

            
            var result = await _repository.AddAsync(bus);

            
            result.Should().NotBeNull();
            result.Id.Should().Be(bus.Id);
            (await _repository.GetByIdAsync(bus.Id)).Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
        {
            
            var bus = TestData.GetValidBus();
            await _repository.AddAsync(bus);

            
            var result = await _repository.GetByIdAsync(bus.Id);

            
            result.Should().NotBeNull();
            result.Id.Should().Be(bus.Id);
            result.Plate.Should().Be(bus.Plate);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            
            var buses = TestData.GetBusList(3);
            foreach (var bus in buses)
            {
                await _repository.AddAsync(bus);
            }

            
            var result = await _repository.GetAllAsync();

            
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task FindAsync_ShouldReturnFilteredEntities()
        {
            
            var buses = TestData.GetBusList(3);
            buses[0].Plate = "ABC1234";
            buses[1].Plate = "DEF5678";
            buses[2].Plate = "ABC1234";
            
            foreach (var bus in buses)
            {
                await _repository.AddAsync(bus);
            }

            
            var result = await _repository.FindAsync(b => b.Plate == "ABC1234");

            
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(b => b.Plate.Should().Be("ABC1234"));
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity()
        {
            
            var bus = TestData.GetValidBus();
            await _repository.AddAsync(bus);

            
            bus.Model = "Updated Model";
            bus.Capacity = 50;
            await _repository.UpdateAsync(bus);

            
            var updated = await _repository.GetByIdAsync(bus.Id);
            updated.Model.Should().Be("Updated Model");
            updated.Capacity.Should().Be(50);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntity()
        {
            
            var bus = TestData.GetValidBus();
            await _repository.AddAsync(bus);

            
            await _repository.DeleteAsync(bus);

            
            var deleted = await _repository.GetByIdAsync(bus.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
        {
            
            var bus = TestData.GetValidBus();
            await _repository.AddAsync(bus);

            
            var result = await _repository.ExistsAsync(b => b.Id == bus.Id);

            
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenEntityNotExists()
        {
            
            var result = await _repository.ExistsAsync(b => b.Id == Guid.NewGuid());

            
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CountAsync_ShouldReturnCorrectCount()
        {
            
            var buses = TestData.GetBusList(3);
            foreach (var bus in buses)
            {
                await _repository.AddAsync(bus);
            }

            
            var result = await _repository.CountAsync();

            
            result.Should().Be(3);
        }

        [Fact]
        public async Task CountAsync_ShouldReturnFilteredCount()
        {
            
            var buses = TestData.GetBusList(3);
            buses[0].Company = "Empresa A";
            buses[1].Company = "Empresa B";
            buses[2].Company = "Empresa A";
            
            foreach (var bus in buses)
            {
                await _repository.AddAsync(bus);
            }

            
            var result = await _repository.CountAsync(b => b.Company == "Empresa A");

            
            result.Should().Be(2);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}