using Microsoft.EntityFrameworkCore;
using ChecadorSSSD.Data;
using ChecadorSSSD.Models;

namespace ChecadorSSSD.Tests;

public abstract class TestBase : IDisposable
{
    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        // Cleanup se hace automáticamente con la base de datos en memoria
    }
}
