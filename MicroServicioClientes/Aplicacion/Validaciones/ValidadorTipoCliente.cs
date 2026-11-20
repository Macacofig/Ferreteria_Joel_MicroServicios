using MicroServicioClientes.Aplicacion.Modelos;

namespace MicroServicioClientes.Aplicacion.Validaciones;

/// <summary>Reglas de formato del tipo de cliente.</summary>
public static class ValidadorTipoCliente
{
    public static Dictionary<string, string> Validar(TipoClienteSolicitud solicitud)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            errores[nameof(TipoClienteSolicitud.Nombre)] = "El nombre del tipo es obligatorio.";
        }
        else if (solicitud.Nombre.Length < 3 || solicitud.Nombre.Length > 50)
        {
            errores[nameof(TipoClienteSolicitud.Nombre)] =
                "El nombre del tipo debe tener entre 3 y 50 caracteres.";
        }

        if (solicitud.DescuentoPorcentaje < 0 || solicitud.DescuentoPorcentaje > 100)
        {
            errores[nameof(TipoClienteSolicitud.DescuentoPorcentaje)] =
                "El descuento debe estar entre 0 y 100.";
        }

        if (solicitud.Descripcion is { Length: > 150 })
        {
            errores[nameof(TipoClienteSolicitud.Descripcion)] =
                "La descripción no puede superar los 150 caracteres.";
        }

        return errores;
    }
}