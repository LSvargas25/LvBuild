using LvApplication.Common.Exceptions;

namespace LvApplication.Services.Auth;

/// <summary>
/// What a profile photo may be: at most 1 MB and a real JPEG, PNG or WEBP. The declared
/// Content-Type comes from the client, so the file signature (magic bytes) must match it too.
/// </summary>
public static class ProfilePhotoRules
{
    public const int MaxSizeBytes = 1024 * 1024;

    private static readonly Dictionary<string, Func<byte[], bool>> SignatureByContentType = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["image/jpeg"] = b => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        ["image/png"] = b =>
            b.Length >= 8
            && b.AsSpan(0, 8)
                .SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        // RIFF....WEBP
        ["image/webp"] = b =>
            b.Length >= 12
            && b.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && b.AsSpan(8, 4).SequenceEqual("WEBP"u8),
    };

    /// <summary>Reads the upload (refusing more than <see cref="MaxSizeBytes"/>) and validates it.</summary>
    public static async Task<byte[]> ReadAndValidateAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        if (!SignatureByContentType.TryGetValue(contentType, out var hasValidSignature))
        {
            throw new ValidationAppException(
                $"Tipo de archivo no permitido: {contentType}. Solo se permiten imágenes JPEG, PNG o WEBP."
            );
        }

        // Read at most one byte over the limit, so an oversized upload is never fully buffered.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxSizeBytes)
            {
                throw new ValidationAppException(
                    "El archivo excede el tamaño máximo permitido de 1 MB."
                );
            }
            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.ToArray();
        if (!hasValidSignature(bytes))
        {
            throw new ValidationAppException(
                $"El contenido del archivo no corresponde a una imagen {contentType} válida."
            );
        }

        return bytes;
    }
}
