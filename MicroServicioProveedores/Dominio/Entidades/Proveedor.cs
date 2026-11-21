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
    public string NombreResponsable { get; private set; } = string.Empty;

    private Proveedor() { }

    public Proveedor(int id, string razonSocial, string nit, string contacto, string telefono, string nombreResponsable = "")
    {
        Id = id;
        RazonSocial = razonSocial;
        Nit = nit;
        Contacto = contacto;
        Telefono = telefono;
        NombreResponsable = nombreResponsable;
    }

    public static Result<Proveedor> CrearNuevo(int id, string razonSocial, string nit, string contacto, string telefono, string nombreResponsable)
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
            NormalizarTelefono(telefono),
            NormalizarTexto(nombreResponsable)));
    }

    public Result Actualizar(string razonSocial, string nit, string contacto, string telefono, string nombreResponsable)
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
        Telefono = NormalizarTelefono(telefono);
        NombreResponsable = NormalizarTexto(nombreResponsable);
        return Result.Ok();
    }

    public static string NormalizarNit(string nit)
    {
        // El NIT se normaliza quitando espacios y guiones para consistencia.
        return (nit ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);
    }

    public static string NormalizarTexto(string valor) => (valor ?? string.Empty).Trim();

    public static string NormalizarTelefono(string telefono)
    {
        // Se eliminan espacios y guiones comunes; la validación exige 8 dígitos (5/6/7 inicial).
        return (telefono ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);
    }

    private static Dictionary<string, string> ValidarCampos(string razonSocial, string nit, string contacto, string telefono)
    {
        var errores = new Dictionary<string, string>();

        string razonSocialNormalizada = NormalizarTexto(razonSocial);
        if (string.IsNullOrWhiteSpace(razonSocialNormalizada))
            errores[nameof(RazonSocial)] = "La razón social es obligatoria.";
        else if (razonSocialNormalizada.Length > 150)
            errores[nameof(RazonSocial)] = "La razón social no puede exceder 150 caracteres.";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(razonSocialNormalizada, @"^[\p{L}\p{N} ]+$"))
            errores[nameof(RazonSocial)] = "La razón social solo puede contener letras, números y espacios (sin símbolos especiales).";

        string nitNormalizado = NormalizarNit(nit);
        if (string.IsNullOrWhiteSpace(nitNormalizado))
            errores[nameof(Nit)] = "El NIT es obligatorio.";
        else if (nitNormalizado.Length > 30)
            errores[nameof(Nit)] = "El NIT no puede exceder 30 caracteres.";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(nitNormalizado, @"^\d+$"))
            errores[nameof(Nit)] = "El NIT solo debe contener números.";

        string contactoNormalizado = NormalizarTexto(contacto);
        if (string.IsNullOrWhiteSpace(contactoNormalizado))
            errores[nameof(Contacto)] = "El contacto es obligatorio.";
        else if (contactoNormalizado.Length > 100)
            errores[nameof(Contacto)] = "El contacto no puede exceder 100 caracteres.";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(contactoNormalizado, @"^[\p{L} ]+$"))
            errores[nameof(Contacto)] = "El contacto solo puede contener letras y espacios (sin símbolos especiales ni números).";

        string telefonoNormalizado = NormalizarTelefono(telefono);
        if (string.IsNullOrWhiteSpace(telefonoNormalizado))
            errores[nameof(Telefono)] = "El teléfono es obligatorio.";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(telefonoNormalizado, @"^[5-7]\d{7}$"))
            errores[nameof(Telefono)] = "El teléfono debe tener 8 dígitos y comenzar con 5, 6 o 7.";

        return errores;
    }
}
