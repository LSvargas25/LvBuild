namespace LvApi.Middleware;

/// <summary>Spanish message for a request body that could not be bound to the endpoint's DTO.</summary>
public static class InvalidRequestMessage
{
    public static string For(IEnumerable<string> modelStateKeys)
    {
        // Keys look like "$.quantity", "$.details[0].hoursWorked" or the action parameter name.
        var fields = modelStateKeys
            .Where(k => k.StartsWith("$.", StringComparison.Ordinal))
            .Select(k => k[2..])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return fields.Count == 0
            ? "La solicitud tiene un formato inválido."
            : $"La solicitud tiene datos con formato inválido en: {string.Join(", ", fields)}.";
    }
}
