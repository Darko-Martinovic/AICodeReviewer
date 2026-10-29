using System.Data;
using Microsoft.Data.SqlClient;

namespace Crm.Data;

public record Customer(int Id, string Name, string Email);

public class CustomerRepository(string connectionString)
{
    public async Task<Customer?> FindAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(
            "SELECT Id, Name, Email FROM dbo.Customers WHERE Id = @id", connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new Customer(reader.GetInt32(0), reader.GetString(1), reader.GetString(2))
            : null;
    }

    public async Task<bool> RenameAsync(int id, string name, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(
            "UPDATE dbo.Customers SET Name = @name WHERE Id = @id", connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name.Trim();

        return await command.ExecuteNonQueryAsync(ct) == 1;
    }
}
