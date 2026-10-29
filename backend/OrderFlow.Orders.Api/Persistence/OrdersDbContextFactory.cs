using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderFlow.Orders.Api.Persistence;

internal sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer(sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.Schema))
            .Options;

        return new OrdersDbContext(options);
    }
}
