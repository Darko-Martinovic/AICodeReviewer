using System.Data;
using Microsoft.Data.SqlClient;

namespace Billing.Data;

public record Invoice(int Id, string CustomerName, decimal Amount, DateTime IssuedOn);

public class InvoiceRepository
{
    private static readonly HashSet<string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "IssuedOn", "Amount", "CustomerName"
    };

    private readonly string _connectionString;

    public InvoiceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Invoice>> SearchAsync(string customer, string sortBy, CancellationToken ct)
    {
        var sql = $"""
            SELECT Id, CustomerName, Amount, IssuedOn
            FROM dbo.Invoices
            WHERE CustomerName LIKE @customer
            ORDER BY {sortBy}
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@customer", SqlDbType.NVarChar, 200).Value = $"%{customer}%";

        var invoices = new List<Invoice>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            invoices.Add(new Invoice(
                reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetDateTime(3)));
        }
        return invoices;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand("DELETE FROM dbo.Invoices WHERE IssuedOn < @cutoff", connection);
        command.Parameters.Add("@cutoff", SqlDbType.DateTime2).Value = cutoff;
        return await command.ExecuteNonQueryAsync(ct);
    }
}
