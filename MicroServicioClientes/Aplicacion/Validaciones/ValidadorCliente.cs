using System.Text.RegularExpressions;
using MicroServicioClientes.Aplicacion.Modelos;

namespace MicroServicioClientes.Aplicacion.Validaciones;

/// <summary>Reglas de formato del cliente. Devuelve un mensaje por campo inválido.</summary>
public static class ValidadorCliente
{
    private static readonly Regex PatronNombre =
        new(@"^\p{L}[\p{L} .'\-]{1,79}$", RegexOptions.Compiled);

    private static readonly Regex PatronDocumento =
        new(@"^[A-Za-z0-9\-]{5,20}$", RegexOptions.Compiled);

    private static readonly Regex PatronTelefono =
        new(@"^\+?[0-9 \-]{7,15}$", RegexOptions.Compiled);

    private static readonly Regex PatronCorreo =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static Dictionary<string, string> Validar(ClienteSolicitud solicitud)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            errores[nameof(ClienteSolicitud.Nombre)] = "El nombre es obligatorio.";
        }
        else if (!PatronNombre.IsMatch(solicitud.Nombre))
        {
            errores[nameof(ClienteSolicitud.Nombre)] =
                "El nombre solo admite letras, espacios, puntos, apóstrofes y guiones (de 2 a 80 caracteres).";
        }

        if (string.IsNullOrWhiteSpace(solicitud.Documento))
        {
            errores[nameof(ClienteSolicitud.Documento)] = "El documento es obligatorio.";
        }
        else if (!PatronDocumento.IsMatch(solicitud.Documento))
        {
            errores[nameof(ClienteSolicitud.Documento)] =
                "El documento debe tener de 5 a 20 caracteres entre letras, números y guiones.";
        }

        if (string.IsNullOrWhiteSpace(solicitud.Telefono))
        {
            errores[nameof(ClienteSolicitud.Telefono)] = "El teléfono es obligatorio.";
        }
        else if (!PatronTelefono.IsMatch(solicitud.Telefono))
        {
            errores[nameof(ClienteSolicitud.Telefono)] =
                "El teléfono debe tener de 7 a 15 dígitos y puede incluir espacios, guiones o el prefijo +.";
        }

        if (solicitud.Correo is not null)
        {
            if (solicitud.Correo.Length > 100 || !PatronCorreo.IsMatch(solicitud.Correo))
            {
                errores[nameof(ClienteSolicitud.Correo)] =
                    "El correo electrónico no tiene un formato válido.";
            }
        }

        if (solicitud.Direccion is { Length: > 150 })
        {
            errores[nameof(ClienteSolicitud.Direccion)] =
                "La dirección no puede superar los 150 caracteres.";
        }

        if (solicitud.IdTipoCliente <= 0)
        {
            errores[nameof(ClienteSolicitud.IdTipoCliente)] = "Seleccione un tipo de cliente.";
        }

        return errores;
    }
}