using MicroServicioProveedores.Dominio.Resultados;

namespace MicroServicioProveedores.Dominio.Entidades;

/// <summary>
/// Proveedor de mercadería de la ferretería.
/// Se conservan los campos y el tipo de Id (entero) del contrato original.
/// </summary>
public class Proveedor
{
    public int Id { get; private set; }
    public string RazonSocial { get; private set; } = string.Empty;
    public string Nit { get; private set; } = string.Empty;
    public string Contacto { get; private set; } = string.Empty;
    public string Telefono { get; private set; } = string.Empty;

    private Proveedor() { }

    public Proveedor(int id, string razonSocial, string nit, string contacto, string telefono)
    {
        Id = id;
        RazonSocial = razonSocial;
        Nit = nit;
        Contacto = contacto;
        Telefono = telefono;
    }

    public static Result<Proveedor> CrearNuevo(int id, string razonSocial, string nit, string contacto, string telefono)
    {
        var errores = ValidarCampos(razonSocial, nit, contacto, telefono);
        if (errores.Count > 0)
        {
            return Result.Fallo<Proveedor>(ErrorResultado.Validacion(
                "Los datos del proveedor no son válidos.",
                errores));
        }

        return Result.Ok(new Proveedor(
            id,
            NormalizarTexto(razonSocial),
            NormalizarNit(nit),
            NormalizarTexto(contacto),
            NormalizarTexto(telefono)));
    }

    public Result Actualizar(string razonSocial, string nit, string contacto, string telefono)
    {
        var errores = ValidarCampos(razonSocial, nit, contacto, telefono);
        if (errores.Count > 0)
        {
            return Result.Fallo(ErrorResultado.Validacion(
                "Los datos del proveedor no son válidos.",
                errores));
        }

        RazonSocial = NormalizarTexto(razonSocial);
        Nit = NormalizarNit(nit);
        Contacto = NormalizarTexto(contacto);
        Telefono = NormalizarTexto(telefono);
        return Result.Ok();
    }

    public static string NormalizarNit(string nit)
    {
        // Supuesto: el NIT se normaliza quitando espacios y guiones y pasando a mayúsculas
        // para garantizar unicidad consistente independiente del formato ingresado.
        return (nit ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .ToUpperInvariant();
    }

    public static string NormalizarTexto(string valor) => (valor ?? string.Empty).Trim();

    private static Dictionary<string, string> ValidarCampos(string razonSocial, string nit, string contacto, string telefono)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(razonSocial))
            errores[nameof(RazonSocial)] = "La razón social es obligatoria.";
        else if (razonSocial.Trim().Length > 150)
            errores[nameof(RazonSocial)] = "La razón social no puede exceder 150 caracteres.";

        string nitNormalizado = NormalizarNit(nit);
        if (string.IsNullOrWhiteSpace(nitNormalizado))
            errores[nameof(Nit)] = "El NIT es obligatorio.";
        else if (nitNormalizado.Length > 30)
            errores[nameof(Nit)] = "El NIT no puede exceder 30 caracteres.";

        if (string.IsNullOrWhiteSpace(contacto))
            errores[nameof(Contacto)] = "El contacto es obligatorio.";
        else if (contacto.Trim().Length > 100)
            errores[nameof(Contacto)] = "El contacto no puede exceder 100 caracteres.";

        if (string.IsNullOrWhiteSpace(telefono))
            errores[nameof(Telefono)] = "El teléfono es obligatorio.";
        else if (telefono.Trim().Length > 30)
            errores[nameof(Telefono)] = "El teléfono no puede exceder 30 caracteres.";

        return errores;
    }
}
