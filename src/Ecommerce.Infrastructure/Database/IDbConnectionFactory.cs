using System.Data;

namespace Ecommerce.Infrastructure.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

