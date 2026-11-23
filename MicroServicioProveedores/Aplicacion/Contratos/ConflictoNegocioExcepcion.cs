namespace MicroServicioProveedores.Aplicacion.Contratos;

public class ConflictoNegocioExcepcion : Exception
{
    public string Campo { get; }
    public ConflictoNegocioExcepcion(string mensaje, string campo = "") : base(mensaje)
    {
        Campo = campo;
    }
}
