using AutoMapper;
using Domain;
using Application.DTOs.Responses;

namespace Application.Profiles;

public class UsuarioProfile : Profile
{
    public UsuarioProfile()
    {
        CreateMap<DateTime, DateOnly>().ConvertUsing(d => DateOnly.FromDateTime(d));

        // Privacidad: la ubicación solo sale si hay consentimiento, aunque la base trajera algo (defensa en profundidad).
        CreateMap<Usuario, UsuarioResponse>()
            .ForMember(d => d.Ciudad, m => m.MapFrom(s => s.ConsentimientoDatos == "si" ? s.Ciudad : null))
            .ForMember(d => d.Latitud, m => m.MapFrom(s => s.ConsentimientoDatos == "si" ? s.Latitud : null))
            .ForMember(d => d.Longitud, m => m.MapFrom(s => s.ConsentimientoDatos == "si" ? s.Longitud : null))
            .ForMember(d => d.ZonaHoraria, m => m.MapFrom(s => s.ConsentimientoDatos == "si" ? s.ZonaHoraria : null))
            .ForMember(d => d.Intereses, m => m.Ignore());

        CreateMap<InteresUsuario, InteresUsuarioResponse>();

        CreateMap<InteraccionLectura, InteraccionResponse>()
            .ForMember(d => d.FechaHoraUtc, m => m.MapFrom(s => DateTime.SpecifyKind(s.FechaHora, DateTimeKind.Utc)))
            .ForMember(d => d.Etiquetas, m => m.MapFrom(s => string.IsNullOrEmpty(s.EtiquetasCsv)
                ? new List<string>() : s.EtiquetasCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()));
    }
}
