namespace Domain;

/// <summary>Un valor de catálogo. <see cref="AdmiteTexto"/> solo aplica a tipos de interacción.</summary>
public class ItemCatalogo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool? AdmiteTexto { get; set; }
}
