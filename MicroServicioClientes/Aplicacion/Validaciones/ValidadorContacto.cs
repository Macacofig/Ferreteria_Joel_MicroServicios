using System.Text.RegularExpressions;
using MicroServicioClientes.Aplicacion.Modelos;

namespace MicroServicioClientes.Aplicacion.Validaciones;

/// <summary>Reglas de formato de una persona de contacto.</summary>
public static class ValidadorContacto
{
    private static readonly Regex PatronTexto =
        new(@"^\p{L}[\p{L} .'\-]{1,99}$", RegexOptions.Compiled);

    private static readonly Regex PatronTelefono =
        new(@"^[567][0-9]{7}$", RegexOptions.Compiled);

    private static readonly Regex PatronCorreo =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static Dictionary<string, string> Validar(ContactoSolicitud solicitud)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            errores[nameof(ContactoSolicitud.Nombre)] = "El nombre del contacto es obligatorio.";
        }
        else if (!PatronTexto.IsMatch(solicitud.Nombre))
        {
            errores[nameof(ContactoSolicitud.Nombre)] =
                "El nombre solo admite letras, espacios, puntos, apóstrofes y guiones (de 2 a 100 caracteres).";
        }

        if (solicitud.Cargo is { Length: > 50 })
        {
            errores[nameof(ContactoSolicitud.Cargo)] =
                "El cargo no puede superar los 50 caracteres.";
        }

        if (solicitud.Telefono is not null && !PatronTelefono.IsMatch(solicitud.Telefono))
        {
            errores[nameof(ContactoSolicitud.Telefono)] =
                "El teléfono debe tener 8 dígitos y comenzar con 5, 6 o 7.";
        }

        if (solicitud.Correo is not null
            && (solicitud.Correo.Length > 100 || !PatronCorreo.IsMatch(solicitud.Correo)))
        {
            errores[nameof(ContactoSolicitud.Correo)] =
                "El correo electrónico no tiene un formato válido.";
        }

        return errores;
    }
}
