using System.Text.RegularExpressions;
using MicroServicioClientes.Aplicacion.Modelos;

namespace MicroServicioClientes.Aplicacion.Validaciones;

/// <summary>Reglas de formato del cliente. Devuelve un mensaje por campo inválido.</summary>
public static class ValidadorCliente
{
    private static readonly Regex PatronTexto =
        new(@"^\p{L}[\p{L} .'\-]{1,99}$", RegexOptions.Compiled);

    private static readonly Regex PatronCi =
        new(@"^[A-Za-z0-9\-]{5,15}$", RegexOptions.Compiled);

    private static readonly Regex PatronTelefono =
        new(@"^\+?[0-9 \-]{7,15}$", RegexOptions.Compiled);

    private static readonly Regex PatronCorreo =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static Dictionary<string, string> Validar(ClienteSolicitud solicitud)
    {
        var errores = new Dictionary<string, string>();

        if (solicitud.Ci is not null && !PatronCi.IsMatch(solicitud.Ci))
        {
            errores[nameof(ClienteSolicitud.Ci)] =
                "La CI debe tener de 5 a 15 caracteres entre letras, números y guiones.";
        }

        ValidarTexto(errores, nameof(ClienteSolicitud.Nombre), "nombre", solicitud.Nombre);
        ValidarTexto(errores, nameof(ClienteSolicitud.Apellido), "apellido", solicitud.Apellido);

        if (solicitud.Correo is not null
            && (solicitud.Correo.Length > 100 || !PatronCorreo.IsMatch(solicitud.Correo)))
        {
            errores[nameof(ClienteSolicitud.Correo)] =
                "El correo electrónico no tiene un formato válido.";
        }

        if (solicitud.Telefono is not null && !PatronTelefono.IsMatch(solicitud.Telefono))
        {
            errores[nameof(ClienteSolicitud.Telefono)] =
                "El teléfono debe tener de 7 a 15 dígitos y puede incluir espacios, guiones o el prefijo +.";
        }

        if (solicitud.Direccion is { Length: > 150 })
        {
            errores[nameof(ClienteSolicitud.Direccion)] =
                "La dirección no puede superar los 150 caracteres.";
        }

        return errores;
    }

    private static void ValidarTexto(
        Dictionary<string, string> errores,
        string campo,
        string etiqueta,
        string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            errores[campo] = $"El {etiqueta} es obligatorio.";
        }
        else if (!PatronTexto.IsMatch(valor))
        {
            errores[campo] =
                $"El {etiqueta} solo admite letras, espacios, puntos, apóstrofes y guiones (de 2 a 100 caracteres).";
        }
    }
}